namespace Fixa.Api.Modulos.Casos.Plazos;

/// <summary>
/// Patrón Strategy — Estrategia concreta: el plazo corre sin pausa, de día y de noche, también en
/// fines de semana y feriados. Sirve para las prioridades urgentes.
/// </summary>
public sealed class PlazoEnHorasCorridas(int horas) : IEstrategiaPlazo
{
    public Task<DateTime> FechaObjetivoAsync(DateTime inicio, CancellationToken cancelacion) =>
        Task.FromResult(inicio.AddHours(horas));
}
