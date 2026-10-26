using Microsoft.EntityFrameworkCore;
using VisitasRecepcion.Models;

namespace VisitasRecepcion.Data;

/// <summary>
/// DbContext de Entity Framework Core (enfoque Code First).
/// Cada DbSet representa una tabla de SQL Server.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Visitante> Visitantes => Set<Visitante>();
    public DbSet<Empleado> Empleados => Set<Empleado>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Visita> Visitas => Set<Visita>();
    public DbSet<LimitePermitido> Limites => Set<LimitePermitido>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Aviso> Avisos => Set<Aviso>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(Config.CadenaConexion);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Tablas en singular: cada tabla se llama igual que su clase (Area, Visita, Aviso...).
        // Sin esto EF usaría el nombre del DbSet, que está en plural.
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
            entidad.SetTableName(entidad.ClrType.Name);

        // Llaves primarias: EF solo reconoce por convención "Id" o "<Clase>Id",
        // y nuestras propiedades se llaman IdArea, IdVisitante, etc., así que las declaramos.
        modelBuilder.Entity<Area>().HasKey(a => a.IdArea);
        modelBuilder.Entity<Empleado>().HasKey(e => e.IdEmpleado);
        modelBuilder.Entity<Visitante>().HasKey(v => v.IdVisitante);
        modelBuilder.Entity<Visita>().HasKey(v => v.IdVisita);
        modelBuilder.Entity<LimitePermitido>().HasKey(l => l.IdLimite);
        modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);
        modelBuilder.Entity<Aviso>().HasKey(a => a.IdAviso);

        // Relación Visita -> Aviso (cada visita genera su aviso para el anfitrión)
        modelBuilder.Entity<Aviso>()
            .HasOne(a => a.Visita).WithMany(v => v.Avisos)
            .HasForeignKey(a => a.IdVisita).OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Aviso>()
            .HasOne(a => a.Empleado).WithMany(e => e.Avisos)
            .HasForeignKey(a => a.IdEmpleado).OnDelete(DeleteBehavior.Restrict);

        // La pantalla de avisos busca por anfitrión y por "sin leer"
        modelBuilder.Entity<Aviso>().HasIndex(a => new { a.IdEmpleado, a.Leido });
        modelBuilder.Entity<Aviso>().Property(a => a.Tipo).HasMaxLength(20);

        // HoraLimite se calcula (entrada + duración), no es una columna
        modelBuilder.Entity<Visita>().Ignore(v => v.HoraLimite);

        // El login y el documento no se pueden repetir
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Login).IsUnique();
        modelBuilder.Entity<Visitante>().HasIndex(v => v.Documento).IsUnique();

        // SQL Server no permite varias rutas de borrado en cascada,
        // por eso usamos Restrict (no se borra en cadena).
        modelBuilder.Entity<Visita>()
            .HasOne(v => v.Visitante).WithMany(x => x.Visitas)
            .HasForeignKey(v => v.IdVisitante).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Visita>()
            .HasOne(v => v.Empleado).WithMany(e => e.Visitas)
            .HasForeignKey(v => v.IdEmpleado).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Visita>()
            .HasOne(v => v.Area).WithMany(a => a.Visitas)
            .HasForeignKey(v => v.IdArea).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Empleado>()
            .HasOne(e => e.Area).WithMany(a => a.Empleados)
            .HasForeignKey(e => e.IdArea).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<LimitePermitido>()
            .HasOne(l => l.Area).WithMany(a => a.Limites)
            .HasForeignKey(l => l.IdArea).OnDelete(DeleteBehavior.Restrict);

        // Usuario (rol Trabajador) -> Empleado al que pertenece la cuenta
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Empleado).WithMany()
            .HasForeignKey(u => u.IdEmpleado).OnDelete(DeleteBehavior.Restrict);
    }

    /// <summary>
    /// Crea la base de datos si no existe y agrega los datos iniciales
    /// (usuario admin, límite genérico y un área de ejemplo).
    /// Se llama una sola vez al iniciar el programa.
    /// </summary>
    public static void Inicializar()
    {
        using var db = new AppDbContext();
        db.Database.EnsureCreated();
        PrepararTablaAvisos(db);
        PrepararUsuarioTrabajador(db);
        PrepararTiempoVisita(db);

        // Usuario administrador por defecto
        if (!db.Usuarios.Any())
        {
            db.Usuarios.Add(new Usuario
            {
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Rol = Roles.Administrador
            });
        }

        // Límite genérico para visitantes nuevos
        if (!db.Limites.Any(l => l.EsGenerico))
        {
            db.Limites.Add(new LimitePermitido
            {
                IdArea = null,
                EsGenerico = true,
                HoraInicio = TimeSpan.Parse(Config.HoraInicioGenerica),
                HoraFin = TimeSpan.Parse(Config.HoraFinGenerica),
                DuracionMaxMin = Config.DuracionGenericaMin
            });
        }

        // Un área de ejemplo para poder probar el sistema
        if (!db.Areas.Any())
        {
            db.Areas.Add(new Area { Nombre = "Recepción", Descripcion = "Área general" });
        }

        db.SaveChanges();

        // Datos de ejemplo para probar el sistema (se desactiva en Config.cs)
        if (Config.CargarDatosEjemplo)
        {
            DatosEjemplo.Cargar(db);
            DatosEjemplo.CrearUsuariosTrabajadores(db);
        }
    }

    /// <summary>
    /// Agrega la columna Usuario.IdEmpleado (para el rol Trabajador) si la base
    /// se creó antes de que existiera ese rol.
    /// </summary>
    private static void PrepararUsuarioTrabajador(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
            IF COL_LENGTH(N'dbo.Usuario', N'IdEmpleado') IS NULL
            BEGIN
                ALTER TABLE dbo.Usuario ADD IdEmpleado INT NULL
                    CONSTRAINT FK_Usuario_Empleado_IdEmpleado REFERENCES dbo.Empleado (IdEmpleado);
                EXEC(N'CREATE INDEX IX_Usuario_IdEmpleado ON dbo.Usuario (IdEmpleado);');
            END");
    }

    /// <summary>
    /// Agrega las columnas Visita.DuracionMaxMin y Aviso.Tipo si la base se creó antes de
    /// que existiera el aviso de "tiempo terminado". Las visitas viejas quedan con el límite genérico.
    /// </summary>
    private static void PrepararTiempoVisita(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw($@"
            IF COL_LENGTH(N'dbo.Visita', N'DuracionMaxMin') IS NULL
                ALTER TABLE dbo.Visita ADD DuracionMaxMin INT NOT NULL
                    CONSTRAINT DF_Visita_DuracionMaxMin DEFAULT {Config.DuracionGenericaMin};

            IF COL_LENGTH(N'dbo.Aviso', N'Tipo') IS NULL
                ALTER TABLE dbo.Aviso ADD Tipo NVARCHAR(20) NOT NULL
                    CONSTRAINT DF_Aviso_Tipo DEFAULT N'{TiposAviso.Llegada}';");
    }

    /// <summary>
    /// EnsureCreated() no agrega tablas nuevas a una base que ya existe.
    /// Si la base se creó con la versión anterior (avisos por correo), aquí se crea
    /// la tabla Aviso para no tener que borrar la base de datos.
    /// </summary>
    private static void PrepararTablaAvisos(AppDbContext db)
    {
        db.Database.ExecuteSqlRaw(@"
            IF OBJECT_ID(N'dbo.Aviso', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Aviso (
                    IdAviso       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Aviso PRIMARY KEY,
                    IdVisita      INT NOT NULL CONSTRAINT FK_Aviso_Visita_IdVisita
                                  REFERENCES dbo.Visita (IdVisita) ON DELETE CASCADE,
                    IdEmpleado    INT NOT NULL CONSTRAINT FK_Aviso_Empleado_IdEmpleado
                                  REFERENCES dbo.Empleado (IdEmpleado),
                    Mensaje       NVARCHAR(MAX) NOT NULL,
                    FechaCreacion DATETIME2 NOT NULL,
                    Leido         BIT NOT NULL,
                    FechaLectura  DATETIME2 NULL
                );
                CREATE INDEX IX_Aviso_IdVisita ON dbo.Aviso (IdVisita);
                CREATE INDEX IX_Aviso_IdEmpleado_Leido ON dbo.Aviso (IdEmpleado, Leido);
            END");

        // Visitas de la versión anterior que fueron notificadas por correo: les creamos su aviso
        // (leído si la visita ya terminó). Solo toca visitas sin aviso, así que no duplica nada.
        db.Database.ExecuteSqlRaw(@"
            INSERT INTO dbo.Aviso (IdVisita, IdEmpleado, Mensaje, FechaCreacion, Leido, FechaLectura)
            SELECT v.IdVisita, v.IdEmpleado,
                   vt.Nombre + N' llegó a recepción para verte (' + a.Nombre + N'). Motivo: ' + v.Motivo + N'.',
                   v.FechaHoraEntrada,
                   CASE WHEN v.FechaHoraSalida IS NULL THEN 0 ELSE 1 END,
                   v.FechaHoraSalida
            FROM dbo.Visita v
            JOIN dbo.Visitante vt ON vt.IdVisitante = v.IdVisitante
            JOIN dbo.Area a ON a.IdArea = v.IdArea
            WHERE v.NotificacionEnviada = 1
              AND NOT EXISTS (SELECT 1 FROM dbo.Aviso x WHERE x.IdVisita = v.IdVisita)");
    }
}
