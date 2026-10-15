using VisitasRecepcion.Data;
using VisitasRecepcion.Models;
using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>
/// Módulo de registro de visitas: buscar visitante, registrar ingreso,
/// notificar al anfitrión y registrar la salida.
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
        ClientSize = new Size(984, 700);
        MinimumSize = new Size(1000, 700);
        StartPosition = FormStartPosition.CenterParent;
        Estilo.Formulario(this);

        ConstruirInterfaz();
        Estilo.Campos(this);   // estilo uniforme para cajas de texto, combos y listas

        CargarCombos();
        CargarAbiertas();
    }

    // ======================= INTERFAZ =======================
    private void ConstruirInterfaz()
    {
        Controls.Add(Estilo.Encabezado("Registro de visitas",
            "Busque un visitante recurrente o registre uno nuevo"));

        // ---------- Tarjeta 1: búsqueda ----------
        var tarjetaBuscar = Estilo.Tarjeta(20, 100, 944, 140);
        tarjetaBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        tarjetaBuscar.Controls.Add(Estilo.TituloTarjeta("1. Buscar visitante", 20, 12));
        tarjetaBuscar.Controls.Add(Estilo.Caption("Nombre o documento", 20, 46));

        txtBuscar.SetBounds(20, 68, 300, 28);

        btnBuscar.Text = "Buscar";
        btnBuscar.SetBounds(330, 66, 110, 32);
        Estilo.Boton(btnBuscar, Estilo.Primario);
        btnBuscar.Click += BtnBuscar_Click;

        tarjetaBuscar.Controls.Add(Estilo.Caption("Si la persona ya visitó antes, selecciónela en la lista.", 20, 108));
        tarjetaBuscar.Controls.Add(Estilo.Caption("Resultados", 470, 14));

        lstResultados.SetBounds(470, 36, 454, 90);
        lstResultados.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lstResultados.SelectedIndexChanged += LstResultados_SelectedIndexChanged;

        tarjetaBuscar.Controls.AddRange(new Control[] { txtBuscar, btnBuscar, lstResultados });

        // ---------- Tarjeta 2: datos de la visita ----------
        var tarjetaDatos = Estilo.Tarjeta(20, 252, 944, 220);
        tarjetaDatos.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        tarjetaDatos.Controls.Add(Estilo.TituloTarjeta("2. Datos de la visita", 20, 12));

        // Fila 1
        AgregarCampo(tarjetaDatos, "Nombre", txtNombre, 20, 48, 210);
        AgregarCampo(tarjetaDatos, "Documento", txtDocumento, 250, 48, 210);
        AgregarCampo(tarjetaDatos, "Teléfono", txtTelefono, 480, 48, 210);
        AgregarCampo(tarjetaDatos, "Correo", txtCorreo, 710, 48, 214);

        // Fila 2
        cmbArea.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbEmpleado.DropDownStyle = ComboBoxStyle.DropDownList;
        AgregarCampo(tarjetaDatos, "Área a visitar", cmbArea, 20, 112, 210);
        AgregarCampo(tarjetaDatos, "Anfitrión (a quién visita)", cmbEmpleado, 250, 112, 210);
        AgregarCampo(tarjetaDatos, "Motivo de la visita", txtMotivo, 480, 112, 444);

        // Botones
        btnNuevo.Text = "Limpiar / Nuevo";
        btnNuevo.SetBounds(584, 172, 130, 38);
        Estilo.BotonSecundario(btnNuevo);
        btnNuevo.Click += (s, e) => LimpiarFormulario();

        btnRegistrar.Text = "Registrar ingreso";
        btnRegistrar.SetBounds(724, 172, 200, 38);
        Estilo.Boton(btnRegistrar, Estilo.Acento);
        btnRegistrar.Click += BtnRegistrar_Click;

        tarjetaDatos.Controls.AddRange(new Control[] { btnNuevo, btnRegistrar });

        // ---------- Tarjeta 3: visitas abiertas (sin salida) ----------
        var tarjetaAbiertas = Estilo.Tarjeta(20, 484, 944, 196);
        tarjetaAbiertas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        tarjetaAbiertas.Controls.Add(Estilo.TituloTarjeta("3. Visitas dentro de la empresa (sin hora de salida)", 20, 12));

        btnSalida.Text = "Registrar salida (un clic)";
        btnSalida.SetBounds(714, 8, 210, 34);
        btnSalida.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        Estilo.Boton(btnSalida, Estilo.Advertencia);
        btnSalida.Click += BtnSalida_Click;

        dgvAbiertas.SetBounds(20, 48, 904, 136);
        dgvAbiertas.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        Estilo.Grid(dgvAbiertas);
        dgvAbiertas.RowTemplate.Height = 28;
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
        dgvAbiertas.DataSource = _visitaService.ObtenerAbiertas();
        if (dgvAbiertas.Columns["IdVisita"] != null)
            dgvAbiertas.Columns["IdVisita"]!.Visible = false;   // el Id no se muestra
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

    private async void BtnRegistrar_Click(object? sender, EventArgs e)
    {
        // ---- Validaciones simples ----
        if (string.IsNullOrWhiteSpace(txtNombre.Text) || string.IsNullOrWhiteSpace(txtDocumento.Text))
        {
            MessageBox.Show("El nombre y el documento son obligatorios.", "Faltan datos",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            return;
        }

        // ---- Armar el visitante con lo que hay en pantalla ----
        var datos = new Visitante
        {
            IdVisitante = _visitanteSeleccionado?.IdVisitante ?? 0,   // 0 = nuevo
            Nombre = txtNombre.Text.Trim(),
            Documento = txtDocumento.Text.Trim(),
            Telefono = txtTelefono.Text.Trim(),
            Correo = txtCorreo.Text.Trim()
        };

        btnRegistrar.Enabled = false;
        try
        {
            // 1) Registrar la visita
            var resultado = _visitaService.RegistrarVisita(
                datos,
                (int)cmbEmpleado.SelectedValue,
                (int)cmbArea.SelectedValue,
                txtMotivo.Text.Trim());

            // 2) Notificar al anfitrión por correo
            var (ok, mensajeCorreo) = await _notificacionService.EnviarAvisoAsync(resultado.IdVisita);

            // 3) Mostrar el resumen
            string tipo = resultado.EraPrimeraVisita
                ? "Visitante NUEVO: se aplicó el límite genérico."
                : "Visitante recurrente.";
            string texto = $"Visita registrada.\n\n{tipo}\nDuración máxima: {resultado.DuracionMaxMin} min.\n\n{mensajeCorreo}";
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
