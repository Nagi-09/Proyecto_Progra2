using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>
/// Pantalla de avisos del anfitrión: el anfitrión elige su nombre y ve
/// quién lo está esperando en recepción. Se actualiza sola cada pocos segundos.
/// </summary>
public class AvisosForm : Form
{
    private readonly NotificacionService _notificacionService = new();

    private readonly ComboBox cmbAnfitrion = new();
    private readonly CheckBox chkSoloSinLeer = new();
    private readonly Label lblResumen = new();
    private readonly Button btnLeido = new();
    private readonly Button btnTodos = new();
    private readonly Button btnTerminar = new();
    private readonly DataGridView dgvAvisos = new();
    private readonly Label lblVacio = new();
    private readonly System.Windows.Forms.Timer timer = new();

    // Evita que el combo recargue la tabla mientras lo estamos llenando nosotros
    private bool _cargandoCombo;

    // Rol Trabajador: solo puede ver los avisos de su propio empleado (null = puede ver todos)
    private readonly int? _soloEmpleado;

    public AvisosForm(int? soloEmpleado = null)
    {
        _soloEmpleado = soloEmpleado;

        Text = soloEmpleado == null ? "Avisos del anfitrión" : "Mis avisos";
        ClientSize = new Size(1100, 620);
        MinimumSize = new Size(960, 560);
        StartPosition = FormStartPosition.CenterParent;
        Estilo.Formulario(this);

        Controls.Add(soloEmpleado == null
            ? Estilo.Encabezado("Avisos del anfitrión", "Seleccione su nombre para ver quién lo espera en recepción")
            : Estilo.Encabezado("Mis avisos", "Quién lo espera en recepción y cuándo termina cada visita"));

        // ---------- Barra de filtros y acciones ----------
        var barra = Estilo.Tarjeta(20, 100, 1060, 84);
        barra.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        barra.Controls.Add(Estilo.Caption("Anfitrión", 20, 14));
        cmbAnfitrion.SetBounds(20, 36, 300, 28);
        cmbAnfitrion.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbAnfitrion.Enabled = soloEmpleado == null;   // el trabajador no puede elegir a otro

        chkSoloSinLeer.Text = "Solo sin leer";
        chkSoloSinLeer.SetBounds(340, 38, 130, 26);
        chkSoloSinLeer.Font = Estilo.Normal;
        chkSoloSinLeer.ForeColor = Estilo.Texto;
        chkSoloSinLeer.BackColor = Color.Transparent;
        chkSoloSinLeer.Cursor = Cursors.Hand;
        chkSoloSinLeer.Checked = true;

        lblResumen.SetBounds(480, 16, 150, 54);
        lblResumen.AutoSize = false;
        lblResumen.TextAlign = ContentAlignment.MiddleLeft;
        lblResumen.Font = Estilo.Negrita;
        lblResumen.ForeColor = Estilo.Texto;
        lblResumen.BackColor = Color.Transparent;

        btnLeido.Text = "Marcar como leído";
        btnLeido.SetBounds(740, 22, 140, 40);
        btnLeido.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Estilo.Boton(btnLeido, Estilo.Acento);
        btnLeido.Click += BtnLeido_Click;

        btnTodos.Text = "Marcar todos";
        btnTodos.SetBounds(890, 22, 150, 40);
        btnTodos.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Estilo.BotonSecundario(btnTodos);
        btnTodos.Click += BtnTodos_Click;

        barra.Controls.AddRange(new Control[] { cmbAnfitrion, chkSoloSinLeer, lblResumen, btnLeido, btnTodos });

        // ---------- Tabla de avisos ----------
        var tarjetaTabla = Estilo.Tarjeta(20, 200, 1060, 356);
        tarjetaTabla.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        tarjetaTabla.Padding = new Padding(1);

        dgvAvisos.Dock = DockStyle.Fill;
        Estilo.Grid(dgvAvisos);
        dgvAvisos.CellFormatting += (s, e) => Estilo.FormatoCeldas(dgvAvisos, e);
        dgvAvisos.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnLeido_Click(s, e); };
        dgvAvisos.SelectionChanged += (s, e) => btnTerminar.Enabled = FilaSeleccionada()?.EnCurso == true;

        // Mensaje que se muestra cuando no hay avisos
        lblVacio.Dock = DockStyle.Fill;
        lblVacio.TextAlign = ContentAlignment.MiddleCenter;
        lblVacio.Font = Estilo.Seccion;
        lblVacio.ForeColor = Estilo.TextoSuave;
        lblVacio.BackColor = Estilo.Blanco;
        lblVacio.Visible = false;

        tarjetaTabla.Controls.Add(dgvAvisos);
        tarjetaTabla.Controls.Add(lblVacio);
        lblVacio.BringToFront();

        Controls.AddRange(new Control[] { barra, tarjetaTabla });

        // ---------- Pie: ayuda y botón para terminar la visita ----------
        Controls.Add(new Label
        {
            Text = "Doble clic en un aviso para marcarlo como leído.  " +
                   "Si el visitante ya se tiene que ir, selecciónelo y presione \"Terminar visita\".",
            Font = Estilo.Pequena,
            ForeColor = Estilo.TextoSuave,
            AutoSize = true,
            Left = 20,
            Top = 580,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        });

        btnTerminar.Text = "Terminar visita";
        btnTerminar.SetBounds(900, 568, 180, 38);
        btnTerminar.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        Estilo.Boton(btnTerminar, Estilo.Peligro);
        btnTerminar.Enabled = false;
        btnTerminar.Click += BtnTerminar_Click;
        Controls.Add(btnTerminar);

