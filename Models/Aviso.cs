namespace VisitasRecepcion.Models;

/// <summary>Tipos de aviso. Se guardan como texto en Aviso.Tipo.</summary>
public static class TiposAviso
{
    public const string Llegada = "Llegada";                   // el visitante llegó a recepción
    public const string TiempoTerminado = "TiempoTerminado";   // se acabó el tiempo permitido de la visita
}

/// <summary>
/// Aviso interno para el anfitrión: se crea al registrar la visita (y cuando se le acaba
/// el tiempo) y el anfitrión lo ve en la pantalla "Avisos". No usa correo, todo queda en SQL Server.
/// </summary>
public class Aviso
{
    public int IdAviso { get; set; }

    public int IdVisita { get; set; }
    public Visita? Visita { get; set; }

    public int IdEmpleado { get; set; }        // anfitrión que debe leer el aviso
    public Empleado? Empleado { get; set; }

    public string Tipo { get; set; } = TiposAviso.Llegada;
    public string Mensaje { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public bool Leido { get; set; }
    public DateTime? FechaLectura { get; set; }   // null mientras no se haya leído
}
