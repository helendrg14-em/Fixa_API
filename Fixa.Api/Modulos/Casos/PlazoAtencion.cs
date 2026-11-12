using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Casos.Plazos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Patrón Strategy — Contexto: calcula la fecha objetivo de un caso con la meta de atención
/// vigente de su categoría y prioridad (RN-06). No sabe calcular plazos: según la meta, elige la
/// estrategia (horas corridas o días hábiles) y le delega el cálculo. Una forma nueva de medir
/// plazos solo agrega su clase en Plazos/ y una línea en ElegirEstrategia.
/// </summary>
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
