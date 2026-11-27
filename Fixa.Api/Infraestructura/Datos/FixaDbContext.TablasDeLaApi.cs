using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Infraestructura.Datos;

/// <summary>
/// Tablas que crea la API con sus propias migraciones de EF Core (Datos/Migraciones). Las demás
/// tablas las crean las migraciones de Laravel.
/// </summary>
public partial class FixaDbContext
{
    public virtual DbSet<PlantillaNotificacion> PlantillasNotificacion { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlantillaNotificacion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("plantillas_notificacion_pkey");

            entity.ToTable("plantillas_notificacion");

            entity.HasIndex(e => e.Estado, "plantillas_notificacion_estado_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(25)
                .HasColumnName("estado");
            entity.Property(e => e.Titulo)
                .HasMaxLength(80)
                .HasColumnName("titulo");
            entity.Property(e => e.Cuerpo)
                .HasMaxLength(500)
                .HasColumnName("cuerpo");
            entity.Property(e => e.Activa).HasColumnName("activa");
            entity.Property(e => e.FechaActualizacion)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_actualizacion");

            entity.HasData(PlantillasIniciales());
        });
    }

    private static IEnumerable<PlantillaNotificacion> PlantillasIniciales()
    {
        var fecha = new DateTime(2026, 11, 27, 8, 0, 0);
        var cuerpos = new Dictionary<EstadoCaso, string>
        {
            [EstadoCaso.Recibida] = "Recibimos tu reporte {codigo}. Te avisaremos cuando avance.",
            [EstadoCaso.EnRevision] = "Tu reporte {codigo} está en revisión por la unidad responsable.",
            [EstadoCaso.Asignada] = "Tu reporte {codigo} fue asignado a la unidad que lo atenderá.",
            [EstadoCaso.Programada] = "Tu reporte {codigo} ya tiene una cuadrilla y una fecha de atención.",
            [EstadoCaso.EnAtencion] = "Una cuadrilla está atendiendo tu reporte {codigo}.",
            [EstadoCaso.PendienteValidacion] = "El trabajo de tu reporte {codigo} se está verificando.",
            [EstadoCaso.Cerrada] = "Tu reporte {codigo} fue atendido. Confirma en la app si quedaste conforme.",
            [EstadoCaso.Rechazada] = "Tu reporte {codigo} no pudo ser atendido. Revisa el motivo en la app.",
            [EstadoCaso.Duplicada] = "Tu reporte {codigo} se unió a otro caso del mismo problema.",
            [EstadoCaso.Derivada] = "Tu reporte {codigo} fue derivado a la entidad que corresponde.",
        };

        return cuerpos.Select((par, indice) => new PlantillaNotificacion
        {
            Id = indice + 1,
            Estado = par.Key.ABd(),
            Titulo = "FIXA: tu reporte cambió de estado",
            Cuerpo = par.Value,
            Activa = true,
            FechaActualizacion = fecha,
        });
    }
}
