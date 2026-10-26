# Sistema de Registro de Visitas en Recepción

Proyecto Final – Programación 2 (Fase 2/3: base de datos + formularios).
C# · Windows Forms · .NET 8 · Entity Framework Core 8 · SQL Server Express · ClosedXML

## Requisitos

- Windows con **Visual Studio 2022** (carga de trabajo "Desarrollo de escritorio con .NET").
- **SQL Server Express** instalado (instancia `SQLEXPRESS`).

## Cómo ejecutarlo

1. Abrir `VisitasRecepcion.csproj` con Visual Studio.
2. Revisar `Config.cs`:
   - `CadenaConexion`: si su instancia no es `localhost\SQLEXPRESS`, cámbienla.
   - `SegundosActualizarAvisos`: cada cuánto se refresca la pantalla de avisos (15 s por defecto).
3. Presionar **F5**. Los paquetes NuGet se restauran solos.
4. La primera vez, el programa **crea la base de datos** `VisitasRecepcionDB` y los datos iniciales.
5. Iniciar sesión con:  **usuario:** `admin`  ·  **contraseña:** `admin123`
   (con los datos de ejemplo también existen `recepcion` / `recepcion123` y un usuario por empleado,
   por ejemplo `amorales` / `trabajador123`).
6. Entrar a **Administración** y crear al menos un **Área** y un **Empleado** (anfitrión).
   Después ya se pueden registrar visitas.

## Estructura (arquitectura en capas)

```
VisitasRecepcion/
├── Program.cs              Punto de entrada: crea la BD y abre el login
├── Config.cs               Cadena de conexión y opciones generales
├── Models/                 Entidades (tablas)
│   ├── Visitante.cs  Empleado.cs  Area.cs  Visita.cs
│   └── LimitePermitido.cs  Usuario.cs  Aviso.cs
├── UI/
│   └── Estilo.cs           Colores, fuentes y estilos comunes (cambien aquí el aspecto del programa)
├── Data/                   CAPA DE DATOS
│   ├── AppDbContext.cs     DbContext de EF Core (Code First) + datos iniciales
│   └── DatosEjemplo.cs     Datos de ejemplo (se desactivan con Config.CargarDatosEjemplo)
├── Services/               CAPA DE NEGOCIO
│   ├── AuthService.cs          Login y usuarios (hash BCrypt)
│   ├── VisitaService.cs        Registro de visitas, límites, salida
│   ├── NotificacionService.cs  Avisos internos al anfitrión (tabla Aviso)
│   ├── ReporteService.cs       Reporte día / semana / mes
│   ├── ExcelService.cs         Exportar a .xlsx (ClosedXML)
│   └── FilaReporte.cs          Fila para la grilla y el Excel
└── Forms/                  CAPA DE PRESENTACIÓN
    ├── LoginForm.cs  MainForm.cs  RegistroVisitaForm.cs
    └── AvisosForm.cs  ReporteForm.cs  AdminForm.cs
```

## Requisitos del documento de Fase 1 que cubre

| Requisito | Dónde está |
|-----------|-----------|
| RF-01 Registrar visitante | `RegistroVisitaForm` + `VisitaService.RegistrarVisita` |
| RF-02 Buscar visitante | `VisitaService.BuscarVisitantes` |
| RF-03 Límite genérico a visitantes nuevos | `VisitaService.RegistrarVisita` |
| RF-04 Límites por área | `AdminForm` (pestaña Límites) |
| RF-05 Aviso al anfitrión | `NotificacionService` + `AvisosForm` |
| RF-06 Salida con un clic | `VisitaService.RegistrarSalida` |
| RF-07 Reporte día/semana/mes en DataGridView | `ReporteForm` + `ReporteService` |
| RF-08 DateTimePicker | `ReporteForm` |
| RF-09 Exportar a Excel | `ExcelService` |
| RF-10 Gestión de empleados, áreas y usuarios | `AdminForm` |
| RNF-04 Login | `LoginForm` + `AuthService` |
| Roles y permisos | `Models/Usuario.cs` (`Roles`) + `MainForm` |
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
- **Roles:** **Administrador** ve todo; **Recepcionista** ve registro, avisos y reportes (sin Administración);
  **Trabajador** es un empleado con su propio usuario: solo ve sus avisos y los números de sus visitas.
  El trabajador se liga a su empleado en Administración → Usuarios; si el empleado se da de baja, ya no puede entrar.
- **Tiempo de la visita:** cada visita guarda su duración máxima (`Visita.DuracionMaxMin`, según el límite que aplicó).
  Cuando se cumple y el visitante sigue dentro, el anfitrión recibe un aviso de "Tiempo terminado" y, si tiene
  sesión abierta como Trabajador, una notificación de Windows. Desde "Mis avisos" puede **terminar la visita**
  antes de tiempo (registra la salida con la hora actual).
- **Tablas en singular:** cada tabla se llama igual que su clase (`Area`, `Visita`, `Aviso`...),
  lo define `AppDbContext.OnModelCreating`.
- **Base de datos:** se usa `EnsureCreated()` por simplicidad. Si cambian los modelos después de
  crear la BD, borren la base `VisitasRecepcionDB` desde SSMS y vuelvan a ejecutar
  (o migren a *Migrations* de EF Core).
- **Avisos internos (sin correo):** al registrar una visita se guarda un aviso en la tabla `Aviso`.
  El anfitrión abre el módulo **Avisos**, elige su nombre y ve quién lo espera; puede marcarlos como
  leídos (doble clic o botón). La pantalla y el menú se actualizan solos cada pocos segundos, y la
  recepcionista ve en la columna **Aviso** si ya fue leído. Si el aviso falla, la visita **sí se guarda**.
- **Bases creadas con la versión anterior:** al iniciar, el programa crea la tabla `Aviso` si falta y le
  genera su aviso a las visitas viejas. La tabla `LogNotificacion` queda sin uso (pueden borrarla).

## Posibles mejoras

- Usar `Migrations` en lugar de `EnsureCreated`.
- Leer la configuración desde un `appsettings.json`.
- Bloquear el registro si el visitante ya tiene una visita abierta.
