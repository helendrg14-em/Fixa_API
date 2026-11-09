using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Infraestructura.Notificaciones;

/// <summary>
/// Avisa a todos los vecinos que reportaron o apoyaron un caso cuando cambia su estado.
/// Recorre los canales disponibles (adaptadores) sin saber cuál es cuál: cada reporte recibe
/// push si dejó su token y correo si dejó su correo. Cada envío queda registrado.
/// </summary>
public sealed class ServicioNotificaciones(FixaDbContext db, IEnumerable<ICanalNotificacion> canales)
{
    public async Task NotificarAsync(EventoHistorial evento, EstadoCaso estado, CancellationToken cancelacion)
    {
        var reportes = await db.Reportes.AsNoTracking()
            .Where(r => r.CasoId == evento.CasoId)
            .Select(r => new { r.Id, r.Codigo, r.TokenDispositivo, r.CorreoContacto })
            .ToListAsync(cancelacion);

        foreach (var reporte in reportes)
        {
            var aviso = new Aviso("FIXA: tu reporte cambió de estado", $"El reporte {reporte.Codigo} ahora está: {estado.Texto()}.");
            foreach (var canal in canales)
            {
                var destino = canal.Canal == CanalNotificacion.Push ? reporte.TokenDispositivo : reporte.CorreoContacto;
                if (string.IsNullOrWhiteSpace(destino))
                    continue;

                var entregada = await canal.EnviarAsync(destino, aviso, cancelacion);
                db.Notificaciones.Add(new Notificacion
                {
                    EventoId = evento.Id,
                    ReporteId = reporte.Id,
                    Canal = canal.Canal.ABd(),
                    Destino = destino,
                    FechaEnvio = entregada ? HoraBolivia.Ahora() : null,
                    EstadoEntrega = (entregada ? EstadoEntrega.Enviada : EstadoEntrega.NoEntregada).ABd(),
                });
            }
        }
        await db.SaveChangesAsync(cancelacion);
    }
}
