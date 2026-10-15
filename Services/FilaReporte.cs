namespace VisitasRecepcion.Services;

/// <summary>
/// Una fila "aplanada" de una visita, lista para mostrarse en un DataGridView
/// y para exportarse a Excel. (Es un DTO: solo guarda datos para mostrar.)
/// </summary>
public class FilaReporte
{
    public int IdVisita { get; set; }          // se oculta en la grilla
    public string Visitante { get; set; } = "";
    public string Documento { get; set; } = "";
    public string Anfitrion { get; set; } = "";
    public string Area { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Entrada { get; set; } = "";
    public string Salida { get; set; } = "";
    public string DuracionMin { get; set; } = "";
    public string Notificado { get; set; } = "";
}
