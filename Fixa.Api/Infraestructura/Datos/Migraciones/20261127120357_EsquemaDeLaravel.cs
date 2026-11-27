using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fixa.Api.Infraestructura.Datos.Migraciones
{
    /// <summary>
    /// Punto de partida de EF Core: registra el modelo de las tablas que crean las migraciones de
    /// Laravel, sin volver a crearlas. Las tablas propias de la API llegan en las migraciones siguientes.
    /// </summary>
    public partial class EsquemaDeLaravel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
