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
    private readonly ComboBox cmbUsrEmpleado = new();
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

        // Fondo gris claro en cada pestaña; la negrita es solo para los títulos de las pestañas
        foreach (TabPage pagina in tabs.TabPages)
        {
            pagina.UseVisualStyleBackColor = false;
            pagina.BackColor = Estilo.Fondo;
            pagina.Font = Estilo.Normal;
        }

        // Pestañas con diseño propio: la activa va en azul, las demás en gris
        tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
        tabs.SizeMode = TabSizeMode.Fixed;
        tabs.ItemSize = new Size(Estilo.Escalar(tabs, 200), Estilo.Escalar(tabs, 40));
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

        Estilo.AjustarAPantalla(this);
    }

    // =====================================================================
    //  Métodos auxiliares para no repetir código
    // =====================================================================
    // Todas las pestañas tienen la misma forma:
    //   arriba, una tarjeta con los campos (título encima de cada campo) y los botones;
    //   abajo, una tarjeta con la tabla que ocupa el resto del espacio.
    private const int Margen = 16;
    private const int AltoFormulario = 132;
    private const int FilaCampos = 16;    // y del título de los campos (el campo va 22 px abajo)
    private const int FilaBotones = 80;   // y de los botones

    /// <summary>Crea la pestaña con su tarjeta de campos (que se devuelve) y su tarjeta de tabla.</summary>
    private static TabPage NuevaPestana(string titulo, DataGridView dgv, out Panel formulario)
    {
        var tab = new TabPage(titulo);

        formulario = Estilo.Tarjeta(Margen, Margen, 800, AltoFormulario);
        tab.Controls.Add(formulario);
        Estilo.Estirar(formulario, alto: false);

        var tarjetaTabla = Estilo.Tarjeta(Margen, Margen * 2 + AltoFormulario, 800, 300);
        tarjetaTabla.Padding = new Padding(1);
        ConfigurarGrid(dgv);
        tarjetaTabla.Controls.Add(dgv);
        tab.Controls.Add(tarjetaTabla);
        Estilo.Estirar(tarjetaTabla);

        return tab;
    }

    /// <summary>Agrega un campo a la tarjeta: título pequeño arriba y el control debajo.</summary>
    private static void Campo(Panel tarjeta, string titulo, Control control, int x, int ancho)
    {
        tarjeta.Controls.Add(Estilo.Caption(titulo, x, FilaCampos));
        control.SetBounds(x, FilaCampos + 22, ancho, 28);
        tarjeta.Controls.Add(control);
    }

    private static Button Boton(string texto, int x, int y, int ancho, EventHandler click)
    {
        var b = new Button { Text = texto, Left = x, Top = y, Width = ancho, Height = 36 };

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
        dgv.Dock = DockStyle.Fill;
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
        var tab = NuevaPestana("Áreas", dgvAreas, out var f);

        Campo(f, "Nombre", txtAreaNombre, 20, 260);
        Campo(f, "Descripción", txtAreaDesc, 296, 440);

        f.Controls.Add(Boton("Agregar", 20, FilaBotones, 120, AgregarArea));
        f.Controls.Add(Boton("Guardar cambios", 152, FilaBotones, 150, ModificarArea));
        f.Controls.Add(Boton("Dar de baja", 314, FilaBotones, 120, DarBajaArea));

        dgvAreas.SelectionChanged += (s, e) =>
        {
            if (dgvAreas.CurrentRow == null) return;
            txtAreaNombre.Text = dgvAreas.CurrentRow.Cells["Nombre"].Value?.ToString() ?? "";
            txtAreaDesc.Text = dgvAreas.CurrentRow.Cells["Descripcion"].Value?.ToString() ?? "";
        };
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
        var tab = NuevaPestana("Empleados / Anfitriones", dgvEmpleados, out var f);

        cmbEmpArea.DropDownStyle = ComboBoxStyle.DropDownList;
        Campo(f, "Nombre", txtEmpNombre, 20, 190);
        Campo(f, "Correo", txtEmpCorreo, 226, 220);
        Campo(f, "Cargo", txtEmpCargo, 462, 160);
        Campo(f, "Área", cmbEmpArea, 638, 160);

        f.Controls.Add(Boton("Agregar", 20, FilaBotones, 120, AgregarEmpleado));
        f.Controls.Add(Boton("Guardar cambios", 152, FilaBotones, 150, ModificarEmpleado));
        f.Controls.Add(Boton("Dar de baja", 314, FilaBotones, 120, DarBajaEmpleado));

        dgvEmpleados.SelectionChanged += (s, e) =>
        {
            if (dgvEmpleados.CurrentRow == null) return;
            txtEmpNombre.Text = dgvEmpleados.CurrentRow.Cells["Nombre"].Value?.ToString() ?? "";
            txtEmpCorreo.Text = dgvEmpleados.CurrentRow.Cells["Correo"].Value?.ToString() ?? "";
            txtEmpCargo.Text = dgvEmpleados.CurrentRow.Cells["Cargo"].Value?.ToString() ?? "";
            cmbEmpArea.SelectedValue = dgvEmpleados.CurrentRow.Cells["IdArea"].Value;
        };
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
        var tab = NuevaPestana("Límites permitidos", dgvLimites, out var f);

        cmbLimArea.DropDownStyle = ComboBoxStyle.DropDownList;
        Campo(f, "Aplica a", cmbLimArea, 20, 280);

        ConfigurarHora(dtpInicio);
        Campo(f, "Hora inicio", dtpInicio, 316, 120);

        ConfigurarHora(dtpFin);
        dtpFin.Value = DateTime.Today.AddHours(17);
        Campo(f, "Hora fin", dtpFin, 452, 120);

        nudDuracion.Minimum = 5;
        nudDuracion.Maximum = 1440;
        nudDuracion.Value = Config.DuracionGenericaMin;
        Campo(f, "Duración máx. (min)", nudDuracion, 588, 140);

        f.Controls.Add(Boton("Guardar límite", 20, FilaBotones, 150, GuardarLimite));
        f.Controls.Add(Estilo.Caption("Si el área ya tiene un límite, se reemplaza.", 186, FilaBotones + 10));

        return tab;
    }

    private static void ConfigurarHora(DateTimePicker dtp)
    {
        dtp.Format = DateTimePickerFormat.Custom;
        dtp.CustomFormat = "HH:mm";   // 24 horas, sin segundos
        dtp.ShowUpDown = true;
        dtp.Value = DateTime.Today.AddHours(8);
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
        dgvLimites.Columns["Inicio"]!.DefaultCellStyle.Format = @"hh\:mm";
        dgvLimites.Columns["Fin"]!.DefaultCellStyle.Format = @"hh\:mm";
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
        var tab = NuevaPestana("Usuarios", dgvUsuarios, out var f);

        Campo(f, "Usuario (login)", txtUsrLogin, 20, 170);

        txtUsrPass.UseSystemPasswordChar = true;
        Campo(f, "Contraseña", txtUsrPass, 206, 170);

        cmbUsrRol.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbUsrRol.Items.AddRange(Roles.Todos);
        cmbUsrRol.SelectedIndex = 0;
        cmbUsrRol.SelectedIndexChanged += (s, e) =>
            cmbUsrEmpleado.Enabled = cmbUsrRol.SelectedItem as string == Roles.Trabajador;
        Campo(f, "Rol", cmbUsrRol, 392, 150);

        // Solo el rol Trabajador se liga a un empleado (para ver únicamente sus avisos)
        cmbUsrEmpleado.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbUsrEmpleado.Enabled = false;
        Campo(f, "Empleado (solo rol Trabajador)", cmbUsrEmpleado, 558, 220);

        f.Controls.Add(Boton("Agregar", 20, FilaBotones, 120, AgregarUsuario));
        f.Controls.Add(Boton("Cambiar contraseña", 152, FilaBotones, 170, CambiarPassword));
        f.Controls.Add(Boton("Dar de baja", 334, FilaBotones, 120, DarBajaUsuario));

        // Al entrar a la pestaña se recargan los empleados (por si se agregó uno en la otra pestaña)
        tabs.SelectedIndexChanged += (s, e) => { if (tabs.SelectedTab == tab) CargarEmpleadosUsuario(); };
        return tab;
    }

    private void CargarUsuarios()
    {
        using var db = new AppDbContext();
        dgvUsuarios.DataSource = db.Usuarios
            .Where(u => u.Activo)
            .OrderBy(u => u.Rol).ThenBy(u => u.Login)
            .Select(u => new
            {
                u.IdUsuario,
                u.Login,
                u.Rol,
                Empleado = u.Empleado != null ? u.Empleado.Nombre : ""
            })   // ¡nunca mostramos el hash!
            .ToList();
        dgvUsuarios.Columns["IdUsuario"]!.Visible = false;

        CargarEmpleadosUsuario();
    }

    /// <summary>Empleados activos que todavía no tienen usuario (para el combo del rol Trabajador).</summary>
    private void CargarEmpleadosUsuario()
    {
        using var db = new AppDbContext();
        cmbUsrEmpleado.DataSource = db.Empleados
            .Where(e => e.Activo && !db.Usuarios.Any(u => u.Activo && u.IdEmpleado == e.IdEmpleado))
            .OrderBy(e => e.Nombre)
            .Select(e => new ItemCombo { Id = e.IdEmpleado, Texto = e.Nombre })
            .ToList();
        cmbUsrEmpleado.DisplayMember = "Texto";
        cmbUsrEmpleado.ValueMember = "Id";
    }

    private void AgregarUsuario(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtUsrLogin.Text) || txtUsrPass.Text.Length < 4)
        {
            Aviso("Escriba el login y una contraseña de al menos 4 caracteres.");
            return;
        }
        string rol = cmbUsrRol.SelectedItem!.ToString()!;
        int? idEmpleado = null;
        if (rol == Roles.Trabajador)
        {
            if (cmbUsrEmpleado.SelectedItem is not ItemCombo empleado)
            {
                Aviso("Seleccione el empleado. (Todos los empleados activos ya tienen usuario.)");
                return;
            }
            idEmpleado = empleado.Id;
        }

        try
        {
            _auth.CrearUsuario(txtUsrLogin.Text.Trim(), txtUsrPass.Text, rol, idEmpleado);
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
            if (usuario.EsAdministrador && db.Usuarios.Count(u => u.Activo && u.Rol == Roles.Administrador) <= 1)
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
