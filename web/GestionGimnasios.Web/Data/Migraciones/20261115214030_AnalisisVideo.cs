using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class AnalisisVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AnalisisPreliminar",
                table: "VideosTecnica",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Alto",
                table: "VideosTecnica",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AnalisisConfirmado",
                table: "VideosTecnica",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Ancho",
                table: "VideosTecnica",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorreccionAnalisis",
                table: "VideosTecnica",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DuracionSeg",
                table: "VideosTecnica",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultadoAnalisis",
                table: "VideosTecnica",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alto",
                table: "VideosTecnica");

            migrationBuilder.DropColumn(
                name: "AnalisisConfirmado",
                table: "VideosTecnica");

            migrationBuilder.DropColumn(
                name: "Ancho",
                table: "VideosTecnica");

            migrationBuilder.DropColumn(
                name: "CorreccionAnalisis",
                table: "VideosTecnica");

            migrationBuilder.DropColumn(
                name: "DuracionSeg",
                table: "VideosTecnica");

            migrationBuilder.DropColumn(
                name: "ResultadoAnalisis",
                table: "VideosTecnica");

            migrationBuilder.AlterColumn<string>(
                name: "AnalisisPreliminar",
                table: "VideosTecnica",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);
        }
    }
}
