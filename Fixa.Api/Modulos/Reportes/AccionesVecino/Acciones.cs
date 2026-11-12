using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Casos;
using Fixa.Api.Modulos.Reportes.Validacion;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes.AccionesVecino;

// Patrón Template Method — Clases concretas. Cada una solo escribe sus pasos propios; la
// verificación del código y la clave la hace siempre la clase base.

/// <summary>Consultar el estado del reporte (RF-07). Solo lee: no tiene reglas extra.</summary>
public sealed class ConsultarSeguimiento(FixaDbContext db) : AccionDelVecino<object>(db)
{
    protected override async Task<object> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        var reporte = await Db.Reportes.AsNoTracking()
            .Where(registro => registro.Id == reporteId)
            .Select(registro => new
            {
                registro.Codigo,
                registro.FechaRegistro,
                registro.EsApoyo,
                registro.Conformidad,
                registro.Caso.Estado,
                registro.Caso.FechaCierre,
                Categoria = registro.Caso.Categoria.Nombre,
                Unidad = registro.Caso.Unidad != null ? registro.Caso.Unidad.Nombre : null,
                UltimaActualizacion = registro.Caso.EventosHistorial.Max(evento => (DateTime?)evento.FechaHora),
                Revision = registro.Caso.Reportes.SelectMany(otro => otro.SolicitudesRevision)
                    .OrderByDescending(solicitud => solicitud.Id)
                    .Select(solicitud => new { solicitud.Estado, solicitud.Respuesta, solicitud.FechaResolucion })
                    .FirstOrDefault(),
            })
            .FirstAsync(cancelacion);

        var estado = ValoresBd.DesdeBd<EstadoCaso>(reporte.Estado);
        var revisionAbierta = estado == EstadoCaso.Cerrada ? reporte.FechaCierre?.AddDays(GestorCasos.DiasParaRevision) : null;
        return new
        {
            reporte.Codigo,
            estado = estado.Texto(),
            reporte.Categoria,
            reporte.Unidad,
            reporte.FechaRegistro,
            reporte.FechaCierre,
            reporte.UltimaActualizacion,
            reporte.EsApoyo,
            reporte.Conformidad,
            // Hasta cuándo puede confirmar la solución o pedir que se revise el cierre (15 días corridos).
            revisionDisponibleHasta = revisionAbierta,
            reporte.Revision,
        };
    }
}

/// <summary>Pedir que se revise un cierre con el que no está conforme (RF-11).</summary>
public sealed class SolicitarRevisionCierre(FixaDbContext db, GestorCasos gestor, string motivo) : AccionDelVecino<bool>(db)
{
    protected override async Task<bool> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        await gestor.SolicitarRevisionAsync(reporteId, motivo, cancelacion);
        return true;
    }
}

/// <summary>Confirmar si el problema quedó solucionado (RF-11, conformidad del vecino).</summary>
public sealed class RegistrarConformidad(FixaDbContext db, GestorCasos gestor, bool conforme) : AccionDelVecino<bool>(db)
{
    protected override async Task<bool> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        await gestor.RegistrarConformidadAsync(reporteId, conforme, cancelacion);
        return true;
    }
}

/// <summary>
/// Guardar o quitar el token del celular para recibir notificaciones push (RF-18). Si el vecino
/// reinstala la app, Firebase le da un token nuevo y la app lo actualiza aquí.
/// </summary>
public sealed class ActualizarDispositivo(FixaDbContext db, string? tokenDispositivo) : AccionDelVecino<bool>(db)
{
    private readonly string? tokenLimpio = string.IsNullOrWhiteSpace(tokenDispositivo) ? null : tokenDispositivo.Trim();

    protected override Task RevisarReglasAsync(long reporteId, CancellationToken cancelacion)
    {
        // Reutiliza el eslabón de la Chain of Responsibility que ya valida el token al reportar.
        new ValidarContacto().Exigir(new DatosAValidar(TokenDispositivo: tokenLimpio));
        return Task.CompletedTask;
    }

    protected override async Task<bool> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        // Se actualiza solo esa columna, sin cargar el reporte completo.
        await Db.Reportes
            .Where(registro => registro.Id == reporteId)
            .ExecuteUpdateAsync(cambio => cambio.SetProperty(registro => registro.TokenDispositivo, tokenLimpio), cancelacion);
        return true;
    }
}
