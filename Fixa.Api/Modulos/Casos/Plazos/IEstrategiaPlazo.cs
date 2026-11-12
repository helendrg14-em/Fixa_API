namespace Fixa.Api.Modulos.Casos.Plazos;

/// <summary>
/// Patrón Strategy — Estrategia: una forma de calcular la fecha objetivo de un caso a partir del
/// momento en que se fija su prioridad. Cada meta de atención (RN-06) se mide de una sola forma:
/// en horas corridas o en días hábiles. PlazoAtencion elige la estrategia y la usa sin saber
/// cómo calcula por dentro.
/// </summary>
public interface IEstrategiaPlazo
{
    Task<DateTime> FechaObjetivoAsync(DateTime inicio, CancellationToken cancelacion);
}
