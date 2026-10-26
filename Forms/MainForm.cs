using VisitasRecepcion.Models;
using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>Ventana principal: resumen del día y acceso a los módulos del sistema.</summary>
public class MainForm : Form
{
    private readonly Usuario _usuario;
    private readonly VisitaService _visitaService = new();
    private readonly NotificacionService _notificacionService = new();

    // Números de las tarjetas de resumen
    private readonly Label lblHoy = new();
    private readonly Label lblDentro = new();
    private readonly Label lblMes = new();
    private readonly Label lblAvisos = new();

    // Cuatro columnas de 220 px con 20 px de separación
    private const int AnchoTarjeta = 220;
    private static int Columna(int i) => 20 + i * (AnchoTarjeta + 20);

    public MainForm(Usuario usuario)
    {
        _usuario = usuario;

        Text = "Sistema de Registro de Visitas";
        ClientSize = new Size(980, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Estilo.Formulario(this);

        // ---------- Encabezado ----------
        var encabezado = Estilo.Encabezado(
            "Sistema de Registro de Visitas",
            _usuario.EsTrabajador
                ? $"Hola, {_usuario.Empleado?.Nombre}  ·  Sus visitas y avisos"
                : "Recepción  ·  Control de ingreso y salida");

        var lblUsuario = new Label
        {
            Text = $"{_usuario.Login}  ·  {_usuario.Rol}",
            Font = Estilo.Negrita,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Width = 360,
            Height = 30,
            Left = ClientSize.Width - 384,
            Top = 24,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        encabezado.Controls.Add(lblUsuario);
        Controls.Add(encabezado);

        // ---------- Tarjetas de resumen ----------
        // El trabajador ve los mismos números, pero solo de sus propias visitas
        bool propio = _usuario.EsTrabajador;
        Controls.Add(CrearEstadistica(propio ? "Mis visitas de hoy" : "Visitas de hoy", lblHoy, Estilo.PrimarioClaro, Columna(0)));
        Controls.Add(CrearEstadistica(propio ? "Me esperan ahora" : "Dentro de la empresa ahora", lblDentro, Estilo.Advertencia, Columna(1)));
        Controls.Add(CrearEstadistica(propio ? "Mis visitas del mes" : "Visitas del mes", lblMes, Estilo.Acento, Columna(2)));
        Controls.Add(CrearEstadistica(propio ? "Mis avisos sin leer" : "Avisos sin leer", lblAvisos, Estilo.Peligro, Columna(3)));

        // ---------- Módulos ----------
        Controls.Add(new Label
        {
            Text = "Módulos",
            Font = Estilo.Seccion,
            ForeColor = Estilo.Texto,
            AutoSize = true,
            Left = 20,
            Top = 232
        });

        // Cada rol ve solo sus módulos; se acomodan en columnas seguidas, sin huecos
        int columna = 0;
        void AgregarModulo(string titulo, string descripcion, Color color, Func<Form> crearVentana)
        {
            Controls.Add(CrearModulo(titulo, descripcion, color, Columna(columna++), 272, () =>
            {
                using var f = crearVentana();
                f.ShowDialog(this);
                CargarResumen();
            }));
        }

        if (_usuario.EsTrabajador)
        {
            // Trabajador: solo sus avisos
            AgregarModulo("Mis avisos",
                "Vea quién lo espera en recepción y marque sus avisos como leídos.",
                Estilo.Advertencia, () => new AvisosForm(_usuario.IdEmpleado));
        }
        else
        {
            // Recepcionista y Administrador
            AgregarModulo("Registrar visitas",
                "Ingreso de visitantes, aviso al anfitrión y registro de salida.",
                Estilo.Acento, () => new RegistroVisitaForm());

            AgregarModulo("Avisos",
                "El anfitrión elige su nombre y ve quién lo espera en recepción.",
                Estilo.Advertencia, () => new AvisosForm());

            AgregarModulo("Reportes",
                "Consulte las visitas por día, semana o mes y expórtelas a Excel.",
                Estilo.PrimarioClaro, () => new ReporteForm());

            // Solo el administrador ve el módulo de administración
            if (_usuario.EsAdministrador)
            {
                AgregarModulo("Administración",
                    "Áreas, empleados, límites permitidos y usuarios del sistema.",
                    Estilo.Primario, () => new AdminForm());
            }
        }

        // Cada pocos segundos: revisa visitas con el tiempo terminado, actualiza los números
        // y, si es un trabajador, le avisa con una notificación de Windows
        var timer = new System.Windows.Forms.Timer { Interval = Config.SegundosActualizarAvisos * 1000 };
        timer.Tick += (s, e) => Revisar();
        timer.Start();
        FormClosed += (s, e) => timer.Dispose();

        if (_usuario.EsTrabajador)
            PrepararNotificaciones();

        // ---------- Pie de página ----------
        Controls.Add(new Label
        {
            Text = "Universidad Mariano Gálvez  ·  Programación 2  ·  Proyecto final",
            Font = Estilo.Pequena,
            ForeColor = Estilo.TextoSuave,
            AutoSize = true,
            Left = 20,
            Top = 548
        });

        var btnSalir = new Button { Text = "Salir", Left = 820, Top = 534, Width = 140, Height = 38 };
        Estilo.BotonSecundario(btnSalir);
        btnSalir.Click += (s, e) => Close();
        Controls.Add(btnSalir);

        Revisar();
        Estilo.AjustarAPantalla(this);
    }

    // ======================= REVISIÓN PERIÓDICA =======================
    private void Revisar()
    {
        try
        {
            _notificacionService.RevisarTiemposTerminados();
        }
        catch
        {
            // Si falla la conexión un momento, se reintenta en el siguiente ciclo
        }

        CargarResumen();
        NotificarNuevos();
    }

    // ======================= NOTIFICACIONES (rol Trabajador) =======================
    // Ícono junto al reloj de Windows; muestra un globo cuando llega un aviso nuevo.
    private NotifyIcon? _icono;
    private int _ultimoAvisoNotificado;

    private void PrepararNotificaciones()
    {
        _icono = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "Registro de Visitas - Mis avisos",
            Visible = true
        };
        _icono.BalloonTipClicked += (s, e) => AbrirMisAvisos();
        _icono.DoubleClick += (s, e) => AbrirMisAvisos();
        FormClosed += (s, e) => _icono.Dispose();   // si no, el ícono se queda junto al reloj

        // Solo se notifican los avisos que lleguen de aquí en adelante;
        // los que ya estaban pendientes se resumen en un solo globo al entrar.
        _ultimoAvisoNotificado = _notificacionService.UltimoIdAviso(_usuario.IdEmpleado!.Value);
        Shown += (s, e) =>
        {
            int pendientes = _notificacionService.ContarSinLeer(_usuario.IdEmpleado);
            if (pendientes > 0)
                _icono.ShowBalloonTip(5000, "Avisos pendientes",
                    $"Tiene {pendientes} aviso{(pendientes == 1 ? "" : "s")} sin leer.", ToolTipIcon.Info);
        };
    }

    private void NotificarNuevos()
    {
        if (_icono == null) return;

        try
        {
            var nuevos = _notificacionService.ObtenerNuevos(_usuario.IdEmpleado!.Value, _ultimoAvisoNotificado);
            if (nuevos.Count == 0) return;

            _ultimoAvisoNotificado = nuevos.Max(a => a.IdAviso);

            // Se muestra el más reciente; si llegaron varios juntos, se dice cuántos
            var ultimo = nuevos[^1];
            string titulo = ultimo.Tipo == TiposAviso.TiempoTerminado
                ? "Terminó el tiempo de una visita"
                : "Tiene una visita en recepción";
            string texto = nuevos.Count == 1
                ? ultimo.Mensaje
                : $"{ultimo.Mensaje}\n(y {nuevos.Count - 1} aviso{(nuevos.Count == 2 ? "" : "s")} más)";

            _icono.ShowBalloonTip(8000, titulo, texto,
                ultimo.Tipo == TiposAviso.TiempoTerminado ? ToolTipIcon.Warning : ToolTipIcon.Info);
        }
        catch
        {
            // La notificación es solo una ayuda: si falla, el aviso igual está en "Mis avisos"
        }
    }

    private void AbrirMisAvisos()
    {
        // Si ya está abierta (o hay otra ventana encima), no se abre otra
        if (OwnedForms.Length > 0 || Application.OpenForms.OfType<AvisosForm>().Any()) return;

        using var f = new AvisosForm(_usuario.IdEmpleado);
        f.ShowDialog(this);
        CargarResumen();
    }

    // ======================= RESUMEN =======================
    private void CargarResumen()
    {
        try
        {
            // null = todos; para el trabajador, solo su empleado
            int? idEmpleado = _usuario.EsTrabajador ? _usuario.IdEmpleado : null;

            var (hoy, dentro, mes) = _visitaService.ObtenerResumen(idEmpleado);
            lblHoy.Text = hoy.ToString();
            lblDentro.Text = dentro.ToString();
            lblMes.Text = mes.ToString();
            lblAvisos.Text = _notificacionService.ContarSinLeer(idEmpleado).ToString();
        }
        catch
        {
            // Si falla la consulta dejamos los números en 0: el resumen es solo informativo
        }
    }

    // ======================= COMPONENTES =======================
    /// <summary>Tarjeta con un número grande (visitas hoy, dentro ahora, etc.).</summary>
    private static Panel CrearEstadistica(string titulo, Label lblNumero, Color color, int x)
    {
        var tarjeta = Estilo.Tarjeta(x, 100, AnchoTarjeta, 110);

        // Barrita de color a la izquierda
        tarjeta.Controls.Add(new Panel { Left = 1, Top = 1, Width = 5, Height = 108, BackColor = color });

        tarjeta.Controls.Add(Estilo.Caption(titulo, 24, 16));

        lblNumero.AutoSize = true;
        lblNumero.Font = Estilo.Numero;
        lblNumero.ForeColor = color;
        lblNumero.BackColor = Color.Transparent;
        lblNumero.Left = 22;
        lblNumero.Top = 40;
        lblNumero.Text = "0";
        tarjeta.Controls.Add(lblNumero);

        return tarjeta;
    }

    /// <summary>Tarjeta grande y clickeable que abre un módulo.</summary>
    private static Panel CrearModulo(string titulo, string descripcion, Color color, int x, int y, Action alAbrir)
    {
        var tarjeta = Estilo.Tarjeta(x, y, AnchoTarjeta, 190);

        var franja = new Panel { Left = 1, Top = 1, Width = AnchoTarjeta - 2, Height = 6, BackColor = color };

        var lblTitulo = new Label
        {
            Text = titulo,
            Font = Estilo.Seccion,
            ForeColor = Estilo.Texto,
            BackColor = Color.Transparent,
            AutoSize = true,
            Left = 20,
            Top = 32
        };

        var lblDescripcion = new Label
        {
            Text = descripcion,
            Font = Estilo.Normal,
            ForeColor = Estilo.TextoSuave,
            BackColor = Color.Transparent,
            AutoSize = false,
            Left = 20,
            Top = 70,
            Width = AnchoTarjeta - 40,
            Height = 72
        };

        var lblAbrir = new Label
        {
            Text = "Abrir  →",
            Font = Estilo.Negrita,
            ForeColor = color,
            BackColor = Color.Transparent,
            AutoSize = true,
            Left = 20,
            Top = 148
        };

        tarjeta.Controls.AddRange(new Control[] { franja, lblTitulo, lblDescripcion, lblAbrir });

        // Que todo el cuadro responda al clic (también las etiquetas de adentro)
        foreach (Control c in new Control[] { tarjeta, franja, lblTitulo, lblDescripcion, lblAbrir })
        {
            c.Cursor = Cursors.Hand;
            c.Click += (s, e) => alAbrir();
        }

        return tarjeta;
    }
}
