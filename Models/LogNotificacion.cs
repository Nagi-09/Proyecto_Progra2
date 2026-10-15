namespace VisitasRecepcion.Models;

/// <summary>Bitácora de los correos enviados a los anfitriones (sirve para auditar fallos).</summary>
public class LogNotificacion
{
    public int IdLog { get; set; }

    public int IdVisita { get; set; }
    public Visita? Visita { get; set; }

    public DateTime FechaEnvio { get; set; }
    public string Estado { get; set; } = "";    // "Enviado" o "Error"
    public string Detalle { get; set; } = "";   // mensaje de error si falló
}
