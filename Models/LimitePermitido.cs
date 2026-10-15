namespace VisitasRecepcion.Models;

/// <summary>
/// Regla de negocio: horario permitido y duración máxima de una visita.
/// Si EsGenerico = true, aplica a visitantes nuevos (IdArea queda en null).
/// </summary>
public class LimitePermitido
{
    public int IdLimite { get; set; }

    public int? IdArea { get; set; }          // null para el límite genérico
    public Area? Area { get; set; }

    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFin { get; set; }
    public int DuracionMaxMin { get; set; }   // duración máxima en minutos
    public bool EsGenerico { get; set; }
}
