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
    public DbSet<LogNotificacion> LogsNotificacion => Set<LogNotificacion>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(Config.CadenaConexion);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Llaves primarias: EF solo reconoce por convención "Id" o "<Clase>Id",
        // y nuestras propiedades se llaman IdArea, IdVisitante, etc., así que las declaramos.
        modelBuilder.Entity<Area>().HasKey(a => a.IdArea);
        modelBuilder.Entity<Empleado>().HasKey(e => e.IdEmpleado);
        modelBuilder.Entity<Visitante>().HasKey(v => v.IdVisitante);
        modelBuilder.Entity<Visita>().HasKey(v => v.IdVisita);
        modelBuilder.Entity<LimitePermitido>().HasKey(l => l.IdLimite);
        modelBuilder.Entity<Usuario>().HasKey(u => u.IdUsuario);
        modelBuilder.Entity<LogNotificacion>().HasKey(l => l.IdLog);

        // Relación Visita -> LogNotificacion (1 visita puede tener N registros de log)
        modelBuilder.Entity<LogNotificacion>()
            .HasOne(l => l.Visita).WithMany(v => v.Logs)
            .HasForeignKey(l => l.IdVisita).OnDelete(DeleteBehavior.Cascade);

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

        // Usuario administrador por defecto
        if (!db.Usuarios.Any())
        {
            db.Usuarios.Add(new Usuario
            {
                Login = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Rol = "Administrador"
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
            DatosEjemplo.Cargar(db);
    }
}
