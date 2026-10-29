using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class Beneficios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Beneficios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Condiciones = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VigenteDesde = table.Column<DateOnly>(type: "date", nullable: false),
                    VigenteHasta = table.Column<DateOnly>(type: "date", nullable: false),
                    CuposTotales = table.Column<int>(type: "int", nullable: true),
                    LimitePorSocio = table.Column<int>(type: "int", nullable: false),
                    AsistenciasMinimas = table.Column<int>(type: "int", nullable: false),
                    RequierePlanActivo = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beneficios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Beneficios_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Canjes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    BeneficioId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    CanjeadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EntregadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EntregadoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Canjes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Canjes_Beneficios_BeneficioId",
                        column: x => x.BeneficioId,
                        principalTable: "Beneficios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Canjes_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Beneficios_CadenaId_Activo_VigenteHasta",
                table: "Beneficios",
                columns: new[] { "CadenaId", "Activo", "VigenteHasta" });

            migrationBuilder.CreateIndex(
                name: "IX_Canjes_BeneficioId_UsuarioId",
                table: "Canjes",
                columns: new[] { "BeneficioId", "UsuarioId" });

            migrationBuilder.CreateIndex(
                name: "IX_Canjes_Codigo",
                table: "Canjes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Canjes_UsuarioId",
                table: "Canjes",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Canjes");

            migrationBuilder.DropTable(
                name: "Beneficios");
        }
    }
}
