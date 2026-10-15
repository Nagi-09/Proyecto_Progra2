using VisitasRecepcion.Data;
using VisitasRecepcion.Models;
using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>
/// Módulo de administración (RF-10 y RF-04): áreas, empleados, límites permitidos y usuarios.
/// Cada pestaña tiene alta, modificación y baja (la baja es "lógica": se desactiva, no se borra).
/// </summary>
public class AdminForm : Form
{
    private readonly TabControl tabs = new();
    private readonly AuthService _auth = new();

    // Clase pequeña para llenar combos con (Id, Texto)
    private class ItemCombo
    {
        public int Id { get; set; }
        public string Texto { get; set; } = "";
    }

    // ---------- Pestaña Áreas ----------
    private readonly TextBox txtAreaNombre = new();
    private readonly TextBox txtAreaDesc = new();
    private readonly DataGridView dgvAreas = new();

    // ---------- Pestaña Empleados ----------
    private readonly TextBox txtEmpNombre = new();
    private readonly TextBox txtEmpCorreo = new();
    private readonly TextBox txtEmpCargo = new();
    private readonly ComboBox cmbEmpArea = new();
    private readonly DataGridView dgvEmpleados = new();

    // ---------- Pestaña Límites ----------
    private readonly ComboBox cmbLimArea = new();
    private readonly DateTimePicker dtpInicio = new();
    private readonly DateTimePicker dtpFin = new();
    private readonly NumericUpDown nudDuracion = new();
    private readonly DataGridView dgvLimites = new();

    // ---------- Pestaña Usuarios ----------
    private readonly TextBox txtUsrLogin = new();
    private readonly TextBox txtUsrPass = new();
    private readonly ComboBox cmbUsrRol = new();
    private readonly DataGridView dgvUsuarios = new();

