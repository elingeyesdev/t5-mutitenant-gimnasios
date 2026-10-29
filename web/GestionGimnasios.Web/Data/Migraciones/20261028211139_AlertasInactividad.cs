using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AlertasInactividad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alertas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Nivel = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DiasInactivo = table.Column<int>(type: "int", nullable: false),
                    UltimaActividad = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DetectadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CerradaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoCierre = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alertas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alertas_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Alertas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReglasInactividad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    DiasAviso = table.Column<int>(type: "int", nullable: false),
                    DiasRiesgo = table.Column<int>(type: "int", nullable: false),
                    SoloConPlanActivo = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReglasInactividad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intervenciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlertaId = table.Column<int>(type: "int", nullable: false),
                    RegistradaPorId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RegistradaEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intervenciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Intervenciones_Alertas_AlertaId",
                        column: x => x.AlertaId,
                        principalTable: "Alertas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Intervenciones_Usuarios_RegistradaPorId",
                        column: x => x.RegistradaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_CadenaId_Estado_Nivel",
                table: "Alertas",
                columns: new[] { "CadenaId", "Estado", "Nivel" });

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_CadenaId_UsuarioId",
                table: "Alertas",
                columns: new[] { "CadenaId", "UsuarioId" },
                unique: true,
                filter: "[Estado] <> 'CERRADA'");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_SucursalId",
                table: "Alertas",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_Alertas_UsuarioId",
                table: "Alertas",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Intervenciones_AlertaId",
                table: "Intervenciones",
                column: "AlertaId");

            migrationBuilder.CreateIndex(
                name: "IX_Intervenciones_RegistradaPorId",
                table: "Intervenciones",
                column: "RegistradaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReglasInactividad_CadenaId",
                table: "ReglasInactividad",
                column: "CadenaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Intervenciones");

            migrationBuilder.DropTable(
                name: "ReglasInactividad");

            migrationBuilder.DropTable(
                name: "Alertas");
        }
    }
}
