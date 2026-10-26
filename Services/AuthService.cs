using Microsoft.EntityFrameworkCore;
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

        var usuario = db.Usuarios
            .Include(u => u.Empleado)
            .FirstOrDefault(u => u.Login == login && u.Activo);
        if (usuario == null)
            return null;

        // Un trabajador solo entra si su empleado existe y sigue activo
        if (usuario.EsTrabajador && (usuario.Empleado == null || !usuario.Empleado.Activo))
            return null;

        // BCrypt compara la contraseña escrita contra el hash guardado
        bool correcta = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        return correcta ? usuario : null;
    }

    /// <summary>
    /// Crea un usuario nuevo guardando el hash de la contraseña.
    /// Si el rol es Trabajador, idEmpleado indica a qué empleado pertenece la cuenta.
    /// </summary>
    public void CrearUsuario(string login, string password, string rol, int? idEmpleado = null)
    {
        using var db = new AppDbContext();

        if (!Roles.Todos.Contains(rol))
            throw new InvalidOperationException("Rol no válido.");

        if (db.Usuarios.Any(u => u.Login == login))
            throw new InvalidOperationException("Ya existe un usuario con ese login.");

        if (rol == Roles.Trabajador)
        {
            if (idEmpleado == null)
                throw new InvalidOperationException("Seleccione el empleado al que pertenece el usuario.");

            if (!db.Empleados.Any(e => e.IdEmpleado == idEmpleado && e.Activo))
                throw new InvalidOperationException("El empleado no existe o está dado de baja.");

            if (db.Usuarios.Any(u => u.Activo && u.IdEmpleado == idEmpleado))
                throw new InvalidOperationException("Ese empleado ya tiene un usuario.");
        }
        else
        {
            idEmpleado = null;   // solo los trabajadores se ligan a un empleado
        }

        db.Usuarios.Add(new Usuario
        {
            Login = login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Rol = rol,
            IdEmpleado = idEmpleado
        });
        db.SaveChanges();
    }
}
