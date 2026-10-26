namespace VisitasRecepcion.Models;

/// <summary>Roles del sistema. Se guardan como texto en Usuario.Rol.</summary>
public static class Roles
{
    public const string Administrador = "Administrador";   // todo, incluida la administración
    public const string Recepcionista = "Recepcionista";   // registro, avisos y reportes
    public const string Trabajador = "Trabajador";         // un empleado: solo ve sus propios avisos

    public static readonly string[] Todos = { Recepcionista, Trabajador, Administrador };
}

/// <summary>Cuenta de acceso al sistema (administrador, recepcionista o trabajador).</summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Login { get; set; } = "";

    // IMPORTANTE: aquí se guarda el HASH de la contraseña (BCrypt), nunca el texto original.
    public string PasswordHash { get; set; } = "";

    public string Rol { get; set; } = Roles.Recepcionista;
    public bool Activo { get; set; } = true;

    // Solo para el rol Trabajador: el empleado (anfitrión) al que pertenece esta cuenta
    public int? IdEmpleado { get; set; }
    public Empleado? Empleado { get; set; }

    public bool EsAdministrador => Rol == Roles.Administrador;
    public bool EsTrabajador => Rol == Roles.Trabajador;
}