    public AdminForm()
    {
        Text = "Administración";
        ClientSize = new Size(900, 640);
        MinimumSize = new Size(860, 600);
        StartPosition = FormStartPosition.CenterParent;
        Estilo.Formulario(this);

        tabs.Dock = DockStyle.Fill;
        tabs.TabPages.Add(ConstruirTabAreas());
        tabs.TabPages.Add(ConstruirTabEmpleados());
        tabs.TabPages.Add(ConstruirTabLimites());
        tabs.TabPages.Add(ConstruirTabUsuarios());

        // Fondo gris claro en cada pestaña
        foreach (TabPage pagina in tabs.TabPages)
        {
            pagina.UseVisualStyleBackColor = false;
            pagina.BackColor = Estilo.Fondo;
        }

        // Pestañas con diseño propio: la activa va en azul, las demás en gris
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(200, 40);
        tabs.Font = Estilo.Negrita;
        tabs.DrawItem += (s, e) =>
        {
            bool activa = e.Index == tabs.SelectedIndex;
            using var pincel = new SolidBrush(activa ? Estilo.Primario : Estilo.Borde);
            e.Graphics.FillRectangle(pincel, e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                tabs.TabPages[e.Index].Text,
                Estilo.Negrita,
                e.Bounds,
                activa ? Color.White : Estilo.Texto,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };

        // Primero se agrega la zona que ocupa todo (tabs) y después el encabezado de arriba
        Controls.Add(tabs);
        Controls.Add(Estilo.Encabezado("Administración",
            "Áreas, empleados, límites permitidos y usuarios del sistema"));

        Estilo.Campos(this);   // estilo uniforme para cajas de texto, combos, etc.

        CargarAreas();
        CargarEmpleados();
        CargarLimites();
        CargarUsuarios();
    }

    // =====================================================================
    //  Métodos auxiliares para no repetir código
    // =====================================================================
    private static Label Etiqueta(string texto, int x, int y) =>
        new Label { Text = texto, Left = x, Top = y + 3, AutoSize = true, ForeColor = Estilo.TextoSuave };

    private static Button Boton(string texto, int x, int y, int ancho, EventHandler click)
    {
        var b = new Button { Text = texto, Left = x, Top = y, Width = ancho, Height = 34 };

        // El color depende de la acción: rojo = baja, verde = agregar/guardar límite, azul = lo demás
        Color color;
        if (texto.StartsWith("Dar de baja"))
            color = Estilo.Peligro;
        else if (texto.StartsWith("Agregar") || texto.StartsWith("Guardar límite"))
            color = Estilo.Acento;
        else
            color = Estilo.Primario;

        Estilo.Boton(b, color);
        b.Click += click;
        return b;
    }

    private static void ConfigurarGrid(DataGridView dgv)
    {
        Estilo.Grid(dgv);   // estilo común de tablas
        dgv.Left = 15;
        dgv.Top = 130;
        dgv.Width = 760;
        dgv.Height = 340;
        dgv.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        dgv.ReadOnly = true;
        dgv.AllowUserToAddRows = false;
        dgv.MultiSelect = false;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
    }

    /// <summary>Lee el Id (primera columna) de la fila seleccionada; devuelve 0 si no hay selección.</summary>
    private static int IdSeleccionado(DataGridView dgv, string columna)
    {
        if (dgv.CurrentRow == null) return 0;
        return (int)dgv.CurrentRow.Cells[columna].Value;
    }

    private static void Error(Exception ex) =>
        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static void Aviso(string texto) =>
        MessageBox.Show(texto, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);

    // =====================================================================
    //  ÁREAS
    // =====================================================================
    private TabPage ConstruirTabAreas()
    {
        var tab = new TabPage("Áreas");

        tab.Controls.Add(Etiqueta("Nombre:", 15, 20));
        txtAreaNombre.SetBounds(90, 17, 250, 25);
        tab.Controls.Add(txtAreaNombre);

        tab.Controls.Add(Etiqueta("Descripción:", 360, 20));
        txtAreaDesc.SetBounds(445, 17, 330, 25);
        tab.Controls.Add(txtAreaDesc);

        tab.Controls.Add(Boton("Agregar", 15, 60, 110, AgregarArea));
        tab.Controls.Add(Boton("Guardar cambios", 135, 60, 130, ModificarArea));
        tab.Controls.Add(Boton("Dar de baja", 275, 60, 110, DarBajaArea));

        ConfigurarGrid(dgvAreas);
        dgvAreas.SelectionChanged += (s, e) =>
        {
            if (dgvAreas.CurrentRow == null) return;
            txtAreaNombre.Text = dgvAreas.CurrentRow.Cells["Nombre"].Value?.ToString() ?? "";
            txtAreaDesc.Text = dgvAreas.CurrentRow.Cells["Descripcion"].Value?.ToString() ?? "";
        };
        tab.Controls.Add(dgvAreas);
        return tab;
    }

    private void CargarAreas()
    {
        using var db = new AppDbContext();
        dgvAreas.DataSource = db.Areas
            .Where(a => a.Activo)
            .OrderBy(a => a.Nombre)
            .Select(a => new { a.IdArea, a.Nombre, a.Descripcion })
            .ToList();
        dgvAreas.Columns["IdArea"]!.Visible = false;

        // Los combos de otras pestañas también dependen de las áreas
        CargarComboAreas();
    }

    private void AgregarArea(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtAreaNombre.Text)) { Aviso("Escriba el nombre del área."); return; }
        try
        {
            using var db = new AppDbContext();
            db.Areas.Add(new Area { Nombre = txtAreaNombre.Text.Trim(), Descripcion = txtAreaDesc.Text.Trim() });
            db.SaveChanges();
            txtAreaNombre.Clear();
            txtAreaDesc.Clear();
            CargarAreas();
        }
        catch (Exception ex) { Error(ex); }
    }

    private void ModificarArea(object? sender, EventArgs e)
    {
        int id = IdSeleccionado(dgvAreas, "IdArea");
        if (id == 0) { Aviso("Seleccione un área de la lista."); return; }
        try
        {
            using var db = new AppDbContext();
            var area = db.Areas.First(a => a.IdArea == id);
            area.Nombre = txtAreaNombre.Text.Trim();
            area.Descripcion = txtAreaDesc.Text.Trim();
            db.SaveChanges();
            CargarAreas();
        }
        catch (Exception ex) { Error(ex); }
    }

    private void DarBajaArea(object? sender, EventArgs e)
    {
        int id = IdSeleccionado(dgvAreas, "IdArea");
        if (id == 0) { Aviso("Seleccione un área de la lista."); return; }
        if (MessageBox.Show("¿Dar de baja esta área?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            using var db = new AppDbContext();
            db.Areas.First(a => a.IdArea == id).Activo = false;
            db.SaveChanges();
            CargarAreas();
        }
        catch (Exception ex) { Error(ex); }
    }

    // =====================================================================
    //  EMPLEADOS
    // =====================================================================
    private TabPage ConstruirTabEmpleados()
    {
        var tab = new TabPage("Empleados / Anfitriones");

        tab.Controls.Add(Etiqueta("Nombre:", 15, 20));
        txtEmpNombre.SetBounds(90, 17, 250, 25);
        tab.Controls.Add(txtEmpNombre);

        tab.Controls.Add(Etiqueta("Correo:", 360, 20));
        txtEmpCorreo.SetBounds(445, 17, 330, 25);
        tab.Controls.Add(txtEmpCorreo);

        tab.Controls.Add(Etiqueta("Cargo:", 15, 55));
        txtEmpCargo.SetBounds(90, 52, 250, 25);
        tab.Controls.Add(txtEmpCargo);

        tab.Controls.Add(Etiqueta("Área:", 360, 55));
        cmbEmpArea.SetBounds(445, 52, 330, 25);
        cmbEmpArea.DropDownStyle = ComboBoxStyle.DropDownList;
        tab.Controls.Add(cmbEmpArea);

        tab.Controls.Add(Boton("Agregar", 15, 90, 110, AgregarEmpleado));
        tab.Controls.Add(Boton("Guardar cambios", 135, 90, 130, ModificarEmpleado));
        tab.Controls.Add(Boton("Dar de baja", 275, 90, 110, DarBajaEmpleado));

        ConfigurarGrid(dgvEmpleados);
        dgvEmpleados.Top = 130;
        dgvEmpleados.SelectionChanged += (s, e) =>
        {
            if (dgvEmpleados.CurrentRow == null) return;
            txtEmpNombre.Text = dgvEmpleados.CurrentRow.Cells["Nombre"].Value?.ToString() ?? "";
            txtEmpCorreo.Text = dgvEmpleados.CurrentRow.Cells["Correo"].Value?.ToString() ?? "";
            txtEmpCargo.Text = dgvEmpleados.CurrentRow.Cells["Cargo"].Value?.ToString() ?? "";
            cmbEmpArea.SelectedValue = dgvEmpleados.CurrentRow.Cells["IdArea"].Value;
        };
        tab.Controls.Add(dgvEmpleados);
        return tab;
    }

    private void CargarComboAreas()
    {
        using var db = new AppDbContext();
        var areas = db.Areas.Where(a => a.Activo).OrderBy(a => a.Nombre).ToList();

        // Combo de empleados
        cmbEmpArea.DataSource = areas.Select(a => new ItemCombo { Id = a.IdArea, Texto = a.Nombre }).ToList();
        cmbEmpArea.DisplayMember = "Texto";
        cmbEmpArea.ValueMember = "Id";

        // Combo de límites: la opción 0 es el límite genérico
        var items = new List<ItemCombo> { new ItemCombo { Id = 0, Texto = "(Genérico - visitantes nuevos)" } };
        items.AddRange(areas.Select(a => new ItemCombo { Id = a.IdArea, Texto = a.Nombre }));
        cmbLimArea.DataSource = items;
        cmbLimArea.DisplayMember = "Texto";
        cmbLimArea.ValueMember = "Id";
    }

    private void CargarEmpleados()
    {
        using var db = new AppDbContext();
        dgvEmpleados.DataSource = db.Empleados
            .Where(e => e.Activo)
            .OrderBy(e => e.Nombre)
            .Select(e => new
            {
                e.IdEmpleado,
                e.Nombre,
                e.Correo,
                e.Cargo,
                Area = e.Area!.Nombre,
                e.IdArea
            })
            .ToList();
        dgvEmpleados.Columns["IdEmpleado"]!.Visible = false;
        dgvEmpleados.Columns["IdArea"]!.Visible = false;
    }

    private void AgregarEmpleado(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtEmpNombre.Text) || cmbEmpArea.SelectedValue == null)
        {
            Aviso("Escriba el nombre y seleccione un área.");
            return;
        }
        try
        {
            using var db = new AppDbContext();
            db.Empleados.Add(new Empleado
            {
                Nombre = txtEmpNombre.Text.Trim(),
                Correo = txtEmpCorreo.Text.Trim(),
                Cargo = txtEmpCargo.Text.Trim(),
                IdArea = (int)cmbEmpArea.SelectedValue
            });
            db.SaveChanges();
            txtEmpNombre.Clear();
            txtEmpCorreo.Clear();
            txtEmpCargo.Clear();
            CargarEmpleados();
        }
        catch (Exception ex) { Error(ex); }
    }

    private void ModificarEmpleado(object? sender, EventArgs e)
    {
        int id = IdSeleccionado(dgvEmpleados, "IdEmpleado");
        if (id == 0) { Aviso("Seleccione un empleado de la lista."); return; }
        try
        {
            using var db = new AppDbContext();
            var emp = db.Empleados.First(x => x.IdEmpleado == id);
            emp.Nombre = txtEmpNombre.Text.Trim();
            emp.Correo = txtEmpCorreo.Text.Trim();
            emp.Cargo = txtEmpCargo.Text.Trim();
            emp.IdArea = (int)cmbEmpArea.SelectedValue!;
            db.SaveChanges();
            CargarEmpleados();
        }
        catch (Exception ex) { Error(ex); }
    }

    private void DarBajaEmpleado(object? sender, EventArgs e)
    {
        int id = IdSeleccionado(dgvEmpleados, "IdEmpleado");
        if (id == 0) { Aviso("Seleccione un empleado de la lista."); return; }
        if (MessageBox.Show("¿Dar de baja a este empleado?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            using var db = new AppDbContext();
            db.Empleados.First(x => x.IdEmpleado == id).Activo = false;
            db.SaveChanges();
            CargarEmpleados();
        }
        catch (Exception ex) { Error(ex); }
    }

    // =====================================================================
    //  LÍMITES PERMITIDOS
    // =====================================================================
    private TabPage ConstruirTabLimites()
    {
        var tab = new TabPage("Límites permitidos");

        tab.Controls.Add(Etiqueta("Aplica a:", 15, 20));
        cmbLimArea.SetBounds(90, 17, 300, 25);
        cmbLimArea.DropDownStyle = ComboBoxStyle.DropDownList;
        tab.Controls.Add(cmbLimArea);

        tab.Controls.Add(Etiqueta("Hora inicio:", 15, 55));
        ConfigurarHora(dtpInicio, 90, 52);
        tab.Controls.Add(dtpInicio);

        tab.Controls.Add(Etiqueta("Hora fin:", 230, 55));
        ConfigurarHora(dtpFin, 295, 52);
        tab.Controls.Add(dtpFin);

        tab.Controls.Add(Etiqueta("Duración máx. (min):", 440, 55));
        nudDuracion.SetBounds(570, 52, 70, 25);
        nudDuracion.Minimum = 5;
        nudDuracion.Maximum = 1440;
        nudDuracion.Value = Config.DuracionGenericaMin;
        tab.Controls.Add(nudDuracion);

        tab.Controls.Add(Boton("Guardar límite", 15, 90, 130, GuardarLimite));

        ConfigurarGrid(dgvLimites);
        tab.Controls.Add(dgvLimites);
        return tab;
    }

    private static void ConfigurarHora(DateTimePicker dtp, int x, int y)
    {
        dtp.SetBounds(x, y, 90, 25);
        dtp.Format = DateTimePickerFormat.Time;
        dtp.ShowUpDown = true;
    }

    private void CargarLimites()
    {
        using var db = new AppDbContext();
        var lista = db.Limites
            .Select(l => new
            {
                Aplica = l.EsGenerico ? "(Genérico)" : l.Area!.Nombre,
                Inicio = l.HoraInicio,
                Fin = l.HoraFin,
                DuracionMaxMin = l.DuracionMaxMin
            })
            .ToList();
        dgvLimites.DataSource = lista;
    }

    private void GuardarLimite(object? sender, EventArgs e)
    {
        if (cmbLimArea.SelectedValue == null) return;

        var inicio = dtpInicio.Value.TimeOfDay;
        var fin = dtpFin.Value.TimeOfDay;
        if (fin <= inicio) { Aviso("La hora fin debe ser mayor que la hora inicio."); return; }

        try
        {
            int idArea = (int)cmbLimArea.SelectedValue;
            using var db = new AppDbContext();

            // Buscamos si ya existe un límite para esa área (o el genérico) para actualizarlo
            LimitePermitido? limite = idArea == 0
                ? db.Limites.FirstOrDefault(l => l.EsGenerico)
                : db.Limites.FirstOrDefault(l => l.IdArea == idArea);

            if (limite == null)
            {
                limite = new LimitePermitido
                {
                    EsGenerico = idArea == 0,
                    IdArea = idArea == 0 ? null : idArea
                };
                db.Limites.Add(limite);
            }

            limite.HoraInicio = inicio;
            limite.HoraFin = fin;
            limite.DuracionMaxMin = (int)nudDuracion.Value;
            db.SaveChanges();

            CargarLimites();
        }
        catch (Exception ex) { Error(ex); }
    }

    // =====================================================================
    //  USUARIOS
    // =====================================================================
    private TabPage ConstruirTabUsuarios()
    {
        var tab = new TabPage("Usuarios");

        tab.Controls.Add(Etiqueta("Login:", 15, 20));
        txtUsrLogin.SetBounds(90, 17, 200, 25);
        tab.Controls.Add(txtUsrLogin);

        tab.Controls.Add(Etiqueta("Contraseña:", 310, 20));
        txtUsrPass.SetBounds(390, 17, 200, 25);
        txtUsrPass.UseSystemPasswordChar = true;
        tab.Controls.Add(txtUsrPass);

        tab.Controls.Add(Etiqueta("Rol:", 610, 20));
        cmbUsrRol.SetBounds(650, 17, 125, 25);
        cmbUsrRol.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbUsrRol.Items.AddRange(new object[] { "Recepcionista", "Administrador" });
        cmbUsrRol.SelectedIndex = 0;
        tab.Controls.Add(cmbUsrRol);

        tab.Controls.Add(Boton("Agregar", 15, 60, 110, AgregarUsuario));
        tab.Controls.Add(Boton("Cambiar contraseña", 135, 60, 150, CambiarPassword));
        tab.Controls.Add(Boton("Dar de baja", 295, 60, 110, DarBajaUsuario));

        ConfigurarGrid(dgvUsuarios);
        dgvUsuarios.Top = 110;
        dgvUsuarios.Height = 360;
        tab.Controls.Add(dgvUsuarios);
        return tab;
    }

    private void CargarUsuarios()
    {
        using var db = new AppDbContext();
        dgvUsuarios.DataSource = db.Usuarios
            .Where(u => u.Activo)
            .OrderBy(u => u.Login)
            .Select(u => new { u.IdUsuario, u.Login, u.Rol })   // ¡nunca mostramos el hash!
            .ToList();
        dgvUsuarios.Columns["IdUsuario"]!.Visible = false;
    }

    private void AgregarUsuario(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUsrLogin.Text) || txtUsrPass.Text.Length < 4)
        {
            Aviso("Escriba el login y una contraseña de al menos 4 caracteres.");
            return;
        }
        try
        {
            _auth.CrearUsuario(txtUsrLogin.Text.Trim(), txtUsrPass.Text, cmbUsrRol.SelectedItem!.ToString()!);
            txtUsrLogin.Clear();
            txtUsrPass.Clear();
            CargarUsuarios();
        }
        catch (Exception ex) { Error(ex); }
    }

    private void CambiarPassword(object? sender, EventArgs e)
    {
        int id = IdSeleccionado(dgvUsuarios, "IdUsuario");
        if (id == 0) { Aviso("Seleccione un usuario de la lista."); return; }
        if (txtUsrPass.Text.Length < 4) { Aviso("Escriba la nueva contraseña (mínimo 4 caracteres)."); return; }
        try
        {
            using var db = new AppDbContext();
            db.Usuarios.First(u => u.IdUsuario == id).PasswordHash = BCrypt.Net.BCrypt.HashPassword(txtUsrPass.Text);
            db.SaveChanges();
            txtUsrPass.Clear();
            Aviso("Contraseña actualizada.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void DarBajaUsuario(object? sender, EventArgs e)
    {
        int id = IdSeleccionado(dgvUsuarios, "IdUsuario");
        if (id == 0) { Aviso("Seleccione un usuario de la lista."); return; }
        if (MessageBox.Show("¿Dar de baja a este usuario?", "Confirmar", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        try
        {
            using var db = new AppDbContext();

            // Protección: no permitir quedarse sin administradores
            var usuario = db.Usuarios.First(u => u.IdUsuario == id);
            if (usuario.Rol == "Administrador" && db.Usuarios.Count(u => u.Activo && u.Rol == "Administrador") <= 1)
            {
                Aviso("No se puede dar de baja al único administrador.");
                return;
            }

            usuario.Activo = false;
            db.SaveChanges();
            CargarUsuarios();
        }
        catch (Exception ex) { Error(ex); }
    }
}
