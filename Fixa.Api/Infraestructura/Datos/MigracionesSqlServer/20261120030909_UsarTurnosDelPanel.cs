using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixa.Api.Infraestructura.Datos.MigracionesSqlServer
{
    /// <inheritdoc />
    public partial class UsarTurnosDelPanel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "cuadrillas",
                keyColumn: "id",
                keyValue: 1L,
                column: "turno",
                value: "Mañana");

            migrationBuilder.UpdateData(
                table: "cuadrillas",
                keyColumn: "id",
                keyValue: 2L,
                column: "turno",
                value: "Tarde");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "cuadrillas",
                keyColumn: "id",
                keyValue: 1L,
                column: "turno",
                value: "manana");

            migrationBuilder.UpdateData(
                table: "cuadrillas",
                keyColumn: "id",
                keyValue: 2L,
                column: "turno",
                value: "tarde");
        }
    }
}
