using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos.Plazos;

/// <summary>
/// Patrón Strategy — Estrategia concreta: el plazo solo avanza en días hábiles; salta sábados,
/// domingos y los feriados registrados por el administrador.
/// </summary>
public sealed class PlazoEnDiasHabiles(int diasHabiles, FixaDbContext db) : IEstrategiaPlazo
{
    public async Task<DateTime> FechaObjetivoAsync(DateTime inicio, CancellationToken cancelacion)
    {
        var feriados = await FeriadosDelPeriodoAsync(DateOnly.FromDateTime(inicio), cancelacion);

        var fechaObjetivo = inicio;
        var diasPendientes = diasHabiles;
        while (diasPendientes > 0)
        {
            fechaObjetivo = fechaObjetivo.AddDays(1);
            if (EsDiaHabil(fechaObjetivo, feriados))
                diasPendientes--;
        }
        return fechaObjetivo;
    }

    private static bool EsDiaHabil(DateTime fecha, HashSet<DateOnly> feriados) =>
        fecha.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)
        && !feriados.Contains(DateOnly.FromDateTime(fecha));

    /// <summary>
    /// Solo trae los feriados que pueden caer dentro del plazo. El margen (triple de días más dos
    /// semanas) cubre los fines de semana y los feriados seguidos que alargan el conteo.
    /// </summary>
    private async Task<HashSet<DateOnly>> FeriadosDelPeriodoAsync(DateOnly hoy, CancellationToken cancelacion)
    {
        var limite = hoy.AddDays(diasHabiles * 3 + 14);
        var feriados = await db.Feriados.AsNoTracking()
            .Where(feriado => feriado.Fecha > hoy && feriado.Fecha <= limite)
            .Select(feriado => feriado.Fecha)
            .ToListAsync(cancelacion);
        return feriados.ToHashSet();
    }
}
