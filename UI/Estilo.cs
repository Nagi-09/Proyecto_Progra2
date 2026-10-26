namespace VisitasRecepcion.UI;

/// <summary>
/// Estilo visual común de todo el sistema: colores, fuentes y métodos
/// para darle el mismo aspecto a botones, tablas, tarjetas y encabezados.
/// Si quieren cambiar los colores del programa, solo editen este archivo.
/// </summary>
public static class Estilo
{
    // ===================== COLORES =====================
    public static readonly Color Primario = Color.FromArgb(30, 58, 95);          // azul oscuro
    public static readonly Color PrimarioClaro = Color.FromArgb(44, 82, 130);    // azul medio
    public static readonly Color Acento = Color.FromArgb(16, 150, 120);          // verde azulado
    public static readonly Color Exito = Color.FromArgb(16, 150, 120);
    public static readonly Color Advertencia = Color.FromArgb(217, 119, 6);      // naranja
    public static readonly Color Peligro = Color.FromArgb(197, 48, 48);          // rojo
    public static readonly Color Fondo = Color.FromArgb(241, 245, 249);          // gris muy claro
    public static readonly Color Blanco = Color.White;
    public static readonly Color Texto = Color.FromArgb(30, 41, 59);
    public static readonly Color TextoSuave = Color.FromArgb(100, 116, 139);
    public static readonly Color Borde = Color.FromArgb(226, 232, 240);
    public static readonly Color TextoSobreAzul = Color.FromArgb(186, 205, 230);

    // ===================== FUENTES =====================
    public static readonly Font Normal = new Font("Segoe UI", 10F);
    public static readonly Font Negrita = new Font("Segoe UI", 10F, FontStyle.Bold);
    public static readonly Font Pequena = new Font("Segoe UI", 9F);
    public static readonly Font TituloTarjetaFuente = new Font("Segoe UI Semibold", 11F);
    public static readonly Font Seccion = new Font("Segoe UI Semibold", 13F);
    public static readonly Font TituloMedio = new Font("Segoe UI Semibold", 16F);
    public static readonly Font TituloGrande = new Font("Segoe UI Semibold", 22F);
    public static readonly Font Numero = new Font("Segoe UI Semibold", 28F);

    // ===================== FORMULARIOS =====================
    /// <summary>Aplica el fondo, la fuente y el color de texto base a una ventana.</summary>
    public static void Formulario(Form f)
    {
        f.BackColor = Fondo;
        f.Font = Normal;
        f.ForeColor = Texto;
    }

    /// <summary>
    /// Se llama al FINAL del constructor de cada ventana, cuando ya tiene todos sus controles.
    /// Todas las posiciones del programa están pensadas para pantallas al 100 % (96 DPI);
    /// si la pantalla usa 125 %, 150 %, etc., aquí se agranda todo en la misma proporción.
    /// (Sin esto las letras crecen pero los cuadros no, y los textos se enciman o se cortan.)
    /// </summary>
    public static void AjustarAPantalla(Form f)
    {
        f.AutoScaleDimensions = new SizeF(96F, 96F);
        f.AutoScaleMode = AutoScaleMode.Dpi;   // al asignarlo, escala la ventana y todo lo que contiene

        // Si aun así la ventana no cabe en la pantalla, se recorta al espacio disponible
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        f.MinimumSize = new Size(Math.Min(f.MinimumSize.Width, area.Width), Math.Min(f.MinimumSize.Height, area.Height));
        f.Size = new Size(Math.Min(f.Width, area.Width), Math.Min(f.Height, area.Height));
    }

    /// <summary>
    /// Convierte píxeles "de 100 %" a píxeles reales según la escala de la pantalla.
    /// Sirve para medidas que Windows Forms no escala solo (alto de filas, tamaño de pestañas).
    /// </summary>
    public static int Escalar(Control c, int pixeles) => (int)Math.Round(pixeles * c.DeviceDpi / 96.0);