        Estilo.Campos(this);

        // Los eventos se conectan al final para que no se disparen mientras se arma la pantalla
        cmbAnfitrion.SelectedIndexChanged += (s, e) => { if (!_cargandoCombo) CargarAvisos(); };
        chkSoloSinLeer.CheckedChanged += (s, e) => CargarAvisos();

        // Revisa si llegaron avisos nuevos cada cierto tiempo (Config.SegundosActualizarAvisos)
        timer.Interval = Config.SegundosActualizarAvisos * 1000;
        timer.Tick += (s, e) => Actualizar();
        timer.Start();
        FormClosed += (s, e) => timer.Dispose();

        CargarAnfitriones();
        CargarAvisos();
        Estilo.AjustarAPantalla(this);
    }

    private int? AnfitrionSeleccionado() =>
        (cmbAnfitrion.SelectedItem as OpcionAnfitrion)?.IdEmpleado;

    // ======================= CARGA DE DATOS =======================
    /// <summary>Llena el combo (con los contadores al día) sin perder la selección actual.</summary>
    private void CargarAnfitriones()
    {
        int? seleccionado = AnfitrionSeleccionado();
        var lista = _notificacionService.ObtenerAnfitriones(_soloEmpleado);

        _cargandoCombo = true;
        cmbAnfitrion.DataSource = lista;
        var anterior = lista.FirstOrDefault(a => a.IdEmpleado == seleccionado);
        cmbAnfitrion.SelectedItem = anterior ?? lista.FirstOrDefault();
        _cargandoCombo = false;
    }

    private void CargarAvisos()
    {
        int? idEmpleado = AnfitrionSeleccionado();
        if (idEmpleado == null)
        {
            dgvAvisos.DataSource = null;
            MostrarVacio("No hay anfitriones registrados.");
            lblResumen.Text = "";
            return;
        }

        // Recordamos la fila elegida para no perderla al actualizar
        int? avisoElegido = AvisoSeleccionado();

        var filas = _notificacionService.ObtenerAvisos(idEmpleado.Value, chkSoloSinLeer.Checked);
        dgvAvisos.DataSource = filas;
        foreach (var oculta in new[] { "IdAviso", "IdVisita", "EnCurso" })
        {
            if (dgvAvisos.Columns[oculta] != null)
                dgvAvisos.Columns[oculta]!.Visible = false;
        }

        foreach (DataGridViewRow fila in dgvAvisos.Rows)
        {
            if ((int)fila.Cells["IdAviso"].Value == avisoElegido)
            {
                dgvAvisos.CurrentCell = fila.Cells["Visitante"];
                break;
            }
        }

        int sinLeer = (cmbAnfitrion.SelectedItem as OpcionAnfitrion)?.SinLeer ?? 0;
        lblResumen.Text = sinLeer == 0
            ? "Todo al día"
            : $"{sinLeer} aviso{(sinLeer == 1 ? "" : "s")} sin leer";
        lblResumen.ForeColor = sinLeer == 0 ? Estilo.Exito : Estilo.Advertencia;

        if (filas.Count == 0)
            MostrarVacio(chkSoloSinLeer.Checked ? "No tiene avisos pendientes." : "Todavía no tiene avisos.");
        else
            lblVacio.Visible = false;

        btnLeido.Enabled = btnTodos.Enabled = sinLeer > 0;
        btnTerminar.Enabled = FilaSeleccionada()?.EnCurso == true;
    }

    /// <summary>Recarga combo y tabla (lo usa el timer y después de marcar avisos).</summary>
    private void Actualizar()
    {
        try
        {
            CargarAnfitriones();
            CargarAvisos();
        }
        catch
        {
            // Si se cae la conexión un momento, se reintenta en el siguiente ciclo
        }
    }

    private void MostrarVacio(string texto)
    {
        lblVacio.Text = texto;
        lblVacio.Visible = true;
    }

    private int? AvisoSeleccionado() =>
        dgvAvisos.CurrentRow?.Cells["IdAviso"].Value as int?;

    private FilaAviso? FilaSeleccionada() =>
        dgvAvisos.CurrentRow?.DataBoundItem as FilaAviso;

    // ======================= EVENTOS =======================
    private void BtnLeido_Click(object? sender, EventArgs e)
    {
        int? idAviso = AvisoSeleccionado();
        if (idAviso == null)
        {
            MessageBox.Show("Seleccione un aviso de la lista.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _notificacionService.MarcarLeido(idAviso.Value);
        Actualizar();
    }

    private void BtnTodos_Click(object? sender, EventArgs e)
    {
        int? idEmpleado = AnfitrionSeleccionado();
        if (idEmpleado == null) return;

        _notificacionService.MarcarTodosLeidos(idEmpleado.Value);
        Actualizar();
    }

    /// <summary>Termina la visita aunque todavía tenga tiempo (por ejemplo, si el visitante ya se tiene que ir).</summary>
    private void BtnTerminar_Click(object? sender, EventArgs e)
    {
        var fila = FilaSeleccionada();
        if (fila == null || !fila.EnCurso) return;

        var respuesta = MessageBox.Show(
            $"¿Terminar la visita de {fila.Visitante}?\n\nSe registrará su salida con la hora actual.",
            "Terminar visita", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (respuesta != DialogResult.Yes) return;

        try
        {
            string visitante = _notificacionService.TerminarVisita(fila.IdVisita, _soloEmpleado);
            Actualizar();
            MessageBox.Show($"La visita de {visitante} quedó terminada.", "Listo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo terminar la visita:\n" + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
