using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class VideosTecnica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VideosTecnica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    AsesorId = table.Column<int>(type: "int", nullable: false),
                    EjercicioId = table.Column<int>(type: "int", nullable: false),
                    Comentario = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Archivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    EnviadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AnalisisPreliminar = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Valoracion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RevisadoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VideosTecnica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VideosTecnica_Ejercicios_EjercicioId",
                        column: x => x.EjercicioId,
                        principalTable: "Ejercicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideosTecnica_Usuarios_AsesorId",
                        column: x => x.AsesorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VideosTecnica_Usuarios_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VideosTecnica_AsesorId_Estado",
                table: "VideosTecnica",
                columns: new[] { "AsesorId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_VideosTecnica_ClienteId_Estado",
                table: "VideosTecnica",
                columns: new[] { "ClienteId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_VideosTecnica_EjercicioId",
                table: "VideosTecnica",
                column: "EjercicioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VideosTecnica");
        }
    }
}