    /// <summary>Barra azul de arriba con título y subtítulo.</summary>
    public static Panel Encabezado(string titulo, string subtitulo)
    {
        var panel = new Panel { Dock = DockStyle.Top, Height = 84, BackColor = Primario };

        panel.Controls.Add(new Label
        {
            Text = titulo,
            Font = TituloMedio,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = true,
            Left = 24,
            Top = 12
        });

        panel.Controls.Add(new Label
        {
            Text = subtitulo,
            Font = Normal,
            ForeColor = TextoSobreAzul,
            BackColor = Color.Transparent,
            AutoSize = true,
            Left = 26,
            Top = 50
        });

        // Línea de color en la parte de abajo del encabezado
        panel.Controls.Add(new Panel { Dock = DockStyle.Bottom, Height = 3, BackColor = Acento });

        return panel;
    }

    // ===================== TARJETAS Y TEXTOS =====================
    /// <summary>Panel blanco con borde fino, para agrupar contenido.</summary>
    public static Panel Tarjeta(int x, int y, int ancho, int alto)
    {
        var panel = new Panel { Left = x, Top = y, Width = ancho, Height = alto, BackColor = Blanco };

        panel.Paint += (s, e) =>
        {
            using var pluma = new Pen(Borde);
            e.Graphics.DrawRectangle(pluma, 0, 0, panel.Width - 1, panel.Height - 1);
        };
        panel.Resize += (s, e) => panel.Invalidate();

        return panel;
    }

    /// <summary>Título de una tarjeta (azul, un poco más grande).</summary>
    public static Label TituloTarjeta(string texto, int x, int y)
    {
        return new Label
        {
            Text = texto,
            Left = x,
            Top = y,
            AutoSize = true,
            Font = TituloTarjetaFuente,
            ForeColor = Primario,
            BackColor = Color.Transparent
        };
    }

    /// <summary>Texto pequeño y gris que va arriba de cada campo.</summary>
    public static Label Caption(string texto, int x, int y)
    {
        return new Label
        {
            Text = texto,
            Left = x,
            Top = y,
            AutoSize = true,
            Font = Pequena,
            ForeColor = TextoSuave,
            BackColor = Color.Transparent
        };
    }

    // ===================== BOTONES =====================
    /// <summary>Botón plano de color, con efecto al pasar el mouse.</summary>
    public static void Boton(Button b, Color color)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 0;
        b.UseVisualStyleBackColor = false;
        b.BackColor = color;
        b.ForeColor = Color.White;
        b.Font = Negrita;
        b.Cursor = Cursors.Hand;

