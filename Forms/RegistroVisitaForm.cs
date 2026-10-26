using VisitasRecepcion.Data;
using VisitasRecepcion.Models;
using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>
/// Módulo de registro de visitas: buscar visitante, registrar ingreso,
/// dejar el aviso interno al anfitrión y registrar la salida.
/// </summary>
public class RegistroVisitaForm : Form
{
    // ----- Servicios (la lógica está en la capa de negocio) -----
    private readonly VisitaService _visitaService = new();
    private readonly NotificacionService _notificacionService = new();

    // Visitante seleccionado de la búsqueda (null = visitante nuevo)
    private Visitante? _visitanteSeleccionado;

    // ----- Controles -----
    private readonly TextBox txtBuscar = new();
    private readonly Button btnBuscar = new();
    private readonly ListBox lstResultados = new();
    private readonly TextBox txtNombre = new();
    private readonly TextBox txtDocumento = new();
    private readonly TextBox txtTelefono = new();
    private readonly TextBox txtCorreo = new();
    private readonly ComboBox cmbArea = new();
    private readonly ComboBox cmbEmpleado = new();
    private readonly TextBox txtMotivo = new();
    private readonly Button btnNuevo = new();
    private readonly Button btnRegistrar = new();
    private readonly DataGridView dgvAbiertas = new();
    private readonly Button btnSalida = new();

    public RegistroVisitaForm()
    {
        Text = "Registro de visitas";
        // Alto pensado para que quepa en pantallas de 1080 px al 150 %
        ClientSize = new Size(984, 640);
        MinimumSize = new Size(1000, 680);
        StartPosition = FormStartPosition.CenterParent;
        Estilo.Formulario(this);

        ConstruirInterfaz();
        Estilo.Campos(this);   // estilo uniforme para cajas de texto, combos y listas

        CargarCombos();
        CargarAbiertas();

        // Refresca la tabla para ver cuándo el anfitrión lee su aviso
        var timer = new System.Windows.Forms.Timer { Interval = Config.SegundosActualizarAvisos * 1000 };
        timer.Tick += (s, e) => CargarAbiertas();
        timer.Start();
        FormClosed += (s, e) => timer.Dispose();

        Estilo.AjustarAPantalla(this);
    }

    // ======================= INTERFAZ =======================
    private void ConstruirInterfaz()
    {
        Controls.Add(Estilo.Encabezado("Registro de visitas",
            "Busque un visitante recurrente o registre uno nuevo"));

        // ---------- Tarjeta 1: búsqueda ----------
        // Las tarjetas van separadas 12 px entre sí y 20 px de los bordes
        var tarjetaBuscar = Estilo.Tarjeta(20, 96, 944, 104);
        tarjetaBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        tarjetaBuscar.Controls.Add(Estilo.TituloTarjeta("1. Buscar visitante", 20, 12));
        tarjetaBuscar.Controls.Add(Estilo.Caption("Nombre o documento", 20, 42));

        txtBuscar.SetBounds(20, 64, 300, 28);
        txtBuscar.KeyDown += (s, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true;   // sin el "ding" de Windows
            BtnBuscar_Click(s, e);
        };

        btnBuscar.Text = "Buscar";
        btnBuscar.SetBounds(332, 62, 110, 32);
        Estilo.Boton(btnBuscar, Estilo.Primario);
        btnBuscar.Click += BtnBuscar_Click;

        tarjetaBuscar.Controls.Add(Estilo.Caption("Resultados  (si la persona ya visitó antes, selecciónela aquí)", 470, 12));

        lstResultados.SetBounds(470, 34, 454, 58);
        lstResultados.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lstResultados.SelectedIndexChanged += LstResultados_SelectedIndexChanged;

        tarjetaBuscar.Controls.AddRange(new Control[] { txtBuscar, btnBuscar, lstResultados });

        // ---------- Tarjeta 2: datos de la visita ----------
        var tarjetaDatos = Estilo.Tarjeta(20, 212, 944, 212);
        tarjetaDatos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        tarjetaDatos.Controls.Add(Estilo.TituloTarjeta("2. Datos de la visita", 20, 12));

        // Fila 1 (todos obligatorios)
        AgregarCampo(tarjetaDatos, "Nombre *", txtNombre, 20, 44, 210);
        AgregarCampo(tarjetaDatos, $"Documento (DPI, {Config.LargoDocumento} números) *", txtDocumento, 250, 44, 210);
        AgregarCampo(tarjetaDatos, $"Teléfono ({Config.LargoTelefono} números) *", txtTelefono, 480, 44, 210);
        AgregarCampo(tarjetaDatos, "Correo *", txtCorreo, 710, 44, 214);

        // Cada campo solo deja escribir los caracteres permitidos
        txtNombre.MaxLength = 100;
        txtDocumento.MaxLength = Config.LargoDocumento;
        txtTelefono.MaxLength = Config.LargoTelefono;
        txtCorreo.MaxLength = 100;
        SoloPermitir(txtNombre, ValidacionVisitante.CaracterNombre);
        SoloPermitir(txtDocumento, ValidacionVisitante.CaracterNumero);
        SoloPermitir(txtTelefono, ValidacionVisitante.CaracterNumero);
        SoloPermitir(txtCorreo, ValidacionVisitante.CaracterCorreo);

        // Fila 2
        cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEmpleado.DropDownStyle = ComboBoxStyle.DropDownList;
        AgregarCampo(tarjetaDatos, "Área a visitar *", cmbArea, 20, 104, 210);
        AgregarCampo(tarjetaDatos, "Anfitrión (a quién visita) *", cmbEmpleado, 250, 104, 210);
        AgregarCampo(tarjetaDatos, "Motivo de la visita *", txtMotivo, 480, 104, 444);

        tarjetaDatos.Controls.Add(Estilo.Caption("* Campos obligatorios", 20, 175));

        // Botones (alineados con el borde derecho de los campos)
        btnNuevo.Text = "Limpiar / Nuevo";
        btnNuevo.SetBounds(574, 166, 150, 36);
        Estilo.BotonSecundario(btnNuevo);
        btnNuevo.Click += (s, e) => LimpiarFormulario();

        btnRegistrar.Text = "Registrar ingreso";
        btnRegistrar.SetBounds(734, 166, 190, 36);
        Estilo.Boton(btnRegistrar, Estilo.Acento);
        btnRegistrar.Click += BtnRegistrar_Click;

        tarjetaDatos.Controls.AddRange(new Control[] { btnNuevo, btnRegistrar });

        // ---------- Tarjeta 3: visitas abiertas (sin salida) ----------
        var tarjetaAbiertas = Estilo.Tarjeta(20, 436, 944, 184);
        tarjetaAbiertas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        tarjetaAbiertas.Controls.Add(Estilo.TituloTarjeta("3. Visitas dentro de la empresa (sin hora de salida)", 20, 12));

        btnSalida.Text = "Registrar salida (un clic)";
        btnSalida.SetBounds(714, 8, 210, 34);
        btnSalida.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Estilo.Boton(btnSalida, Estilo.Advertencia);
        btnSalida.Click += BtnSalida_Click;

        dgvAbiertas.SetBounds(20, 50, 904, 120);
        dgvAbiertas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Estilo.Grid(dgvAbiertas);
        dgvAbiertas.RowTemplate.Height = Estilo.Escalar(dgvAbiertas, 28);
        dgvAbiertas.CellFormatting += (s, e) => Estilo.FormatoCeldas(dgvAbiertas, e);

        tarjetaAbiertas.Controls.AddRange(new Control[] { btnSalida, dgvAbiertas });

        Controls.AddRange(new Control[] { tarjetaBuscar, tarjetaDatos, tarjetaAbiertas });
    }

