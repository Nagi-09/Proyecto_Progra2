namespace VisitasRecepcion.Models;

/// <summary>Personal interno que puede ser anfitrión de una visita.</summary>
public class Empleado
{
    public int IdEmpleado { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Cargo { get; set; } = "";
    public bool Activo { get; set; } = true;

    // El área se guarda como llave foránea (así la BD queda normalizada a 3FN)
    public int IdArea { get; set; }
    public Area? Area { get; set; }

    public List<Visita> Visitas { get; set; } = new();
    public List<Aviso> Avisos { get; set; } = new();

    public override string ToString() => Nombre;
}
