namespace VisitasRecepcion.Models;

/// <summary>Zona o departamento de la empresa al que puede dirigirse un visitante.</summary>
public class Area
{
    public int IdArea { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool Activo { get; set; } = true;   // "baja lógica": no se borra, solo se desactiva

    // Relaciones (1 área -> N empleados, N visitas, N límites)
    public List<Empleado> Empleados { get; set; } = new();
    public List<Visita> Visitas { get; set; } = new();
    public List<LimitePermitido> Limites { get; set; } = new();

    public override string ToString() => Nombre;
}
