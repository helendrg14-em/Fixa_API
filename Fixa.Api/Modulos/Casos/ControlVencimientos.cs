using Fixa.Api.Infraestructura.Datos;

namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Marca como vencidos los casos abiertos cuya fecha objetivo ya pasó (RN-06) y deja un evento
/// en el historial de cada uno. La instrucción depende del motor (ver IConsultasMotor): en los
/// dos, si dos procesos corren a la vez, ningún caso se marca ni se registra dos veces.
/// </summary>
public sealed class ControlVencimientos(FixaDbContext db)
{
    public Task<int> MarcarAsync(CancellationToken cancelacion) => db.Consultas.MarcarVencidosAsync(cancelacion);
}
