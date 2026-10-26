using VisitasRecepcion.Models;
using VisitasRecepcion.Services;
using VisitasRecepcion.UI;

namespace VisitasRecepcion.Forms;

/// <summary>Pantalla de inicio de sesión (RNF-04).</summary>
public class LoginForm : Form
{
    private readonly TextBox txtLogin = new();
    private readonly TextBox txtPassword = new();
    private readonly Button btnIngresar = new();
    private readonly AuthService _auth = new();

    // El Program.cs lee este valor para saber quién inició sesión
    public Usuario? UsuarioAutenticado { get; private set; }

    public LoginForm()
    {
        Text = "Registro de Visitas - Iniciar sesión";
        ClientSize = new Size(760, 440);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Estilo.Formulario(this);
        BackColor = Color.White;

        // ---------- Panel izquierdo (marca) ----------
        var panelMarca = new Panel { Left = 0, Top = 0, Width = 300, Height = 440, BackColor = Estilo.Primario };

        var franja = new Panel { Left = 34, Top = 128, Width = 56, Height = 5, BackColor = Estilo.Acento };

        var lblMarca = new Label
        {
            Text = "Registro" + Environment.NewLine + "de Visitas",
            Font = Estilo.TituloGrande,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Left = 30,
            Top = 148
        };

        var lblLema = new Label
        {
            Text = "Control de ingreso y salida" + Environment.NewLine + "en recepción, simple y ordenado.",
            Font = Estilo.Normal,
            ForeColor = Estilo.TextoSobreAzul,
            BackColor = Color.Transparent,
            AutoSize = true,
            Left = 34,
            Top = 255
        };

        panelMarca.Controls.AddRange(new Control[] { franja, lblMarca, lblLema });

        // ---------- Lado derecho (formulario) ----------
        var lblTitulo = new Label
        {
            Text = "Bienvenido",
            Font = Estilo.TituloMedio,
            ForeColor = Estilo.Texto,
            AutoSize = true,
            Left = 348,
            Top = 80
        };

        var lblSub = new Label
        {
            Text = "Inicie sesión para continuar",
            Font = Estilo.Normal,
            ForeColor = Estilo.TextoSuave,
            AutoSize = true,
            Left = 350,
            Top = 120
        };

        txtLogin.SetBounds(350, 207, 360, 30);
        txtLogin.PlaceholderText = "Escriba su usuario";

        txtPassword.SetBounds(350, 277, 360, 30);
        txtPassword.PlaceholderText = "Escriba su contraseña";
        txtPassword.UseSystemPasswordChar = true;   // muestra puntitos

        btnIngresar.Text = "Ingresar";
        btnIngresar.SetBounds(350, 340, 360, 44);
        Estilo.Boton(btnIngresar, Estilo.Primario);
        btnIngresar.Click += BtnIngresar_Click;

        Controls.AddRange(new Control[]
        {
            panelMarca, lblTitulo, lblSub,
            Estilo.Caption("Usuario", 352, 185), txtLogin,
            Estilo.Caption("Contraseña", 352, 255), txtPassword,
            btnIngresar
        });

        Estilo.Campos(this);
        txtLogin.Font = new Font("Segoe UI", 11F);
        txtPassword.Font = new Font("Segoe UI", 11F);

        // Enter = clic en Ingresar
        AcceptButton = btnIngresar;

        Estilo.AjustarAPantalla(this);
    }

    private void BtnIngresar_Click(object? sender, EventArgs e)
    {
        var usuario = _auth.Login(txtLogin.Text.Trim(), txtPassword.Text);

        if (usuario == null)
        {
            MessageBox.Show("Usuario o contraseña incorrectos.", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.Clear();
            txtPassword.Focus();
            return;
        }

        UsuarioAutenticado = usuario;
        DialogResult = DialogResult.OK;   // cierra el login
    }
}
