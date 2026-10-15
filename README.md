# Sistema de Registro de Visitas en Recepción

Proyecto Final – Programación 2 (Fase 2/3: base de datos + formularios).
C# · Windows Forms · .NET 8 · Entity Framework Core 8 · SQL Server Express · ClosedXML

## Requisitos

- Windows con **Visual Studio 2022** (carga de trabajo "Desarrollo de escritorio con .NET").
- **SQL Server Express** instalado (instancia `SQLEXPRESS`).
- Opcional: un servidor SMTP de pruebas para los correos (por ejemplo [smtp4dev](https://github.com/rnwood/smtp4dev)).

## Cómo ejecutarlo

1. Abrir `VisitasRecepcion.csproj` con Visual Studio.
2. Revisar `Config.cs`:
   - `CadenaConexion`: si su instancia no es `localhost\SQLEXPRESS`, cámbienla.
   - Datos SMTP (`SmtpServidor`, `SmtpPuerto`, etc.).
3. Presionar **F5**. Los paquetes NuGet se restauran solos.
4. La primera vez, el programa **crea la base de datos** `VisitasRecepcionDB` y los datos iniciales.
5. Iniciar sesión con:  **usuario:** `admin`  ·  **contraseña:** `admin123`
6. Entrar a **Administración** y crear al menos un **Área** y un **Empleado** (anfitrión, con correo).
   Después ya se pueden registrar visitas.

## Estructura (arquitectura en capas)

```
VisitasRecepcion/
├── Program.cs              Punto de entrada: crea la BD y abre el login
├── Config.cs               Cadena de conexión y datos SMTP
├── Models/                 Entidades (tablas)
│   ├── Visitante.cs  Empleado.cs  Area.cs  Visita.cs
│   └── LimitePermitido.cs  Usuario.cs  LogNotificacion.cs
├── UI/
│   └── Estilo.cs           Colores, fuentes y estilos comunes (cambien aquí el aspecto del programa)
├── Data/                   CAPA DE DATOS
│   ├── AppDbContext.cs     DbContext de EF Core (Code First) + datos iniciales
│   └── DatosEjemplo.cs     Datos de ejemplo (se desactivan con Config.CargarDatosEjemplo)
├── Services/               CAPA DE NEGOCIO
│   ├── AuthService.cs          Login y usuarios (hash BCrypt)
│   ├── VisitaService.cs        Registro de visitas, límites, salida
│   ├── NotificacionService.cs  Correo al anfitrión (SMTP) + bitácora
│   ├── ReporteService.cs       Reporte día / semana / mes
│   ├── ExcelService.cs         Exportar a .xlsx (ClosedXML)
│   └── FilaReporte.cs          Fila para la grilla y el Excel
└── Forms/                  CAPA DE PRESENTACIÓN
    ├── LoginForm.cs  MainForm.cs  RegistroVisitaForm.cs
    └── ReporteForm.cs  AdminForm.cs
```

## Requisitos del documento de Fase 1 que cubre

| Requisito | Dónde está |
|-----------|-----------|
| RF-01 Registrar visitante | `RegistroVisitaForm` + `VisitaService.RegistrarVisita` |
| RF-02 Buscar visitante | `VisitaService.BuscarVisitantes` |
| RF-03 Límite genérico a visitantes nuevos | `VisitaService.RegistrarVisita` |
| RF-04 Límites por área | `AdminForm` (pestaña Límites) |
| RF-05 Correo al anfitrión | `NotificacionService` |
| RF-06 Salida con un clic | `VisitaService.RegistrarSalida` |
| RF-07 Reporte día/semana/mes en DataGridView | `ReporteForm` + `ReporteService` |
| RF-08 DateTimePicker | `ReporteForm` |
| RF-09 Exportar a Excel | `ExcelService` |
| RF-10 Gestión de empleados, áreas y usuarios | `AdminForm` |
| RNF-04 Login | `LoginForm` + `AuthService` |
| RNF-05 Capas | carpetas `Forms` / `Services` / `Data` |
| RNF-06 BD normalizada | `Empleado` guarda `IdArea` (llave foránea) |

## Notas / decisiones de diseño

- **Contraseñas:** se guarda el hash con BCrypt, nunca el texto (`Usuario.PasswordHash`).
- **Bajas lógicas:** áreas, empleados y usuarios se desactivan (`Activo = false`) para no
  perder el historial de visitas.
- **Empleado → Área:** en el diagrama de la Fase 1 `Area` era un texto dentro de `Empleado`;
  aquí es una llave foránea (`IdArea`) para cumplir la 3FN. Actualicen el diagrama ER si lo entregan.
- **Límites:** son informativos. Si la visita se registra fuera del horario, el sistema muestra
  una advertencia pero igual la guarda (el documento los define como reglas de referencia).
- **Base de datos:** se usa `EnsureCreated()` por simplicidad. Si cambian los modelos después de
  crear la BD, borren la base `VisitasRecepcionDB` desde SSMS y vuelvan a ejecutar
  (o migren a *Migrations* de EF Core).
- Si el correo falla, la visita **sí se guarda** y el error queda en la tabla `LogsNotificacion`.

## Posibles mejoras

- Usar `Migrations` en lugar de `EnsureCreated`.
- Leer la configuración desde un `appsettings.json`.
- Bloquear el registro si el visitante ya tiene una visita abierta.
