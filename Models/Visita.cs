namespace VisitasRecepcion.Models;

/// <summary>Registro de cada ingreso/salida de un visitante.</summary>
public class Visita
{
    public int IdVisita { get; set; }

    public int IdVisitante { get; set; }
    public Visitante? Visitante { get; set; }

    public int IdEmpleado { get; set; }      // anfitrión
    public Empleado? Empleado { get; set; }

    public int IdArea { get; set; }
    public Area? Area { get; set; }

    public DateTime FechaHoraEntrada { get; set; }
    public DateTime? FechaHoraSalida { get; set; }   // null = visita "abierta"
    public string Motivo { get; set; } = "";
    public bool NotificacionEnviada { get; set; }   // true cuando ya se generó el aviso al anfitrión

    // Tiempo permitido (según el límite que aplicó al registrarla). Al pasar, se avisa al anfitrión.
    public int DuracionMaxMin { get; set; } = Config.DuracionGenericaMin;
    public DateTime HoraLimite => FechaHoraEntrada.AddMinutes(DuracionMaxMin);

    public List<Aviso> Avisos { get; set; } = new();
}
