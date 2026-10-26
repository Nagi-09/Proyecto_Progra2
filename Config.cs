namespace VisitasRecepcion;

/// <summary>
/// Configuración general del sistema.
/// Cambien estos valores según su computadora.
/// Solo se necesita SQL Server: los avisos al anfitrión son internos (sin correo).
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

    // ---------- Avisos internos ----------
    // Cada cuántos segundos la pantalla de avisos busca avisos nuevos.
    public const int SegundosActualizarAvisos = 15;

    // ---------- Datos del visitante ----------
    public const int LargoDocumento = 13;   // DPI: 13 números
    public const int LargoTelefono = 8;     // teléfono de Guatemala: 8 números

    // ---------- Valores por defecto ----------
    public const int DuracionGenericaMin = 60;   // límite genérico: 60 minutos
    public const string HoraInicioGenerica = "08:00";
    public const string HoraFinGenerica = "17:00";
}
