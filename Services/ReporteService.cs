using Microsoft.EntityFrameworkCore;
using VisitasRecepcion.Data;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Services;

public enum VistaReporte
{
    Dia,
    Semana,
    Mes
}

/// <summary>Genera los datos del reporte de visitas (RF-07 y RF-08).</summary>
public class ReporteService
{
    /// <summary>Calcula desde/hasta según la vista elegida y la fecha del DateTimePicker.</summary>
    public static (DateTime Desde, DateTime Hasta) CalcularRango(DateTime fecha, VistaReporte vista)
    {
        DateTime desde, hasta;

        switch (vista)
        {
            case VistaReporte.Dia:
                desde = fecha.Date;
                hasta = desde.AddDays(1);
                break;

            case VistaReporte.Semana:
                // La semana empieza el lunes
                int diasDesdeLunes = ((int)fecha.DayOfWeek + 6) % 7;
                desde = fecha.Date.AddDays(-diasDesdeLunes);
                hasta = desde.AddDays(7);
                break;

            default: // Mes
                desde = new DateTime(fecha.Year, fecha.Month, 1);
                hasta = desde.AddMonths(1);
                break;
        }

        return (desde, hasta);
    }

    /// <summary>Devuelve las visitas del rango [desde, hasta).</summary>
    public List<FilaReporte> ObtenerVisitas(DateTime fecha, VistaReporte vista)
    {
        var (desde, hasta) = CalcularRango(fecha, vista);

        using var db = new AppDbContext();

        var visitas = db.Visitas
            .Include(v => v.Visitante)
            .Include(v => v.Empleado)
            .Include(v => v.Area)
            .Include(v => v.Avisos)
            .Where(v => v.FechaHoraEntrada >= desde && v.FechaHoraEntrada < hasta)
            .OrderBy(v => v.FechaHoraEntrada)
            .ToList();

        return visitas.Select(AFila).ToList();
    }

    /// <summary>Convierte una Visita (entidad) en una FilaReporte (para mostrar).</summary>
    public static FilaReporte AFila(Visita v)
    {
        string duracion = "";
        if (v.FechaHoraSalida != null)
        {
            var minutos = (v.FechaHoraSalida.Value - v.FechaHoraEntrada).TotalMinutes;
            duracion = Math.Round(minutos).ToString();
        }

        return new FilaReporte
        {
            IdVisita = v.IdVisita,
            Visitante = v.Visitante?.Nombre ?? "",
            Documento = v.Visitante?.Documento ?? "",
            Anfitrion = v.Empleado?.Nombre ?? "",
            Area = v.Area?.Nombre ?? "",
            Motivo = v.Motivo,
            Entrada = v.FechaHoraEntrada.ToString("dd/MM/yyyy HH:mm"),
            Salida = v.FechaHoraSalida?.ToString("dd/MM/yyyy HH:mm") ?? "—",
            DuracionMin = duracion,
            Aviso = EstadoAviso(v)
        };
    }

    /// <summary>Estado del aviso al anfitrión: si se generó y si ya lo leyó.</summary>
    private static string EstadoAviso(Visita v)
    {
        // Solo cuenta el aviso de llegada (el de "tiempo terminado" es aparte)
        var llegada = v.Avisos.Where(a => a.Tipo == TiposAviso.Llegada).ToList();
        if (llegada.Count == 0) return "No enviado";
        return llegada.Any(a => a.Leido) ? "Leído" : "Sin leer";
    }
}
