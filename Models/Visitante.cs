namespace VisitasRecepcion.Models;

/// <summary>Persona externa que visita la empresa.</summary>
public class Visitante
{
    public int IdVisitante { get; set; }
    public string Nombre { get; set; } = "";
    public string Documento { get; set; } = "";   // DPI, pasaporte, etc.
    public string Telefono { get; set; } = "";
    public string Correo { get; set; } = "";

    // true mientras solo tenga su primera visita; pasa a false cuando regresa
    public bool EsPrimeraVisita { get; set; } = true;

    // 1 visitante -> N visitas (recurrencia)
    public List<Visita> Visitas { get; set; } = new();

    // Así se muestra en el ListBox de búsqueda
    public override string ToString() => $"{Nombre}  ({Documento})";
}
