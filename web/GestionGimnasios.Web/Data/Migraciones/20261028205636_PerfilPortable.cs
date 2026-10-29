using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class PerfilPortable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Consentimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    Datos = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OtorgadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevocadoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Consentimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Consentimientos_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Consentimientos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConsultasPerfil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsentimientoId = table.Column<int>(type: "int", nullable: false),
                    ConsultadoPorId = table.Column<int>(type: "int", nullable: false),
                    ConsultadoEn = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultasPerfil", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsultasPerfil_Consentimientos_ConsentimientoId",
                        column: x => x.ConsentimientoId,
                        principalTable: "Consentimientos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultasPerfil_Usuarios_ConsultadoPorId",
                        column: x => x.ConsultadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_CadenaId",
                table: "Consentimientos",
                column: "CadenaId");

            migrationBuilder.CreateIndex(
                name: "IX_Consentimientos_UsuarioId_CadenaId_RevocadoEn",
                table: "Consentimientos",
                columns: new[] { "UsuarioId", "CadenaId", "RevocadoEn" });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultasPerfil_ConsentimientoId",
                table: "ConsultasPerfil",
                column: "ConsentimientoId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultasPerfil_ConsultadoPorId",
                table: "ConsultasPerfil",
                column: "ConsultadoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultasPerfil");

            migrationBuilder.DropTable(
                name: "Consentimientos");
        }
    }
}
