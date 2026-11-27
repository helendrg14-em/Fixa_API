using Fixa.Api.Dominio;
using Fixa.Api.Dtos;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Casos;
using Fixa.Api.Modulos.Reportes.Validacion;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes.AccionesVecino;

public sealed class ConsultarSeguimiento(FixaDbContext db) : AccionDelVecino<SeguimientoSalida>(db)
{
    protected override async Task<SeguimientoSalida> RealizarAsync(long reporteId, CancellationToken cancelacion)
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
                    .Select(solicitud => new RevisionSalida(solicitud.Estado, solicitud.Respuesta, solicitud.FechaResolucion))
                    .FirstOrDefault(),
            })
            .FirstAsync(cancelacion);

        var estado = ValoresBd.DesdeBd<EstadoCaso>(reporte.Estado);
        var revisionAbierta = estado == EstadoCaso.Cerrada ? reporte.FechaCierre?.AddDays(GestorCasos.DiasParaRevision) : null;
        return new SeguimientoSalida(
            reporte.Codigo,
            estado.Texto(),
            reporte.Categoria,
            reporte.Unidad,
            reporte.FechaRegistro,
            reporte.FechaCierre,
            reporte.UltimaActualizacion,
            reporte.EsApoyo,
            reporte.Conformidad,
            revisionAbierta,
            reporte.Revision);
    }
}

public sealed class SolicitarRevisionCierre(FixaDbContext db, GestorCasos gestor, string motivo) : AccionDelVecino<bool>(db)
{
    protected override async Task<bool> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        await gestor.SolicitarRevisionAsync(reporteId, motivo, cancelacion);
        return true;
    }
}

public sealed class RegistrarConformidad(FixaDbContext db, GestorCasos gestor, bool conforme) : AccionDelVecino<bool>(db)
{
    protected override async Task<bool> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        await gestor.RegistrarConformidadAsync(reporteId, conforme, cancelacion);
        return true;
    }
}

public sealed class ActualizarDispositivo(FixaDbContext db, string? tokenDispositivo) : AccionDelVecino<bool>(db)
{
    private readonly string? tokenLimpio = string.IsNullOrWhiteSpace(tokenDispositivo) ? null : tokenDispositivo.Trim();

    protected override Task RevisarReglasAsync(long reporteId, CancellationToken cancelacion)
    {
        new ValidarContacto().Exigir(new DatosAValidar(TokenDispositivo: tokenLimpio));
        return Task.CompletedTask;
    }

    protected override async Task<bool> RealizarAsync(long reporteId, CancellationToken cancelacion)
    {
        await Db.Reportes
            .Where(registro => registro.Id == reporteId)
            .ExecuteUpdateAsync(cambio => cambio.SetProperty(registro => registro.TokenDispositivo, tokenLimpio), cancelacion);
        return true;
    }
}
