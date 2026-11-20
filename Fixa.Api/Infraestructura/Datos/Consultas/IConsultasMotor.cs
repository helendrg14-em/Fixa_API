using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Modulos.Reportes;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Infraestructura.Datos.Consultas;

/// <summary>
/// Strategy: las pocas consultas que no se pueden escribir en LINQ igual para todos los motores
/// (bloqueo de filas, contador atómico, distancias en metros). FIXA usa PostgreSQL + PostGIS en
/// producción; SQL Server queda disponible con la misma API (ver FixaDbContext.Consultas).
/// </summary>
public interface IConsultasMotor
{
    /// <summary>Lee el caso bloqueando su fila hasta el fin de la transacción.</summary>
    Task<Caso?> BloquearCasoAsync(long casoId, CancellationToken cancelacion);

    /// <summary>Siguiente número del código FX-AAAA-000123 de la gestión (dentro de una transacción).</summary>
    Task<int> SiguienteNumeroAsync(int anio, CancellationToken cancelacion);

    /// <summary>Marca los casos abiertos con la fecha objetivo vencida y registra el evento.</summary>
    Task<int> MarcarVencidosAsync(CancellationToken cancelacion);

    /// <summary>Casos abiertos de la misma categoría dentro del radio de cercanía del caso.</summary>
    Task<List<long>> CasosCercanosAlCasoAsync(long casoId, CancellationToken cancelacion);

    /// <summary>Si el punto está dentro del radio de cercanía de algún reporte del caso.</summary>
    Task<bool> EstaCercaDelCasoAsync(long casoId, double longitud, double latitud, CancellationToken cancelacion);

    /// <summary>Casos abiertos de la categoría cerca del punto, del más cercano al más lejano.</summary>
    Task<List<CasoCercanoFila>> CasosCercanosAlPuntoAsync(
        string categoria, double longitud, double latitud, int maximo, CancellationToken cancelacion);

    /// <summary>Promedio de horas entre la creación y el cierre de los casos dados (null si no hay).</summary>
    Task<double?> PromedioHorasCierreAsync(IQueryable<Caso> cerrados, CancellationToken cancelacion);

    /// <summary>Si el error de guardado es por un valor único repetido.</summary>
    bool EsValorRepetido(DbUpdateException error);
}
