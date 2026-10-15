using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>Reporte de visitas por día / semana / mes, con DateTimePicker y exportación a Excel.</summary>
public class ReporteForm : Form
{
    private readonly ReporteService _reporteService = new();
    private readonly ExcelService _excelService = new();

    private readonly ComboBox cmbVista = new();
    private readonly DateTimePicker dtpFecha = new();
    private readonly Button btnExportar = new();
    private readonly Label lblResumen = new();
    private readonly DataGridView dgvReporte = new();

    // Aquí guardamos lo que se está mostrando, para exportarlo a Excel
    private List<FilaReporte> _filasActuales = new();

    public ReporteForm()
    {
        Text = "Reporte de visitas";
        ClientSize = new Size(1040, 640);
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterParent;
        Estilo.Formulario(this);

        Controls.Add(Estilo.Encabezado("Reporte de visitas",
            "Consulte las visitas por día, semana o mes y expórtelas a Excel"));

        // ---------- Barra de filtros ----------
        var barra = Estilo.Tarjeta(20, 100, 1000, 84);
        barra.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        barra.Controls.Add(Estilo.Caption("Vista", 20, 14));
        cmbVista.SetBounds(20, 36, 150, 28);
        cmbVista.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbVista.Items.AddRange(new object[] { "Día", "Semana", "Mes" });
        cmbVista.SelectedIndex = 0;

        barra.Controls.Add(Estilo.Caption("Fecha", 190, 14));
        dtpFecha.SetBounds(190, 36, 160, 28);
        dtpFecha.Format = DateTimePickerFormat.Short;
        dtpFecha.Value = DateTime.Today;

        lblResumen.SetBounds(380, 16, 410, 54);
        lblResumen.AutoSize = false;
        lblResumen.TextAlign = ContentAlignment.MiddleLeft;
        lblResumen.Font = Estilo.Negrita;
        lblResumen.ForeColor = Estilo.Texto;

        btnExportar.Text = "Exportar a Excel";
        btnExportar.SetBounds(810, 22, 170, 40);
        btnExportar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Estilo.Boton(btnExportar, Estilo.Acento);
        btnExportar.Click += BtnExportar_Click;

        barra.Controls.AddRange(new Control[] { cmbVista, dtpFecha, lblResumen, btnExportar });

        // ---------- Tabla ----------
        var tarjetaTabla = Estilo.Tarjeta(20, 200, 1000, 420);
        tarjetaTabla.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tarjetaTabla.Padding = new Padding(1);

        dgvReporte.Dock = DockStyle.Fill;
        Estilo.Grid(dgvReporte);
        dgvReporte.CellFormatting += (s, e) => Estilo.FormatoCeldas(dgvReporte, e);
        tarjetaTabla.Controls.Add(dgvReporte);

        Controls.AddRange(new Control[] { barra, tarjetaTabla });

        Estilo.Campos(this);

        // Los eventos se conectan al final para que no se disparen mientras se arma la pantalla
        cmbVista.SelectedIndexChanged += (s, e) => CargarReporte();
        dtpFecha.ValueChanged += (s, e) => CargarReporte();

        CargarReporte();
    }

    private VistaReporte VistaSeleccionada()
    {
        return cmbVista.SelectedIndex switch
        {
            1 => VistaReporte.Semana,
            2 => VistaReporte.Mes,
            _ => VistaReporte.Dia
        };
    }

    private void CargarReporte()
    {
        var vista = VistaSeleccionada();
        _filasActuales = _reporteService.ObtenerVisitas(dtpFecha.Value, vista);

        dgvReporte.DataSource = _filasActuales;
        if (dgvReporte.Columns["IdVisita"] != null)
            dgvReporte.Columns["IdVisita"]!.Visible = false;

        var (desde, hasta) = ReporteService.CalcularRango(dtpFecha.Value, vista);
        int sinSalida = _filasActuales.Count(f => f.Salida == "—");

        lblResumen.Text =
            $"Del {desde:dd/MM/yyyy} al {hasta.AddDays(-1):dd/MM/yyyy}" + Environment.NewLine +
            $"Total: {_filasActuales.Count} visitas   ·   Sin salida: {sinSalida}";
    }

    private void BtnExportar_Click(object? sender, EventArgs e)
    {
        if (_filasActuales.Count == 0)
        {
            MessageBox.Show("No hay datos para exportar.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialogo = new SaveFileDialog
        {
            Filter = "Archivo de Excel (*.xlsx)|*.xlsx",
            FileName = $"Reporte_Visitas_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (dialogo.ShowDialog() == DialogResult.OK)
        {
            try
            {
                _excelService.Exportar(_filasActuales, dialogo.FileName);
                MessageBox.Show("Archivo exportado correctamente.", "Listo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                // Por ejemplo, si el archivo está abierto en Excel
                MessageBox.Show("No se pudo exportar:\n" + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
