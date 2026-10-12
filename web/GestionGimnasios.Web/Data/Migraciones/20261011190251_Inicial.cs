using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cadenas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cadenas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ejercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrupoMuscular = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GruposSecundarios = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Equipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dificultad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    YoutubeVideoId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagenUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    TipoRegistro = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ejercicios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Metricas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    RegistradaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PesoKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    GrasaPct = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MasaMuscularKg = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CinturaCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PechoCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CaderaCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    BrazoCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MusloCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    PantorrillaCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metricas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    PlantillaId = table.Column<int>(type: "int", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EnviadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeidaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Parametros",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Clave = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parametros", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfilesEntrenamiento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    ObjetivoPrincipal = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NivelExperiencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TiposPreferidos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AreasPrioritarias = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiasDisponibles = table.Column<int>(type: "int", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilesEntrenamiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Recurso = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanesNutricionales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AsesorId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    KcalDiarias = table.Column<int>(type: "int", nullable: true),
                    ProteinaG = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true),
                    CarbohidratosG = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true),
                    GrasaG = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanComidas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesNutricionales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Plantillas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Tipo = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plantillas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreferenciasNotificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Push = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmacionesReserva = table.Column<bool>(type: "bit", nullable: false),
                    RecordatoriosClase = table.Column<bool>(type: "bit", nullable: false),
                    AlertasCancelacion = table.Column<bool>(type: "bit", nullable: false),
                    Promociones = table.Column<bool>(type: "bit", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreferenciasNotificacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Restricciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreasAfectadas = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MovimientosEvitar = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequiereAprobacion = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Restricciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nivel = table.Column<int>(type: "int", nullable: false),
                    EsDeSistema = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OtpCodigo = table.Column<string>(type: "nvarchar(6)", maxLength: 6, nullable: true),
                    OtpExpiraEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Planes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrecioMensual = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    MaxSucursales = table.Column<int>(type: "int", nullable: true),
                    Beneficios = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Planes_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CapacidadMaxima = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    Abierta = table.Column<bool>(type: "bit", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sucursales_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolPermisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    PermisoId = table.Column<int>(type: "int", nullable: false),
                    OtorgadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OtorgadoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolPermisos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asesorias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AsesorId = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetaSesionesSemana = table.Column<int>(type: "int", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asesorias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asesorias_Usuarios_AsesorId",
                        column: x => x.AsesorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Asesorias_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Citas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NutricionistaId = table.Column<int>(type: "int", nullable: false),
                    PacienteId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Hora = table.Column<TimeOnly>(type: "time", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Citas_Usuarios_NutricionistaId",
                        column: x => x.NutricionistaId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Citas_Usuarios_PacienteId",
                        column: x => x.PacienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContactosEmergencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Relacion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContactosEmergencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContactosEmergencia_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Conversaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AsesorId = table.Column<int>(type: "int", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Conversaciones_Usuarios_AsesorId",
                        column: x => x.AsesorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Conversaciones_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Perfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: true),
                    Genero = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ci = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AlturaCm = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    NivelExperiencia = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CondicionesMedicas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeportesFavoritos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NombreUsuario = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Perfiles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accesos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    Ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Salida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Metodo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accesos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accesos_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Accesos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DuracionMin = table.Column<int>(type: "int", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    AccesoLibre = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actividades_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorariosPersonal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<int>(type: "int", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosPersonal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosPersonal_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HorariosPersonal_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HorariosSucursal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    DiaSemana = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Abre = table.Column<TimeOnly>(type: "time", nullable: false),
                    Cierra = table.Column<TimeOnly>(type: "time", nullable: false),
                    EsFeriado = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosSucursal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosSucursal_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Maquinas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ImagenUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Maquinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Maquinas_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Membresias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Membresias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Membresias_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Membresias_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Membresias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Rutinas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: true),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EntrenadorId = table.Column<int>(type: "int", nullable: false),
                    AsignadaAId = table.Column<int>(type: "int", nullable: true),
                    Dificultad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DuracionSemanas = table.Column<int>(type: "int", nullable: true),
                    EsPlantilla = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rutinas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rutinas_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rutinas_Usuarios_AsignadaAId",
                        column: x => x.AsignadaAId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rutinas_Usuarios_EntrenadorId",
                        column: x => x.EntrenadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Suscripciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    CadenaId = table.Column<int>(type: "int", nullable: true),
                    SucursalPrincipalId = table.Column<int>(type: "int", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Inicio = table.Column<DateOnly>(type: "date", nullable: false),
                    Fin = table.Column<DateOnly>(type: "date", nullable: false),
                    RenovacionAutomatica = table.Column<bool>(type: "bit", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suscripciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Suscripciones_Planes_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Planes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Suscripciones_Sucursales_SucursalPrincipalId",
                        column: x => x.SucursalPrincipalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Suscripciones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ubicaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ciudad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitud = table.Column<decimal>(type: "decimal(10,8)", precision: 10, scale: 8, nullable: false),
                    Longitud = table.Column<decimal>(type: "decimal(11,8)", precision: 11, scale: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ubicaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ubicaciones_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuarioRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RolId = table.Column<int>(type: "int", nullable: false),
                    CadenaId = table.Column<int>(type: "int", nullable: true),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    AsignadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AsignadoPorId = table.Column<int>(type: "int", nullable: true),
                    ExpiraEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuarioRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Roles_RolId",
                        column: x => x.RolId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsuarioRoles_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Visitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: false),
                    Entrada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Salida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuracionMin = table.Column<int>(type: "int", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visitas_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Mensajes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversacionId = table.Column<int>(type: "int", nullable: false),
                    RemitenteId = table.Column<int>(type: "int", nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BorradoParaRemitente = table.Column<bool>(type: "bit", nullable: false),
                    BorradoParaReceptor = table.Column<bool>(type: "bit", nullable: false),
                    BorradoParaTodos = table.Column<bool>(type: "bit", nullable: false),
                    LeidoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mensajes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Mensajes_Conversaciones_ConversacionId",
                        column: x => x.ConversacionId,
                        principalTable: "Conversaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HorariosActividad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    ActividadId = table.Column<int>(type: "int", nullable: false),
                    InstructorId = table.Column<int>(type: "int", nullable: true),
                    DiaSemana = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: false),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: false),
                    CupoMaximo = table.Column<int>(type: "int", nullable: false),
                    Recurrente = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HorariosActividad", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HorariosActividad_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HorariosActividad_Usuarios_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RutinaEjercicios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RutinaId = table.Column<int>(type: "int", nullable: false),
                    EjercicioId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false),
                    Series = table.Column<int>(type: "int", nullable: true),
                    Repeticiones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PesoKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DiaSemana = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DescansoSeg = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Parametros = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RutinaEjercicios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RutinaEjercicios_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sesiones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    RutinaId = table.Column<int>(type: "int", nullable: true),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    TipoDeporte = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Inicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fin = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DuracionSeg = table.Column<int>(type: "int", nullable: true),
                    Calorias = table.Column<int>(type: "int", nullable: true),
                    Notas = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sesiones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sesiones_Rutinas_RutinaId",
                        column: x => x.RutinaId,
                        principalTable: "Rutinas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sesiones_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SuscripcionId = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metodo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pagos_Suscripciones_SuscripcionId",
                        column: x => x.SuscripcionId,
                        principalTable: "Suscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HorarioActividadId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Ingreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Salida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Asistencias_HorariosActividad_HorarioActividadId",
                        column: x => x.HorarioActividadId,
                        principalTable: "HorariosActividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencias_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ListaEspera",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    ReservaId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    HorarioActividadId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: true),
                    Posicion = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    NotificadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AsignadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListaEspera", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListaEspera_HorariosActividad_HorarioActividadId",
                        column: x => x.HorarioActividadId,
                        principalTable: "HorariosActividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ListaEspera_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reservas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    HorarioActividadId = table.Column<int>(type: "int", nullable: true),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    ActividadId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Inicio = table.Column<TimeOnly>(type: "time", nullable: true),
                    Fin = table.Column<TimeOnly>(type: "time", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CanceladaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RecordatorioEnviado = table.Column<bool>(type: "bit", nullable: false),
                    CreadaPorId = table.Column<int>(type: "int", nullable: false),
                    CodigoQr = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reservas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reservas_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_HorariosActividad_HorarioActividadId",
                        column: x => x.HorarioActividadId,
                        principalTable: "HorariosActividad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reservas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SesionId = table.Column<int>(type: "int", nullable: false),
                    RutinaEjercicioId = table.Column<int>(type: "int", nullable: true),
                    EjercicioId = table.Column<int>(type: "int", nullable: true),
                    NumeroSerie = table.Column<int>(type: "int", nullable: false),
                    Repeticiones = table.Column<int>(type: "int", nullable: true),
                    PesoKg = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    DuracionSeg = table.Column<int>(type: "int", nullable: true),
                    DistanciaM = table.Column<int>(type: "int", nullable: true),
                    DescansoSeg = table.Column<int>(type: "int", nullable: true),
                    EsfuerzoPercibido = table.Column<int>(type: "int", nullable: true),
                    Metadatos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletadaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Series_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Series_Sesiones_SesionId",
                        column: x => x.SesionId,
                        principalTable: "Sesiones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accesos_SucursalId_Ingreso",
                table: "Accesos",
                columns: new[] { "SucursalId", "Ingreso" });

            migrationBuilder.CreateIndex(
                name: "IX_Accesos_UsuarioId_Estado",
                table: "Accesos",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_SucursalId",
                table: "Actividades",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Asesorias_AsesorId",
                table: "Asesorias",
                column: "AsesorId");

            migrationBuilder.CreateIndex(
                name: "IX_Asesorias_ClienteId_AsesorId",
                table: "Asesorias",
                columns: new[] { "ClienteId", "AsesorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_HorarioActividadId",
                table: "Asistencias",
                column: "HorarioActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_UsuarioId",
                table: "Asistencias",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cadenas_Nombre",
                table: "Cadenas",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citas_NutricionistaId",
                table: "Citas",
                column: "NutricionistaId");

            migrationBuilder.CreateIndex(
                name: "IX_Citas_PacienteId",
                table: "Citas",
                column: "PacienteId");

            migrationBuilder.CreateIndex(
                name: "IX_ContactosEmergencia_UsuarioId",
                table: "ContactosEmergencia",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversaciones_AsesorId",
                table: "Conversaciones",
                column: "AsesorId");

            migrationBuilder.CreateIndex(
                name: "IX_Conversaciones_ClienteId_AsesorId",
                table: "Conversaciones",
                columns: new[] { "ClienteId", "AsesorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ejercicios_GrupoMuscular",
                table: "Ejercicios",
                column: "GrupoMuscular");

            migrationBuilder.CreateIndex(
                name: "IX_Ejercicios_Nombre",
                table: "Ejercicios",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosActividad_ActividadId",
                table: "HorariosActividad",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosActividad_InstructorId",
                table: "HorariosActividad",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosPersonal_SucursalId",
                table: "HorariosPersonal",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosPersonal_UsuarioId_SucursalId_DiaSemana",
                table: "HorariosPersonal",
                columns: new[] { "UsuarioId", "SucursalId", "DiaSemana" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HorariosSucursal_SucursalId",
                table: "HorariosSucursal",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_HorarioActividadId_Estado_Posicion",
                table: "ListaEspera",
                columns: new[] { "HorarioActividadId", "Estado", "Posicion" });

            migrationBuilder.CreateIndex(
                name: "IX_ListaEspera_UsuarioId",
                table: "ListaEspera",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Maquinas_SucursalId_Estado",
                table: "Maquinas",
                columns: new[] { "SucursalId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_CadenaId",
                table: "Membresias",
                column: "CadenaId");

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_SucursalId",
                table: "Membresias",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Membresias_UsuarioId",
                table: "Membresias",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensajes_ConversacionId",
                table: "Mensajes",
                column: "ConversacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Metricas_UsuarioId_RegistradaEn",
                table: "Metricas",
                columns: new[] { "UsuarioId", "RegistradaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioId_EnviadaEn",
                table: "Notificaciones",
                columns: new[] { "UsuarioId", "EnviadaEn" });

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_SuscripcionId",
                table: "Pagos",
                column: "SuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_Parametros_Clave",
                table: "Parametros",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_Ci",
                table: "Perfiles",
                column: "Ci",
                unique: true,
                filter: "[Ci] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_NombreUsuario",
                table: "Perfiles",
                column: "NombreUsuario",
                unique: true,
                filter: "[NombreUsuario] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Perfiles_UsuarioId",
                table: "Perfiles",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PerfilesEntrenamiento_UsuarioId",
                table: "PerfilesEntrenamiento",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Codigo",
                table: "Permisos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planes_CadenaId_Nombre",
                table: "Planes",
                columns: new[] { "CadenaId", "Nombre" },
                unique: true,
                filter: "[CadenaId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanesNutricionales_AsesorId_ClienteId",
                table: "PlanesNutricionales",
                columns: new[] { "AsesorId", "ClienteId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Plantillas_Tipo",
                table: "Plantillas",
                column: "Tipo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PreferenciasNotificacion_UsuarioId",
                table: "PreferenciasNotificacion",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_ActividadId",
                table: "Reservas",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_CodigoQr",
                table: "Reservas",
                column: "CodigoQr",
                unique: true,
                filter: "[CodigoQr] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_HorarioActividadId_Fecha",
                table: "Reservas",
                columns: new[] { "HorarioActividadId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_SucursalId",
                table: "Reservas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId_Estado",
                table: "Reservas",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Restricciones_UsuarioId",
                table: "Restricciones",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_PermisoId",
                table: "RolPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_RolPermisos_RolId",
                table: "RolPermisos",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_EjercicioId",
                table: "RutinaEjercicios",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_RutinaEjercicios_RutinaId",
                table: "RutinaEjercicios",
                column: "RutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_AsignadaAId",
                table: "Rutinas",
                column: "AsignadaAId");

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_EntrenadorId",
                table: "Rutinas",
                column: "EntrenadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Rutinas_SucursalId",
                table: "Rutinas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_EjercicioId",
                table: "Series",
                column: "EjercicioId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_SesionId",
                table: "Series",
                column: "SesionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_RutinaId",
                table: "Sesiones",
                column: "RutinaId");

            migrationBuilder.CreateIndex(
                name: "IX_Sesiones_UsuarioId_Inicio",
                table: "Sesiones",
                columns: new[] { "UsuarioId", "Inicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_CadenaId_Nombre",
                table: "Sucursales",
                columns: new[] { "CadenaId", "Nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suscripciones_PlanId",
                table: "Suscripciones",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripciones_SucursalPrincipalId",
                table: "Suscripciones",
                column: "SucursalPrincipalId");

            migrationBuilder.CreateIndex(
                name: "IX_Suscripciones_UsuarioId_Estado",
                table: "Suscripciones",
                columns: new[] { "UsuarioId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_Ubicaciones_SucursalId",
                table: "Ubicaciones",
                column: "SucursalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_CadenaId_SucursalId",
                table: "UsuarioRoles",
                columns: new[] { "CadenaId", "SucursalId" });

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_RolId",
                table: "UsuarioRoles",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_SucursalId",
                table: "UsuarioRoles",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioRoles_UsuarioId",
                table: "UsuarioRoles",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Email",
                table: "Usuarios",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_SucursalId",
                table: "Visitas",
                column: "SucursalId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accesos");

            migrationBuilder.DropTable(
                name: "Asesorias");

            migrationBuilder.DropTable(
                name: "Asistencias");

            migrationBuilder.DropTable(
                name: "Citas");

            migrationBuilder.DropTable(
                name: "ContactosEmergencia");

            migrationBuilder.DropTable(
                name: "HorariosPersonal");

            migrationBuilder.DropTable(
                name: "HorariosSucursal");

            migrationBuilder.DropTable(
                name: "ListaEspera");

            migrationBuilder.DropTable(
                name: "Maquinas");

            migrationBuilder.DropTable(
                name: "Membresias");

            migrationBuilder.DropTable(
                name: "Mensajes");

            migrationBuilder.DropTable(
                name: "Metricas");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "Pagos");

            migrationBuilder.DropTable(
                name: "Parametros");

            migrationBuilder.DropTable(
                name: "Perfiles");

            migrationBuilder.DropTable(
                name: "PerfilesEntrenamiento");

            migrationBuilder.DropTable(
                name: "PlanesNutricionales");

            migrationBuilder.DropTable(
                name: "Plantillas");

            migrationBuilder.DropTable(
                name: "PreferenciasNotificacion");

            migrationBuilder.DropTable(
                name: "Reservas");

            migrationBuilder.DropTable(
                name: "Restricciones");

            migrationBuilder.DropTable(
                name: "RolPermisos");

            migrationBuilder.DropTable(
                name: "RutinaEjercicios");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "Ubicaciones");

            migrationBuilder.DropTable(
                name: "UsuarioRoles");

            migrationBuilder.DropTable(
                name: "Visitas");

            migrationBuilder.DropTable(
                name: "Conversaciones");

            migrationBuilder.DropTable(
                name: "Suscripciones");

            migrationBuilder.DropTable(
                name: "HorariosActividad");

            migrationBuilder.DropTable(
                name: "Permisos");

            migrationBuilder.DropTable(
                name: "Ejercicios");

            migrationBuilder.DropTable(
                name: "Sesiones");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Planes");

            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "Rutinas");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Cadenas");
        }
    }
}
