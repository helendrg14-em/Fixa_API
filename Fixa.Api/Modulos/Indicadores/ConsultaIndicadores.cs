using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Dtos;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Indicadores;

/// <summary>
/// Consultas de solo lectura para indicadores y mapa (por categoría, distrito y periodo). Los
/// conteos se hacen en PostgreSQL, no en memoria, para que sigan rápidos con muchos casos.
/// </summary>
public sealed class ConsultaIndicadores(FixaDbContext db)
{
    private const int DiasPorDefecto = 30, DiasMaximos = 366, PuntosMaximos = 5000;

    // El mapa público no muestra lo que no es un problema real (RN-07: datos mínimos).
    private static readonly string[] ocultosAlPublico = [EstadoCaso.Rechazada.ABd(), EstadoCaso.Duplicada.ABd()];

    /// <summary>Resumen del periodo. unidadId null = todas las unidades (administrador).</summary>
    public async Task<IndicadoresSalida> ResumenAsync(FiltroIndicadores filtro, long? unidadId, CancellationToken cancelacion)
    {
        var (casos, desde, hasta) = Filtrar(filtro, unidadId);
        var cerrada = EstadoCaso.Cerrada.ABd();

        var totales = await casos.GroupBy(_ => 1).Select(g => new
        {
            total = g.Count(),
            cerrados = g.Count(c => c.Estado == cerrada),
            cerradosDentroDeMeta = g.Count(c => c.Estado == cerrada && c.FechaObjetivo != null && c.FechaCierre <= c.FechaObjetivo),
            cerradosConMeta = g.Count(c => c.Estado == cerrada && c.FechaObjetivo != null),
            vencidosAbiertos = g.Count(c => c.Vencido && !EstadoCasoReglas.FinalesBd.Contains(c.Estado)),
            promedioHorasCierre = g.Where(c => c.Estado == cerrada && c.FechaCierre != null)
                .Average(c => (double?)(c.FechaCierre!.Value - c.FechaCreacion).TotalHours),
        }).FirstOrDefaultAsync(cancelacion);

        var porEstado = await casos.GroupBy(c => c.Estado)
            .Select(g => new { g.Key, Cantidad = g.Count() }).OrderByDescending(x => x.Cantidad)
            .Select(x => new ConteoPorEstado(x.Key, x.Cantidad)).ToListAsync(cancelacion);
        var porCategoria = await casos.GroupBy(c => c.Categoria.Nombre)
            .Select(g => new { g.Key, Cantidad = g.Count() }).OrderByDescending(x => x.Cantidad)
            .Select(x => new ConteoPorCategoria(x.Key, x.Cantidad)).ToListAsync(cancelacion);
        var porDistrito = await casos.SelectMany(c => c.Reportes.Where(r => !r.EsApoyo)).GroupBy(r => r.Distrito.Nombre)
            .Select(g => new { g.Key, Cantidad = g.Count() }).OrderByDescending(x => x.Cantidad)
            .Select(x => new ConteoPorDistrito(x.Key, x.Cantidad)).ToListAsync(cancelacion);

        return new IndicadoresSalida(
            new PeriodoSalida(desde, hasta),
            totales?.total,
            totales?.cerrados,
            totales?.vencidosAbiertos,
            totales?.cerradosDentroDeMeta,
            // Solo cuentan los cerrados que tenían meta; sin datos se devuelve null, no 0 %.
            totales is { cerradosConMeta: > 0 }
                ? Math.Round(100.0 * totales.cerradosDentroDeMeta / totales.cerradosConMeta, 1)
                : null,
            totales?.promedioHorasCierre is double horas ? Math.Round(horas, 1) : null,
            porEstado,
            porCategoria,
            porDistrito);
    }

    /// <summary>
    /// Puntos en formato GeoJSON para Leaflet. La versión pública no lleva ids, códigos ni prioridad
    /// y redondea la ubicación a unos 100 m, para que el reporte sea anónimo (RN-07). Sí lleva la
    /// primera foto del reporte, para que el vecino reconozca el problema en el mapa.
    /// </summary>
    public async Task<MapaSalida> MapaAsync(FiltroIndicadores filtro, long? unidadId, bool publico, CancellationToken cancelacion)
    {
        var (casos, _, _) = Filtrar(filtro, unidadId);
        if (publico)
            casos = casos.Where(c => !ocultosAlPublico.Contains(c.Estado));

        var puntos = await casos
            .SelectMany(c => c.Reportes.Where(r => !r.EsApoyo).Select(r => new
            {
                c.Id, categoria = c.Categoria.Codigo, c.Estado, c.Prioridad, c.Vencido, c.FechaCreacion,
                latitud = r.Ubicacion.Y, longitud = r.Ubicacion.X,
                foto = r.FotosReporte.OrderBy(f => f.Id).Select(f => f.Url).FirstOrDefault(),
            }))
            .OrderByDescending(p => p.FechaCreacion)
            .Take(PuntosMaximos)
            .ToListAsync(cancelacion);

        return new MapaSalida("FeatureCollection", puntos.Select(p => new PuntoMapa(
            "Feature",
            new GeometriaPunto("Point", publico
                ? [Math.Round(p.longitud, 3), Math.Round(p.latitud, 3)]
                : [p.longitud, p.latitud]),
            publico
                ? new PropiedadesPublicas(p.categoria, ValoresBd.DesdeBd<EstadoCaso>(p.Estado).Texto(), p.foto)
                : new PropiedadesPanel(p.Id, p.categoria, p.Estado, p.Prioridad, p.Vencido))).ToList());
    }

    private (IQueryable<Caso> Casos, DateOnly Desde, DateOnly Hasta) Filtrar(FiltroIndicadores filtro, long? unidadId)
    {
        var hasta = filtro.Hasta ?? HoraBolivia.Hoy();
        var desde = filtro.Desde ?? hasta.AddDays(-(DiasPorDefecto - 1));
        if (desde > hasta)
            throw new ReglaNegocioException("La fecha inicial no puede ser posterior a la final.");
        if (hasta.DayNumber - desde.DayNumber >= DiasMaximos)
            throw new ReglaNegocioException("El periodo máximo es de un año.");

        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var casos = db.Casos.AsNoTracking().Where(c => c.FechaCreacion >= inicio && c.FechaCreacion < fin);

        if (unidadId is long unidad)
            casos = casos.Where(c => c.UnidadId == unidad);
        if (filtro.DistritoId is long distrito)
            casos = casos.Where(c => c.Reportes.Any(r => !r.EsApoyo && r.DistritoId == distrito));
        if (filtro.Categoria is not null)
        {
            if (!ValoresBd.TryDesdeBd<CodigoCategoria>(filtro.Categoria, out var categoria))
                throw new ReglaNegocioException("La categoría debe ser bache, alumbrado o residuos.");
            var codigo = categoria.ABd();
            casos = casos.Where(c => c.Categoria.Codigo == codigo);
        }
        return (casos, desde, hasta);
    }
}
