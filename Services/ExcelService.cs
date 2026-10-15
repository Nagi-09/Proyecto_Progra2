using ClosedXML.Excel;

namespace VisitasRecepcion.Services;

/// <summary>Exporta el reporte a un archivo Excel .xlsx usando ClosedXML (RF-09).</summary>
public class ExcelService
{
    public void Exportar(List<FilaReporte> filas, string rutaArchivo)
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Visitas");

        // Encabezados
        string[] titulos =
        {
            "Visitante", "Documento", "Anfitrión", "Área", "Motivo",
            "Entrada", "Salida", "Duración (min)", "Notificado"
        };
        for (int i = 0; i < titulos.Length; i++)
            hoja.Cell(1, i + 1).Value = titulos[i];

        // Estilo del encabezado
        var encabezado = hoja.Range(1, 1, 1, titulos.Length);
        encabezado.Style.Font.Bold = true;
        encabezado.Style.Fill.BackgroundColor = XLColor.LightSteelBlue;

        // Datos (fila 2 en adelante)
        int fila = 2;
        foreach (var f in filas)
        {
            hoja.Cell(fila, 1).Value = f.Visitante;
            hoja.Cell(fila, 2).Value = f.Documento;
            hoja.Cell(fila, 3).Value = f.Anfitrion;
            hoja.Cell(fila, 4).Value = f.Area;
            hoja.Cell(fila, 5).Value = f.Motivo;
            hoja.Cell(fila, 6).Value = f.Entrada;
            hoja.Cell(fila, 7).Value = f.Salida;
            hoja.Cell(fila, 8).Value = f.DuracionMin;
            hoja.Cell(fila, 9).Value = f.Notificado;
            fila++;
        }

        hoja.Columns().AdjustToContents();
        libro.SaveAs(rutaArchivo);
    }
}
