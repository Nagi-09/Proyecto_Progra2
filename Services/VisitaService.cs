using Microsoft.EntityFrameworkCore;
using VisitasRecepcion.Data;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Services;

/// <summary>Resultado de registrar una visita (lo usa el formulario para mostrar mensajes).</summary>
public class ResultadoRegistro
{
    public int IdVisita { get; set; }
    public bool EraPrimeraVisita { get; set; }
    public string Advertencia { get; set; } = "";     // vacío si todo está dentro del límite
    public int DuracionMaxMin { get; set; }
}

/// <summary>Lógica de negocio del registro de visitas.</summary>
public class VisitaService
{
    /// <summary>Busca visitantes por nombre o por número de documento (RF-02).</summary>
    public List<Visitante> BuscarVisitantes(string texto)
    {
        using var db = new AppDbContext();
        texto = texto.Trim();

        return db.Visitantes
            .Where(v => v.Nombre.Contains(texto) || v.Documento.Contains(texto))
            .OrderBy(v => v.Nombre)
            .Take(50)
            .ToList();
    }

    /// <summary>
    /// Registra el ingreso de un visitante (RF-01, RF-03, RF-04).
    /// Si datos.IdVisitante == 0 es un visitante nuevo; si no, es recurrente.
    /// </summary>
    public ResultadoRegistro RegistrarVisita(Visitante datos, int idEmpleado, int idArea, string motivo)
    {
        using var db = new AppDbContext();
        var resultado = new ResultadoRegistro();

        Visitante visitante;

        if (datos.IdVisitante == 0)
        {
            // ----- Visitante NUEVO -----
            if (db.Visitantes.Any(v => v.Documento == datos.Documento))
                throw new InvalidOperationException(
                    "Ya existe un visitante con ese documento. Búsquelo y selecciónelo de la lista.");

            visitante = new Visitante
            {
                Nombre = datos.Nombre,
                Documento = datos.Documento,
                Telefono = datos.Telefono,
                Correo = datos.Correo,
                EsPrimeraVisita = true
            };
            db.Visitantes.Add(visitante);
            resultado.EraPrimeraVisita = true;
        }
        else
        {
            // ----- Visitante RECURRENTE: solo actualizamos sus datos de contacto -----
            visitante = db.Visitantes.First(v => v.IdVisitante == datos.IdVisitante);
            visitante.Telefono = datos.Telefono;
            visitante.Correo = datos.Correo;
            visitante.EsPrimeraVisita = false;
            resultado.EraPrimeraVisita = false;
        }

        // ----- Buscar el límite que corresponde -----
        // Primera visita => límite genérico. Recurrente => límite del área (si existe) o genérico.
        LimitePermitido? limite = null;
        if (!resultado.EraPrimeraVisita)
            limite = db.Limites.FirstOrDefault(l => l.IdArea == idArea && !l.EsGenerico);
        limite ??= db.Limites.FirstOrDefault(l => l.EsGenerico);

        var ahora = DateTime.Now;

        if (limite != null)
        {
            resultado.DuracionMaxMin = limite.DuracionMaxMin;

            var horaActual = ahora.TimeOfDay;
            if (horaActual < limite.HoraInicio || horaActual > limite.HoraFin)
            {
                resultado.Advertencia =
                    $"Fuera del horario permitido ({limite.HoraInicio:hh\\:mm} - {limite.HoraFin:hh\\:mm}).";
            }
        }

        // ----- Guardar la visita -----
        var visita = new Visita
        {
            Visitante = visitante,
            IdEmpleado = idEmpleado,
            IdArea = idArea,
            Motivo = motivo,
            FechaHoraEntrada = ahora
        };
        db.Visitas.Add(visita);
        db.SaveChanges();

        resultado.IdVisita = visita.IdVisita;
        return resultado;
    }

    /// <summary>Registra la hora de salida de una visita con un solo clic (RF-06).</summary>
    public void RegistrarSalida(int idVisita)
    {
        using var db = new AppDbContext();
        var visita = db.Visitas.First(v => v.IdVisita == idVisita);

        if (visita.FechaHoraSalida == null)
        {
            visita.FechaHoraSalida = DateTime.Now;
            db.SaveChanges();
        }
    }

    /// <summary>Números para las tarjetas del menú principal: visitas de hoy, dentro ahora y del mes.</summary>
    public (int Hoy, int Dentro, int Mes) ObtenerResumen()
    {
        using var db = new AppDbContext();

        var hoy = DateTime.Today;
        var manana = hoy.AddDays(1);
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var finMes = inicioMes.AddMonths(1);

        int visitasHoy = db.Visitas.Count(v => v.FechaHoraEntrada >= hoy && v.FechaHoraEntrada < manana);
        int dentro = db.Visitas.Count(v => v.FechaHoraSalida == null);
        int delMes = db.Visitas.Count(v => v.FechaHoraEntrada >= inicioMes && v.FechaHoraEntrada < finMes);

        return (visitasHoy, dentro, delMes);
    }

    /// <summary>Visitas que todavía no tienen hora de salida.</summary>
    public List<FilaReporte> ObtenerAbiertas()
    {
        using var db = new AppDbContext();

        var visitas = db.Visitas
            .Include(v => v.Visitante)
            .Include(v => v.Empleado)
            .Include(v => v.Area)
            .Where(v => v.FechaHoraSalida == null)
            .OrderByDescending(v => v.FechaHoraEntrada)
            .ToList();

        return visitas.Select(ReporteService.AFila).ToList();
    }
}
