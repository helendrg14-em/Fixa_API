using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Calcula la fecha objetivo de un caso con la meta de atención vigente de su categoría y
/// prioridad (RN-06). La meta puede estar en horas corridas o en días hábiles; los días
/// hábiles saltan sábados, domingos y los feriados registrados.
/// </summary>
public sealed class PlazoAtencion(FixaDbContext db)
{
    public async Task<DateTime?> FechaObjetivoAsync(long categoriaId, Prioridad prioridad, CancellationToken cancelacion)
    {
        var hoy = HoraBolivia.Hoy();
        var meta = await db.MetasAtencion.AsNoTracking()
            .Where(m => m.CategoriaId == categoriaId && m.Prioridad == prioridad.ABd()
                        && m.VigenteDesde <= hoy && (m.VigenteHasta == null || m.VigenteHasta >= hoy))
            .OrderByDescending(m => m.VigenteDesde)
            .FirstOrDefaultAsync(cancelacion);

        var inicio = HoraBolivia.Ahora();
        if (meta?.PlazoHoras is int horas)
            return inicio.AddHours(horas);
        if (meta?.PlazoDiasHabiles is not int dias)
            return null;

        var feriados = (await db.Feriados.AsNoTracking()
            .Where(f => f.Fecha > hoy && f.Fecha <= hoy.AddDays(dias * 3 + 14))
            .Select(f => f.Fecha)
            .ToListAsync(cancelacion)).ToHashSet();

        var fecha = inicio;
        while (dias > 0)
        {
            fecha = fecha.AddDays(1);
            var dia = DateOnly.FromDateTime(fecha);
            if (fecha.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !feriados.Contains(dia))
                dias--;
        }
        return fecha;
    }
}