        var colorHover = Oscurecer(color, 18);
        b.MouseEnter += (s, e) => b.BackColor = colorHover;
        b.MouseLeave += (s, e) => b.BackColor = color;
    }

    /// <summary>Botón blanco con borde azul (para acciones secundarias).</summary>
    public static void BotonSecundario(Button b)
    {
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.FlatAppearance.BorderColor = Primario;
        b.UseVisualStyleBackColor = false;
        b.BackColor = Blanco;
        b.ForeColor = Primario;
        b.Font = Negrita;
        b.Cursor = Cursors.Hand;

        b.MouseEnter += (s, e) => b.BackColor = Fondo;
        b.MouseLeave += (s, e) => b.BackColor = Blanco;
    }

    public static Color Oscurecer(Color c, int cantidad)
    {
        return Color.FromArgb(
            Math.Max(0, c.R - cantidad),
            Math.Max(0, c.G - cantidad),
            Math.Max(0, c.B - cantidad));
    }

    // ===================== CAMPOS DE TEXTO / COMBOS =====================
    /// <summary>Recorre una ventana y le da estilo a todos sus TextBox, ComboBox, ListBox, etc.</summary>
    public static void Campos(Control raiz)
    {
        foreach (Control c in raiz.Controls)
        {
            switch (c)
            {
                case TextBox t:
                    t.BorderStyle = BorderStyle.FixedSingle;
                    t.Font = Normal;
                    break;
                case ComboBox cb:
                    // El estilo de Windows dibuja el borde; el plano lo pierde sobre fondo blanco
                    cb.FlatStyle = FlatStyle.System;
                    cb.Font = Normal;
                    break;
                case DateTimePicker d:
                    d.Font = Normal;
                    break;
                case ListBox lb:
                    lb.BorderStyle = BorderStyle.FixedSingle;
                    lb.Font = Normal;
                    break;
                case NumericUpDown n:
                    n.BorderStyle = BorderStyle.FixedSingle;
                    n.Font = Normal;
                    break;
            }

            if (c.HasChildren)
                Campos(c);
        }
    }

    // ===================== TABLAS (DataGridView) =====================
    public static void Grid(DataGridView g)
    {
        g.BackgroundColor = Blanco;
        g.BorderStyle = BorderStyle.None;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.GridColor = Borde;

        g.EnableHeadersVisualStyles = false;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersDefaultCellStyle.BackColor = Primario;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Primario;
        g.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.White;
        g.ColumnHeadersDefaultCellStyle.Font = Negrita;
        g.ColumnHeadersDefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        g.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;   // títulos en una sola línea
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersHeight = Escalar(g, 38);

        g.RowHeadersVisible = false;
        g.RowTemplate.Height = Escalar(g, 32);
        g.DefaultCellStyle.Font = Normal;
        g.DefaultCellStyle.ForeColor = Texto;
        g.DefaultCellStyle.BackColor = Blanco;
        g.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
        g.DefaultCellStyle.SelectionForeColor = Texto;
        g.DefaultCellStyle.Padding = new Padding(6, 0, 0, 0);
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);

        g.ReadOnly = true;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.AllowUserToResizeRows = false;
        g.MultiSelect = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        // Las columnas toman el nombre de la propiedad (sin tildes); aquí se les pone un título legible
        g.DataBindingComplete += (s, e) =>
        {
            foreach (DataGridViewColumn col in g.Columns)
            {
                if (TitulosColumnas.TryGetValue(col.Name, out var titulo))
                    col.HeaderText = titulo;
            }
        };
    }

    private static readonly Dictionary<string, string> TitulosColumnas = new()
    {
        ["Anfitrion"] = "Anfitrión",
        ["Aplica"] = "Aplica a",
        ["Area"] = "Área",
        ["Descripcion"] = "Descripción",
        ["DuracionMin"] = "Minutos",
        ["DuracionMaxMin"] = "Duración máx. (min)",
        ["Inicio"] = "Hora inicio",
        ["Fin"] = "Hora fin",
    };

    // ===================== ACOMODO =====================
    /// <summary>
    /// Hace que el control llegue hasta el borde derecho (y, si alto = true, hasta el de abajo)
    /// de su contenedor, dejando el mismo margen que tiene a la izquierda. Se mantiene al
    /// cambiar el tamaño de la ventana. Llamarlo después de agregar el control a su contenedor.
    /// (Sirve donde Anchor no funciona bien, por ejemplo dentro de las pestañas.)
    /// </summary>
    public static void Estirar(Control c, bool alto = true)
    {
        var contenedor = c.Parent!;
        c.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        void Acomodar()
        {
            int margen = c.Left;
            c.Width = Math.Max(0, contenedor.ClientSize.Width - c.Left - margen);
            if (alto)
                c.Height = Math.Max(0, contenedor.ClientSize.Height - c.Top - margen);
        }

        contenedor.Resize += (s, e) => Acomodar();
        Acomodar();
    }

    /// <summary>
    /// Pinta el estado del aviso (verde = leído, naranja = sin leer, rojo = no enviado)
    /// y de naranja las salidas pendientes (—).
    /// Se conecta al evento CellFormatting de la tabla.
    /// </summary>
    public static void FormatoCeldas(DataGridView g, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
        if (e.Value is not string texto || e.CellStyle == null) return;

        string columna = g.Columns[e.ColumnIndex].Name;

        if (columna == "Aviso" || columna == "Estado")
        {
            e.CellStyle.ForeColor = texto switch
            {
                "Leído" => Exito,
                "Sin leer" => Advertencia,
                _ => Peligro
            };
            e.CellStyle.Font = Negrita;
        }
        else if ((columna == "Tipo" && texto == "Tiempo terminado") || (columna == "Visita" && texto == "Tiempo excedido"))
        {
            e.CellStyle.ForeColor = Peligro;
            e.CellStyle.Font = Negrita;
        }
        else if (columna == "Visita" && texto.StartsWith("En curso"))
        {
            e.CellStyle.ForeColor = Exito;
        }
        else if (columna == "Salida" && texto == "—")
        {
            e.CellStyle.ForeColor = Advertencia;
            e.CellStyle.Font = Negrita;
        }
    }
}
