using VisitasRecepcion.Data;
using VisitasRecepcion.Forms;

namespace VisitasRecepcion;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // 1) Crear la base de datos (si no existe) y los datos iniciales
        try
        {
            AppDbContext.Inicializar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "No se pudo conectar con SQL Server Express.\n\n" +
                "Revise la cadena de conexión en Config.cs y que SQL Server esté en ejecución.\n\n" +
                "Detalle: " + ex.Message,
                "Error de base de datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // 2) Mostrar el login; si es correcto, abrir la ventana principal
        using var login = new LoginForm();
        if (login.ShowDialog() == DialogResult.OK && login.UsuarioAutenticado != null)
        {
            Application.Run(new MainForm(login.UsuarioAutenticado));
        }
    }
}
