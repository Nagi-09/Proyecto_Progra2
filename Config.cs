namespace VisitasRecepcion;

/// <summary>
/// Configuración general del sistema.
/// Cambien estos valores según su computadora / servidor de correo.
/// </summary>
public static class Config
{
    // ---------- Base de datos ----------
    // SQL Server instalado en la misma PC (instancia predeterminada).
    public const string CadenaConexion =
        "Server=localhost;Database=VisitasRecepcionDB;Trusted_Connection=True;TrustServerCertificate=True;";

    // true = al crear la base de datos se cargan datos de ejemplo (áreas, empleados, visitas...).
    // Pónganlo en false para entregar el sistema con la base vacía.
    public const bool CargarDatosEjemplo = true;

    // ---------- Correo (SMTP) ----------
    // Para pruebas pueden usar un servidor SMTP local (por ejemplo "smtp4dev")
    // o el SMTP de Gmail/Outlook con una contraseña de aplicación.
    public const string SmtpServidor = "localhost";
    public const int SmtpPuerto = 25;
    public const bool SmtpUsarSsl = false;
    public const string SmtpUsuario = "";     // dejar vacío si no requiere autenticación
    public const string SmtpPassword = "";
    public const string CorreoRemitente = "recepcion@miempresa.com";

    // ---------- Valores por defecto ----------
    public const int DuracionGenericaMin = 60;   // límite genérico: 60 minutos
    public const string HoraInicioGenerica = "08:00";
    public const string HoraFinGenerica = "17:00";
}
