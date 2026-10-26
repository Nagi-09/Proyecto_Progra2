using Microsoft.EntityFrameworkCore;
using VisitasRecepcion.Data;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Services;

/// <summary>Una fila de la pantalla de avisos (DTO: solo datos para mostrar).</summary>
public class FilaAviso
{
    public int IdAviso { get; set; }           // se oculta en la grilla
    public int IdVisita { get; set; }          // se oculta en la grilla
    public bool EnCurso { get; set; }          // se oculta: true si el visitante sigue dentro
    public string Recibido { get; set; } = "";
    public string Tipo { get; set; } = "";     // "Llegada" o "Tiempo terminado"
    public string Visitante { get; set; } = "";
    public string Area { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Visita { get; set; } = "";   // "En curso (hasta 10:45)", "Tiempo excedido" o "Finalizada 10:30"
    public string Estado { get; set; } = "";   // "Sin leer" o "Leído"
}

/// <summary>Opción del combo de anfitriones: muestra cuántos avisos tiene sin leer.</summary>
public class OpcionAnfitrion
{
    public int IdEmpleado { get; set; }
    public string Nombre { get; set; } = "";
    public int SinLeer { get; set; }

    public override string ToString() => SinLeer > 0 ? $"{Nombre}  ({SinLeer} sin leer)" : Nombre;
}

/// <summary>
/// Avisos internos al anfitrión (RF-05). Al registrar una visita se guarda un aviso
/// en la tabla Avisos y el anfitrión lo ve en la pantalla "Avisos". No usa correo.
/// </summary>
public class NotificacionService
{
    /// <summary>
    /// Crea el aviso de llegada para el anfitrión de la visita. Devuelve (true, mensaje)
    /// si salió bien o (false, mensaje de error) si falló. Nunca lanza excepción hacia afuera:
    /// si el aviso falla, la visita ya quedó guardada.
    /// </summary>
    public (bool Ok, string Mensaje) CrearAviso(int idVisita)
    {
        try
        {
            using var db = new AppDbContext();

            var visita = db.Visitas
                .Include(v => v.Visitante)
                .Include(v => v.Empleado)
                .Include(v => v.Area)
                .First(v => v.IdVisita == idVisita);

            db.Avisos.Add(new Aviso
            {
                IdVisita = idVisita,
                IdEmpleado = visita.IdEmpleado,
                Tipo = TiposAviso.Llegada,
                FechaCreacion = DateTime.Now,
                Mensaje = $"{visita.Visitante!.Nombre} llegó a recepción para verte " +
                          $"({visita.Area!.Nombre}). Motivo: {visita.Motivo}.",
                Leido = false
            });
            visita.NotificacionEnviada = true;
            db.SaveChanges();

            return (true, $"Aviso enviado a {visita.Empleado!.Nombre}.");
        }
        catch (Exception ex)
        {
            return (false, "No se pudo generar el aviso al anfitrión: " + ex.Message);
        }
    }

    /// <summary>
    /// Anfitriones activos, con su cantidad de avisos sin leer.
    /// Si se indica soloEmpleado, devuelve solo ese (lo usa el rol Trabajador).
    /// </summary>
    public List<OpcionAnfitrion> ObtenerAnfitriones(int? soloEmpleado = null)
    {
        using var db = new AppDbContext();

        return db.Empleados
            .Where(e => e.Activo && (soloEmpleado == null || e.IdEmpleado == soloEmpleado))
            .OrderBy(e => e.Nombre)
            .Select(e => new OpcionAnfitrion
            {
                IdEmpleado = e.IdEmpleado,
                Nombre = e.Nombre,
                SinLeer = db.Avisos.Count(a => !a.Leido && a.IdEmpleado == e.IdEmpleado)
            })
            .ToList();
    }

    /// <summary>Avisos de un anfitrión, del más nuevo al más viejo.</summary>
    public List<FilaAviso> ObtenerAvisos(int idEmpleado, bool soloSinLeer)
    {
        using var db = new AppDbContext();

        var consulta = db.Avisos
            .Include(a => a.Visita).ThenInclude(v => v!.Visitante)
            .Include(a => a.Visita).ThenInclude(v => v!.Area)
            .Where(a => a.IdEmpleado == idEmpleado);

        if (soloSinLeer)
            consulta = consulta.Where(a => !a.Leido);

        var ahora = DateTime.Now;

        return consulta
            .OrderByDescending(a => a.FechaCreacion)
            .Take(200)
            .ToList()
            .Select(a => new FilaAviso
            {
                IdAviso = a.IdAviso,
                IdVisita = a.IdVisita,
                EnCurso = a.Visita!.FechaHoraSalida == null,
                Recibido = a.FechaCreacion.ToString("dd/MM/yyyy HH:mm"),
                Tipo = a.Tipo == TiposAviso.TiempoTerminado ? "Tiempo terminado" : "Llegada",
                Visitante = a.Visita.Visitante!.Nombre,
                Area = a.Visita.Area!.Nombre,
                Motivo = a.Visita.Motivo,
                Visita = EstadoVisita(a.Visita, ahora),
                Estado = a.Leido ? "Leído" : "Sin leer"
            })
            .ToList();
    }

