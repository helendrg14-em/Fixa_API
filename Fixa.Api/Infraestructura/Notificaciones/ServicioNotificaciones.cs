using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Infraestructura.Notificaciones;

/// <summary>
/// Avisa a todos los vecinos que reportaron o apoyaron un caso cuando cambia su estado.
/// Recorre los canales disponibles (adaptadores) sin saber cuál es cuál: cada reporte recibe
/// push si dejó su token y correo si dejó su correo. Cada envío queda registrado.
/// El texto sale de la plantilla del estado; si el administrador la desactivó, no se avisa.
/// </summary>
public sealed class ServicioNotificaciones(FixaDbContext db, IEnumerable<ICanalNotificacion> canales)
{
    private const string TituloPorDefecto = "FIXA: tu reporte cambió de estado";

    public async Task NotificarAsync(EventoHistorial evento, EstadoCaso estado, CancellationToken cancelacion)
    {
        var plantilla = await PlantillaAsync(estado, cancelacion);
        if (plantilla is { Activa: false })
            return;

        var reportes = await db.Reportes.AsNoTracking()
            .Where(r => r.CasoId == evento.CasoId)
            .Select(r => new { r.Id, r.Codigo, r.TokenDispositivo, r.CorreoContacto })
            .ToListAsync(cancelacion);

        foreach (var reporte in reportes)
        {
            var aviso = Redactar(plantilla, estado, reporte.Codigo);
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

    /// <summary>
    /// Vuelve a enviar un aviso que no se entregó, con el contacto actual del vecino (si cambió su
    /// correo o su celular, se usa el nuevo) y el texto actual de la plantilla.
    /// </summary>
    public async Task ReintentarAsync(Notificacion notificacion, CancellationToken cancelacion)
    {
        if (notificacion.EstadoEntrega != EstadoEntrega.NoEntregada.ABd())
            throw new ReglaNegocioException("Solo se pueden reintentar los avisos que no se entregaron.");

        var tipoCanal = ValoresBd.DesdeBd<CanalNotificacion>(notificacion.Canal);
        var canal = canales.FirstOrDefault(c => c.Canal == tipoCanal)
            ?? throw new ReglaNegocioException("El canal de este aviso no está disponible.");
        var destino = tipoCanal == CanalNotificacion.Push ? notificacion.Reporte.TokenDispositivo : notificacion.Reporte.CorreoContacto;
        if (string.IsNullOrWhiteSpace(destino))
            throw new ReglaNegocioException("El vecino ya no tiene un contacto registrado para este canal.");

        var estado = ValoresBd.DesdeBd<EstadoCaso>(notificacion.Evento.EstadoNuevo ?? notificacion.Evento.Caso.Estado);
        var aviso = Redactar(await PlantillaAsync(estado, cancelacion), estado, notificacion.Reporte.Codigo);

        var entregada = await canal.EnviarAsync(destino, aviso, cancelacion);
        notificacion.Destino = destino;
        notificacion.FechaEnvio = entregada ? HoraBolivia.Ahora() : null;
        notificacion.EstadoEntrega = (entregada ? EstadoEntrega.Enviada : EstadoEntrega.NoEntregada).ABd();
        await db.SaveChangesAsync(cancelacion);
    }

    private Task<PlantillaNotificacion?> PlantillaAsync(EstadoCaso estado, CancellationToken cancelacion)
    {
        var estadoBd = estado.ABd();
        return db.PlantillasNotificacion.AsNoTracking().FirstOrDefaultAsync(p => p.Estado == estadoBd, cancelacion);
    }

    private static Aviso Redactar(PlantillaNotificacion? plantilla, EstadoCaso estado, string codigo) => plantilla is null
        ? new Aviso(TituloPorDefecto, $"El reporte {codigo} ahora está: {estado.Texto()}.")
        : new Aviso(plantilla.Titulo, plantilla.Cuerpo.Replace("{codigo}", codigo).Replace("{estado}", estado.Texto()));
}
