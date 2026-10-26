using System.Text.RegularExpressions;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Services;

/// <summary>
/// Reglas de los datos del visitante. Las usa el formulario (para avisar al usuario)
/// y VisitaService (para que nunca se guarde un dato inválido).
/// </summary>
public static class ValidacionVisitante
{
    // Qué caracteres se pueden escribir en cada campo (el formulario los usa para filtrar el teclado)
    public static bool CaracterNombre(char c) => char.IsLetter(c) || c == ' ';
    public static bool CaracterNumero(char c) => char.IsDigit(c);
    public static bool CaracterCorreo(char c) => !char.IsWhiteSpace(c);

    // texto@dominio.algo, sin espacios
    private static readonly Regex FormatoCorreo = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    /// <summary>
    /// Revisa los datos y devuelve la lista de errores (vacía si todo está bien).
    /// Cada error indica el campo para que el formulario pueda ponerle el foco.
    /// </summary>
    public static List<(string Campo, string Mensaje)> Validar(Visitante v)
    {
        var errores = new List<(string Campo, string Mensaje)>();

        if (string.IsNullOrWhiteSpace(v.Nombre))
            errores.Add(("Nombre", "El nombre es obligatorio."));
        else if (!v.Nombre.All(CaracterNombre))
            errores.Add(("Nombre", "El nombre solo puede tener letras y espacios."));

        if (string.IsNullOrWhiteSpace(v.Documento))
            errores.Add(("Documento", "El documento es obligatorio."));
        else if (!v.Documento.All(CaracterNumero) || v.Documento.Length != Config.LargoDocumento)
            errores.Add(("Documento", $"El documento debe tener {Config.LargoDocumento} números (DPI)."));

        if (string.IsNullOrWhiteSpace(v.Telefono))
            errores.Add(("Telefono", "El teléfono es obligatorio."));
        else if (!v.Telefono.All(CaracterNumero) || v.Telefono.Length != Config.LargoTelefono)
            errores.Add(("Telefono", $"El teléfono debe tener {Config.LargoTelefono} números."));

        if (string.IsNullOrWhiteSpace(v.Correo))
            errores.Add(("Correo", "El correo es obligatorio."));
        else if (!FormatoCorreo.IsMatch(v.Correo))
            errores.Add(("Correo", "El correo no es válido (ejemplo: nombre@correo.com)."));

        return errores;
    }
}
