using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionGimnasios.Web.Data.Migraciones
{
    /// <inheritdoc />
    public partial class PautasNutricionales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hidratacion",
                table: "PlanesNutricionales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PautaAntesEntrenar",
                table: "PlanesNutricionales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PautaDespuesEntrenar",
                table: "PlanesNutricionales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hidratacion",
                table: "PlanesNutricionales");

            migrationBuilder.DropColumn(
                name: "PautaAntesEntrenar",
                table: "PlanesNutricionales");

            migrationBuilder.DropColumn(
                name: "PautaDespuesEntrenar",
                table: "PlanesNutricionales");
        }
    }
}
