using VisitasRecepcion.Models;

namespace VisitasRecepcion.Data;

/// <summary>
/// Carga datos de ejemplo (áreas, empleados, visitantes, límites y visitas de los
/// últimos 45 días) para poder probar el sistema y los reportes.
/// Solo se ejecuta si todavía no hay empleados en la base de datos.
/// Para desactivarlo, ponga Config.CargarDatosEjemplo = false.
/// </summary>
public static class DatosEjemplo
{
    public static void Cargar(AppDbContext db)
    {
        // Si ya hay empleados, la base ya tiene datos: no hacemos nada
        if (db.Empleados.Any())
            return;

        // Random con semilla fija: siempre genera los mismos datos
        var rnd = new Random(2026);

        // ---------------- Áreas ----------------
        Area ObtenerArea(string nombre, string descripcion)
        {
            var area = db.Areas.FirstOrDefault(a => a.Nombre == nombre);
            if (area == null)
            {
                area = new Area { Nombre = nombre, Descripcion = descripcion };
                db.Areas.Add(area);
            }
            return area;
        }

        var gerencia = ObtenerArea("Gerencia", "Oficinas de la gerencia general");
        var rrhh = ObtenerArea("Recursos Humanos", "Selección y administración de personal");
        var contabilidad = ObtenerArea("Contabilidad", "Finanzas y contabilidad");
        var soporte = ObtenerArea("Soporte Técnico", "Tecnología e informática");
        var ventas = ObtenerArea("Ventas", "Atención a clientes y cotizaciones");

        // ---------------- Empleados (anfitriones) ----------------
        var empleados = new List<Empleado>
        {
            new Empleado { Nombre = "Ana Lucía Morales",   Cargo = "Gerente General",          Correo = "ana.morales@miempresa.com",    Area = gerencia },
            new Empleado { Nombre = "Carlos Méndez",       Cargo = "Jefe de Recursos Humanos", Correo = "carlos.mendez@miempresa.com",  Area = rrhh },
            new Empleado { Nombre = "Lucía Hernández",     Cargo = "Analista de RRHH",         Correo = "lucia.hernandez@miempresa.com",Area = rrhh },
            new Empleado { Nombre = "Roberto Castillo",    Cargo = "Contador General",         Correo = "roberto.castillo@miempresa.com",Area = contabilidad },
            new Empleado { Nombre = "María José Pérez",    Cargo = "Asistente Contable",       Correo = "mariajose.perez@miempresa.com",Area = contabilidad },
            new Empleado { Nombre = "Diego Ramírez",       Cargo = "Técnico de Soporte",       Correo = "diego.ramirez@miempresa.com",  Area = soporte },
            new Empleado { Nombre = "Sofía Villatoro",     Cargo = "Ejecutiva de Ventas",      Correo = "sofia.villatoro@miempresa.com",Area = ventas },
            new Empleado { Nombre = "Jorge López",         Cargo = "Gerente de Ventas",        Correo = "jorge.lopez@miempresa.com",    Area = ventas }
        };
        db.Empleados.AddRange(empleados);

        // ---------------- Límites permitidos por área ----------------
        db.Limites.AddRange(
            NuevoLimite(gerencia,     9, 16, 30),
            NuevoLimite(rrhh,         8, 17, 45),
            NuevoLimite(contabilidad, 8, 16, 40),
            NuevoLimite(soporte,      8, 18, 60),
            NuevoLimite(ventas,       8, 18, 60));

        // ---------------- Usuario recepcionista ----------------
        if (!db.Usuarios.Any(u => u.Login == "recepcion"))
        {
            db.Usuarios.Add(new Usuario
            {
                Login = "recepcion",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("recepcion123"),
                Rol = "Recepcionista"
            });
        }

        // ---------------- Visitantes (datos ficticios) ----------------
        var visitantes = new List<Visitante>
        {
            NuevoVisitante("Pedro Alvarado",   "2543178900101", "5512 3456", "pedro.alvarado@correo.com"),
            NuevoVisitante("Karla Ixcoy",      "2610445210402", "4234 7788", "karla.ixcoy@correo.com"),
            NuevoVisitante("Luis Tzoc",        "1987654320401", "5890 1122", "luis.tzoc@correo.com"),
            NuevoVisitante("Andrea Cifuentes", "2345678901301", "3345 6677", "andrea.cifuentes@correo.com"),
            NuevoVisitante("Marvin Colop",     "2278901234502", "4100 2299", "marvin.colop@correo.com"),
            NuevoVisitante("Silvia Batz",      "2456123780403", "5566 0011", "silvia.batz@correo.com"),
            NuevoVisitante("Héctor Xicay",     "2098765431204", "3021 8844", "hector.xicay@correo.com"),
            NuevoVisitante("Brenda Coyoy",     "2567890123405", "5733 9900", "brenda.coyoy@correo.com"),
            NuevoVisitante("Julio Chivalán",   "2189012345606", "4455 1313", "julio.chivalan@correo.com"),
            NuevoVisitante("Evelyn Sirín",     "2334455667807", "5099 4242", "evelyn.sirin@correo.com"),
            NuevoVisitante("Fernando Tohom",   "2401122334408", "3188 7070", "fernando.tohom@correo.com"),
            NuevoVisitante("Gabriela Yac",     "2512233445509", "4677 5050", "gabriela.yac@correo.com")
        };
        db.Visitantes.AddRange(visitantes);

        string[] motivos =
        {
            "Reunión de negocios", "Entrega de documentos", "Entrevista de trabajo",
            "Cotización de servicios", "Soporte técnico", "Visita de proveedor",
            "Auditoría", "Cita con gerencia"
        };

        // ---------------- Visitas históricas (últimos 45 días, solo lunes a viernes) ----------------
        var visitas = new List<Visita>();

        for (int d = 45; d >= 1; d--)
        {
            var dia = DateTime.Today.AddDays(-d);
            if (dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday)
                continue;

            int cantidad = rnd.Next(2, 6);   // entre 2 y 5 visitas por día
            for (int i = 0; i < cantidad; i++)
            {
                var visitante = visitantes[rnd.Next(visitantes.Count)];
                var empleado = empleados[rnd.Next(empleados.Count)];
                var entrada = dia.AddHours(8).AddMinutes(rnd.Next(0, 8 * 60 + 1));   // entre 8:00 y 16:00

                visitas.Add(new Visita
                {
                    Visitante = visitante,
                    Empleado = empleado,
                    Area = empleado.Area,
                    Motivo = motivos[rnd.Next(motivos.Length)],
                    FechaHoraEntrada = entrada,
                    FechaHoraSalida = entrada.AddMinutes(rnd.Next(10, 76)),
                    NotificacionEnviada = true
                });
            }
        }

        // ---------------- Visitas de hoy, todavía dentro de la empresa ----------------
        var ahora = DateTime.Now;
        visitas.Add(NuevaVisitaAbierta(visitantes[0], empleados[0], "Cita con gerencia", ahora.AddMinutes(-15), true));
        visitas.Add(NuevaVisitaAbierta(visitantes[3], empleados[3], "Auditoría", ahora.AddMinutes(-40), true));
        visitas.Add(NuevaVisitaAbierta(visitantes[7], empleados[6], "Cotización de servicios", ahora.AddMinutes(-70), false));

        db.Visitas.AddRange(visitas);

        // ---------------- Visitantes recurrentes vs. de primera vez ----------------
        foreach (var v in visitantes)
        {
            int total = visitas.Count(x => x.Visitante == v);
            v.EsPrimeraVisita = total <= 1;
        }

        // ---------------- Bitácora de notificaciones ----------------
        int contador = 0;
        foreach (var visita in visitas)
        {
            contador++;

            // Una de cada 15 visitas "fallo" el correo, para ver errores en la bitácora
            bool fallo = !visita.NotificacionEnviada || contador % 15 == 0;
            if (fallo)
                visita.NotificacionEnviada = false;

            db.LogsNotificacion.Add(new LogNotificacion
            {
                Visita = visita,
                FechaEnvio = visita.FechaHoraEntrada.AddSeconds(2),
                Estado = fallo ? "Error" : "Enviado",
                Detalle = fallo ? "No se pudo conectar con el servidor SMTP (dato de ejemplo)." : ""
            });
        }

        db.SaveChanges();
    }

    // ======== Métodos auxiliares para no repetir código ========

    private static LimitePermitido NuevoLimite(Area area, int horaInicio, int horaFin, int duracionMin)
    {
        return new LimitePermitido
        {
            Area = area,
            EsGenerico = false,
            HoraInicio = TimeSpan.FromHours(horaInicio),
            HoraFin = TimeSpan.FromHours(horaFin),
            DuracionMaxMin = duracionMin
        };
    }

    private static Visitante NuevoVisitante(string nombre, string documento, string telefono, string correo)
    {
        return new Visitante
        {
            Nombre = nombre,
            Documento = documento,
            Telefono = telefono,
            Correo = correo,
            EsPrimeraVisita = true
        };
    }

    private static Visita NuevaVisitaAbierta(Visitante visitante, Empleado empleado, string motivo, DateTime entrada, bool notificada)
    {
        return new Visita
        {
            Visitante = visitante,
            Empleado = empleado,
            Area = empleado.Area,
            Motivo = motivo,
            FechaHoraEntrada = entrada,
            FechaHoraSalida = null,          // sigue dentro de la empresa
            NotificacionEnviada = notificada
        };
    }
}
