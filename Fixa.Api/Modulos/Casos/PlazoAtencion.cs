using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Casos.Plazos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

public sealed class PlazoAtencion(FixaDbContext db)
{
    public async Task<DateTime?> FechaObjetivoAsync(long categoriaId, Prioridad prioridad, CancellationToken cancelacion)
    {
        var metaVigente = await MetaVigenteAsync(categoriaId, prioridad, cancelacion);
        var estrategia = ElegirEstrategia(metaVigente);

        // Sin meta configurada para esa categoría y prioridad, el caso no tiene fecha objetivo.
        return estrategia is null ? null : await estrategia.FechaObjetivoAsync(HoraBolivia.Ahora(), cancelacion);
    }

    private IEstrategiaPlazo? ElegirEstrategia(MetaAtencion? meta) => meta switch
    {
        { PlazoHoras: int horas } => new PlazoEnHorasCorridas(horas),
        { PlazoDiasHabiles: int dias } => new PlazoEnDiasHabiles(dias, db),
        _ => null,
    };

    private Task<MetaAtencion?> MetaVigenteAsync(long categoriaId, Prioridad prioridad, CancellationToken cancelacion)
    {
        var hoy = HoraBolivia.Hoy();
        return db.MetasAtencion.AsNoTracking()
            .Where(meta => meta.CategoriaId == categoriaId && meta.Prioridad == prioridad.ABd()
                           && meta.VigenteDesde <= hoy && (meta.VigenteHasta == null || meta.VigenteHasta >= hoy))
            .OrderByDescending(meta => meta.VigenteDesde)
            .FirstOrDefaultAsync(cancelacion);
    }
}