    /// <summary>Agrega una etiqueta pequeña y, debajo, el control (caja de texto o combo).</summary>
    private static void AgregarCampo(Panel tarjeta, string titulo, Control control, int x, int y, int ancho)
    {
        tarjeta.Controls.Add(Estilo.Caption(titulo, x, y));
        control.SetBounds(x, y + 22, ancho, 28);
        tarjeta.Controls.Add(control);
    }

    /// <summary>
    /// Hace que una caja de texto solo acepte los caracteres permitidos.
    /// KeyPress bloquea lo que se escribe; TextChanged limpia lo que se pega con Ctrl+V
    /// o lo que viene de la base (por ejemplo, un teléfono guardado como "5512 3456").
    /// </summary>
    private static void SoloPermitir(TextBox caja, Func<char, bool> permitido)
    {
        caja.KeyPress += (s, e) =>
        {
            if (!char.IsControl(e.KeyChar) && !permitido(e.KeyChar))
                e.Handled = true;
        };

        caja.TextChanged += (s, e) =>
        {
            string limpio = new string(caja.Text.Where(permitido).ToArray());
            if (limpio.Length > caja.MaxLength)
                limpio = limpio[..caja.MaxLength];
            if (limpio == caja.Text) return;

            int cursor = Math.Max(0, caja.SelectionStart - (caja.Text.Length - limpio.Length));
            caja.Text = limpio;
            caja.SelectionStart = Math.Min(cursor, limpio.Length);
        };
    }

    // ======================= CARGA DE DATOS =======================
    private void CargarCombos()
    {
        using var db = new AppDbContext();
        cmbArea.DataSource = db.Areas.Where(a => a.Activo).OrderBy(a => a.Nombre).ToList();
        cmbArea.DisplayMember = "Nombre";
        cmbArea.ValueMember = "IdArea";

        cmbEmpleado.DataSource = db.Empleados.Where(e => e.Activo).OrderBy(e => e.Nombre).ToList();
        cmbEmpleado.DisplayMember = "Nombre";
        cmbEmpleado.ValueMember = "IdEmpleado";
    }

    private void CargarAbiertas()
    {
        // Recordamos la fila elegida para no perderla al refrescar
        var elegida = dgvAbiertas.CurrentRow?.Cells["IdVisita"].Value as int?;

        dgvAbiertas.DataSource = _visitaService.ObtenerAbiertas();
        if (dgvAbiertas.Columns["IdVisita"] != null)
            dgvAbiertas.Columns["IdVisita"]!.Visible = false;   // el Id no se muestra

        foreach (DataGridViewRow fila in dgvAbiertas.Rows)
        {
            if ((int)fila.Cells["IdVisita"].Value == elegida)
            {
                dgvAbiertas.CurrentCell = fila.Cells["Visitante"];
                break;
            }
        }
    }

