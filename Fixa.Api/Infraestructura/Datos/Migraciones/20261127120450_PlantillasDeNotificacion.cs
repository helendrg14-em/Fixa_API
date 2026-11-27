using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fixa.Api.Infraestructura.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class PlantillasDeNotificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plantillas_notificacion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    estado = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    titulo = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    cuerpo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    fecha_actualizacion = table.Column<DateTime>(type: "timestamp(0) without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("plantillas_notificacion_pkey", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "plantillas_notificacion",
                columns: new[] { "id", "activa", "cuerpo", "estado", "fecha_actualizacion", "titulo" },
                values: new object[,]
                {
                    { 1L, true, "Recibimos tu reporte {codigo}. Te avisaremos cuando avance.", "recibida", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 2L, true, "Tu reporte {codigo} está en revisión por la unidad responsable.", "en_revision", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 3L, true, "Tu reporte {codigo} fue asignado a la unidad que lo atenderá.", "asignada", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 4L, true, "Tu reporte {codigo} ya tiene una cuadrilla y una fecha de atención.", "programada", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 5L, true, "Una cuadrilla está atendiendo tu reporte {codigo}.", "en_atencion", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 6L, true, "El trabajo de tu reporte {codigo} se está verificando.", "pendiente_validacion", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 7L, true, "Tu reporte {codigo} fue atendido. Confirma en la app si quedaste conforme.", "cerrada", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 8L, true, "Tu reporte {codigo} no pudo ser atendido. Revisa el motivo en la app.", "rechazada", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 9L, true, "Tu reporte {codigo} se unió a otro caso del mismo problema.", "duplicada", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" },
                    { 10L, true, "Tu reporte {codigo} fue derivado a la entidad que corresponde.", "derivada", new DateTime(2026, 11, 27, 8, 0, 0, 0, DateTimeKind.Unspecified), "FIXA: tu reporte cambió de estado" }
                });

            migrationBuilder.CreateIndex(
                name: "plantillas_notificacion_estado_unique",
                table: "plantillas_notificacion",
                column: "estado",
                unique: true);

            migrationBuilder.Sql("SELECT setval(pg_get_serial_sequence('plantillas_notificacion', 'id'), (SELECT MAX(id) FROM plantillas_notificacion));");
            migrationBuilder.Sql("ALTER TABLE plantillas_notificacion ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plantillas_notificacion");
        }
    }
}