    private static string EstadoVisita(Visita v, DateTime ahora)
    {
        if (v.FechaHoraSalida != null) return $"Finalizada {v.FechaHoraSalida:HH:mm}";
        return v.HoraLimite <= ahora ? "Tiempo excedido" : $"En curso (hasta {v.HoraLimite:HH:mm})";
    }

    /// <summary>
    /// Busca visitas que siguen dentro y ya pasaron su tiempo permitido, y le deja al
    /// anfitrión un aviso de "tiempo terminado" (uno solo por visita).
    /// La llama el menú principal cada pocos segundos. Devuelve cuántos avisos creó.
    /// </summary>
    public int RevisarTiemposTerminados()
    {
        using var db = new AppDbContext();
        var ahora = DateTime.Now;

        var vencidas = db.Visitas
            .Include(v => v.Visitante)
            .Where(v => v.FechaHoraSalida == null
                     && v.FechaHoraEntrada.AddMinutes(v.DuracionMaxMin) <= ahora
                     && !v.Avisos.Any(a => a.Tipo == TiposAviso.TiempoTerminado))
            .ToList();

        foreach (var v in vencidas)
        {
            db.Avisos.Add(new Aviso
            {
                IdVisita = v.IdVisita,
                IdEmpleado = v.IdEmpleado,
                Tipo = TiposAviso.TiempoTerminado,
                FechaCreacion = ahora,
                Mensaje = $"Terminó el tiempo de la visita de {v.Visitante!.Nombre} " +
                          $"({v.DuracionMaxMin} min, desde las {v.FechaHoraEntrada:HH:mm}).",
                Leido = false
            });
        }

        db.SaveChanges();
        return vencidas.Count;
    }

    /// <summary>
    /// Termina una visita aunque todavía le quede tiempo: registra la salida ahora y da por
    /// leídos sus avisos. Si se indica soloEmpleado, solo puede terminar visitas de ese anfitrión.
    /// Devuelve el nombre del visitante.
    /// </summary>
    public string TerminarVisita(int idVisita, int? soloEmpleado = null)
    {
        using var db = new AppDbContext();
        var visita = db.Visitas
            .Include(v => v.Visitante)
            .Include(v => v.Avisos)
            .First(v => v.IdVisita == idVisita);

        if (soloEmpleado != null && visita.IdEmpleado != soloEmpleado)
            throw new InvalidOperationException("Solo puede terminar sus propias visitas.");

        var ahora = DateTime.Now;
        visita.FechaHoraSalida ??= ahora;

        foreach (var aviso in visita.Avisos.Where(a => !a.Leido))
        {
            aviso.Leido = true;
            aviso.FechaLectura = ahora;
        }

        db.SaveChanges();
        return visita.Visitante!.Nombre;
    }

    /// <summary>Id del aviso más reciente del empleado (0 si no tiene). Sirve para detectar avisos nuevos.</summary>
    public int UltimoIdAviso(int idEmpleado)
    {
        using var db = new AppDbContext();
        return db.Avisos.Where(a => a.IdEmpleado == idEmpleado).Max(a => (int?)a.IdAviso) ?? 0;
    }

    /// <summary>Avisos sin leer del empleado creados después de desdeId (para la notificación de Windows).</summary>
    public List<Aviso> ObtenerNuevos(int idEmpleado, int desdeId)
    {
        using var db = new AppDbContext();
        return db.Avisos
            .Where(a => a.IdEmpleado == idEmpleado && a.IdAviso > desdeId && !a.Leido)
            .OrderBy(a => a.IdAviso)
            .ToList();
    }

    /// <summary>Avisos sin leer (tarjeta del menú): de todos, o solo de un empleado.</summary>
    public int ContarSinLeer(int? idEmpleado = null)
    {
        using var db = new AppDbContext();
        return db.Avisos.Count(a => !a.Leido && (idEmpleado == null || a.IdEmpleado == idEmpleado));
    }

    public void MarcarLeido(int idAviso)
    {
        using var db = new AppDbContext();
        var aviso = db.Avisos.First(a => a.IdAviso == idAviso);

        if (!aviso.Leido)
        {
            aviso.Leido = true;
            aviso.FechaLectura = DateTime.Now;
            db.SaveChanges();
        }
    }

    public void MarcarTodosLeidos(int idEmpleado)
    {
        using var db = new AppDbContext();
        var ahora = DateTime.Now;

        foreach (var aviso in db.Avisos.Where(a => !a.Leido && a.IdEmpleado == idEmpleado))
        {
            aviso.Leido = true;
            aviso.FechaLectura = ahora;
        }
        db.SaveChanges();
    }
}