    // ======================= EVENTOS =======================
    private void BtnBuscar_Click(object? sender, EventArgs e)
    {
        var lista = _visitaService.BuscarVisitantes(txtBuscar.Text);
        lstResultados.DataSource = lista;
        lstResultados.SelectedIndex = -1;   // que no se seleccione solo el primero

        if (lista.Count == 0)
            MessageBox.Show("No se encontraron visitantes. Puede registrarlo como nuevo llenando los datos.",
                "Sin resultados", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void LstResultados_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (lstResultados.SelectedItem is Visitante v)
        {
            // Visitante recurrente: llenamos sus datos y bloqueamos nombre/documento
            _visitanteSeleccionado = v;
            txtNombre.Text = v.Nombre;
            txtDocumento.Text = v.Documento;
            txtTelefono.Text = v.Telefono;
            txtCorreo.Text = v.Correo;
            txtNombre.ReadOnly = true;
            txtDocumento.ReadOnly = true;
        }
    }

    private void BtnRegistrar_Click(object? sender, EventArgs e)
    {
        // ---- Armar el visitante con lo que hay en pantalla ----
        var datos = new Visitante
        {
            IdVisitante = _visitanteSeleccionado?.IdVisitante ?? 0,   // 0 = nuevo
            Nombre = string.Join(" ", txtNombre.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            Documento = txtDocumento.Text.Trim(),
            Telefono = txtTelefono.Text.Trim(),
            Correo = txtCorreo.Text.Trim()
        };

        // ---- Validar nombre, documento, teléfono y correo ----
        var errores = ValidacionVisitante.Validar(datos);
        if (errores.Count > 0)
        {
            MessageBox.Show("Revise los datos del visitante:\n\n• " + string.Join("\n• ", errores.Select(x => x.Mensaje)),
                "Datos incompletos o inválidos", MessageBoxButtons.OK, MessageBoxIcon.Warning);

            // Ponemos el cursor en el primer campo con error
            TextBox campo = errores[0].Campo switch
            {
                "Nombre" => txtNombre,
                "Documento" => txtDocumento,
                "Telefono" => txtTelefono,
                _ => txtCorreo
            };
            campo.Focus();
            campo.SelectAll();
            return;
        }

        if (cmbArea.SelectedValue == null || cmbEmpleado.SelectedValue == null)
        {
            MessageBox.Show("Seleccione el área y el anfitrión. (Si no hay, el administrador debe crearlos.)",
                "Faltan datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(txtMotivo.Text))
        {
            MessageBox.Show("Escriba el motivo de la visita.", "Faltan datos",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtMotivo.Focus();
            return;
        }

        btnRegistrar.Enabled = false;
        try
        {
            // 1) Registrar la visita
            var resultado = _visitaService.RegistrarVisita(
                datos,
                (int)cmbEmpleado.SelectedValue,
                (int)cmbArea.SelectedValue,
                txtMotivo.Text.Trim());

            // 2) Dejar el aviso interno para el anfitrión
            var (ok, mensajeAviso) = _notificacionService.CrearAviso(resultado.IdVisita);

            // 3) Mostrar el resumen
            string tipo = resultado.EraPrimeraVisita
                ? "Visitante NUEVO: se aplicó el límite genérico."
                : "Visitante recurrente.";
            string texto = $"Visita registrada.\n\n{tipo}\nDuración máxima: {resultado.DuracionMaxMin} min.\n\n{mensajeAviso}";
            if (resultado.Advertencia != "")
                texto += "\n\nADVERTENCIA: " + resultado.Advertencia;

            MessageBox.Show(texto, "Registro exitoso", MessageBoxButtons.OK,
                (ok && resultado.Advertencia == "") ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

            LimpiarFormulario();
            CargarAbiertas();
        }
        catch (Exception ex)
        {
            MessageBox.Show("No se pudo registrar la visita:\n" + ex.Message, "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnRegistrar.Enabled = true;
        }
    }

    private void BtnSalida_Click(object? sender, EventArgs e)
    {
        if (dgvAbiertas.CurrentRow == null)
        {
            MessageBox.Show("Seleccione una visita de la lista.", "Aviso",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int idVisita = (int)dgvAbiertas.CurrentRow.Cells["IdVisita"].Value;
        _visitaService.RegistrarSalida(idVisita);
        CargarAbiertas();
    }

    private void LimpiarFormulario()
    {
        _visitanteSeleccionado = null;
        lstResultados.DataSource = null;
        txtBuscar.Clear();
        txtNombre.Clear();
        txtDocumento.Clear();
        txtTelefono.Clear();
        txtCorreo.Clear();
        txtMotivo.Clear();
        txtNombre.ReadOnly = false;
        txtDocumento.ReadOnly = false;
        txtNombre.Focus();
    }
}
