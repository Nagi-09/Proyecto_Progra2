namespace VisitasRecepcion.Models;

/// <summary>Cuenta de acceso al sistema (recepcionista o administrador).</summary>
public class Usuario
{
    public int IdUsuario { get; set; }
    public string Login { get; set; } = "";

    // IMPORTANTE: aquí se guarda el HASH de la contraseña (BCrypt), nunca el texto original.
    public string PasswordHash { get; set; } = "";

    public string Rol { get; set; } = "Recepcionista";   // "Recepcionista" o "Administrador"
    public bool Activo { get; set; } = true;
}
