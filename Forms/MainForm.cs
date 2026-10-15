using VisitasRecepcion.Models;
using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>Ventana principal: resumen del día y acceso a los módulos del sistema.</summary>
public class MainForm : Form
{
    private readonly Usuario _usuario;
    private readonly VisitaService _visitaService = new();

    // Números de las tarjetas de resumen
    private readonly Label lblHoy = new();
    private readonly Label lblDentro = new();
    private readonly Label lblMes = new();

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
            "Recepción  ·  Control de ingreso y salida");

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
        Controls.Add(CrearEstadistica("Visitas de hoy", lblHoy, Estilo.PrimarioClaro, 20));
        Controls.Add(CrearEstadistica("Dentro de la empresa ahora", lblDentro, Estilo.Advertencia, 340));
        Controls.Add(CrearEstadistica("Visitas del mes", lblMes, Estilo.Acento, 660));

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

        Controls.Add(CrearModulo(
            "Registrar visitas",
            "Ingreso de visitantes, aviso al anfitrión y registro de salida.",
            Estilo.Acento, 20, 272,
            () =>
            {
                using var f = new RegistroVisitaForm();
                f.ShowDialog(this);
                CargarResumen();
            }));

        Controls.Add(CrearModulo(
            "Reportes",
            "Consulte las visitas por día, semana o mes y expórtelas a Excel.",
            Estilo.PrimarioClaro, 340, 272,
            () =>
            {
                using var f = new ReporteForm();
                f.ShowDialog(this);
            }));

        // Solo el administrador ve el módulo de administración
        if (_usuario.Rol == "Administrador")
        {
            Controls.Add(CrearModulo(
                "Administración",
                "Áreas, empleados, límites permitidos y usuarios del sistema.",
                Estilo.Advertencia, 660, 272,
                () =>
                {
                    using var f = new AdminForm();
                    f.ShowDialog(this);
                }));
        }

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

        CargarResumen();
    }

    // ======================= RESUMEN =======================
    private void CargarResumen()
    {
        try
        {
            var (hoy, dentro, mes) = _visitaService.ObtenerResumen();
            lblHoy.Text = hoy.ToString();
            lblDentro.Text = dentro.ToString();
            lblMes.Text = mes.ToString();
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
        var tarjeta = Estilo.Tarjeta(x, 100, 300, 110);

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
        var tarjeta = Estilo.Tarjeta(x, y, 300, 190);

        var franja = new Panel { Left = 1, Top = 1, Width = 298, Height = 6, BackColor = color };

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
            Top = 74,
            Width = 260,
            Height = 62
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
