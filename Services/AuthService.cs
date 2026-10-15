using VisitasRecepcion.Data;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Services;

/// <summary>Maneja el inicio de sesión y la creación de usuarios.</summary>
public class AuthService
{
    /// <summary>Devuelve el usuario si el login y la contraseña son correctos; si no, null.</summary>
    public Usuario? Login(string login, string password)
    {
        using var db = new AppDbContext();

        var usuario = db.Usuarios.FirstOrDefault(u => u.Login == login && u.Activo);
        if (usuario == null)
            return null;

        // BCrypt compara la contraseña escrita contra el hash guardado
        bool correcta = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        return correcta ? usuario : null;
    }

    /// <summary>Crea un usuario nuevo guardando el hash de la contraseña.</summary>
    public void CrearUsuario(string login, string password, string rol)
    {
        using var db = new AppDbContext();

        if (db.Usuarios.Any(u => u.Login == login))
            throw new InvalidOperationException("Ya existe un usuario con ese login.");

        db.Usuarios.Add(new Usuario
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = rol
        });
        db.SaveChanges();
    }
}
