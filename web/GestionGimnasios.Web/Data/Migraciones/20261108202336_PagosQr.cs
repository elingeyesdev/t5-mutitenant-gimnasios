using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class PagosQr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrdenesPagoQr",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CadenaId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    SuscripcionId = table.Column<int>(type: "int", nullable: false),
                    SucursalId = table.Column<int>(type: "int", nullable: true),
                    Codigo = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Meses = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiraEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ComprobanteReferencia = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ComprobanteArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ComprobanteEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevisadaPorId = table.Column<int>(type: "int", nullable: true),
                    RevisadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PagoId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesPagoQr", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesPagoQr_Cadenas_CadenaId",
                        column: x => x.CadenaId,
                        principalTable: "Cadenas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesPagoQr_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesPagoQr_Sucursales_SucursalId",
                        column: x => x.SucursalId,
                        principalTable: "Sucursales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesPagoQr_Suscripciones_SuscripcionId",
                        column: x => x.SuscripcionId,
                        principalTable: "Suscripciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesPagoQr_Usuarios_RevisadaPorId",
                        column: x => x.RevisadaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrdenesPagoQr_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_CadenaId_Estado",
                table: "OrdenesPagoQr",
                columns: new[] { "CadenaId", "Estado" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_Codigo",
                table: "OrdenesPagoQr",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_PagoId",
                table: "OrdenesPagoQr",
                column: "PagoId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_RevisadaPorId",
                table: "OrdenesPagoQr",
                column: "RevisadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_SucursalId",
                table: "OrdenesPagoQr",
                column: "SucursalId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_SuscripcionId",
                table: "OrdenesPagoQr",
                column: "SuscripcionId",
                unique: true,
                filter: "[Estado] = 'PENDIENTE'");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesPagoQr_UsuarioId",
                table: "OrdenesPagoQr",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrdenesPagoQr");
        }
    }
}
