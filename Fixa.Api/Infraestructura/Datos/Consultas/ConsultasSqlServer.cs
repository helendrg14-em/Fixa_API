using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Modulos.Reportes;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Fixa.Api.Infraestructura.Datos.Consultas;

/// <summary>
/// Consultas para SQL Server. Las ubicaciones son columnas geography, así que las distancias
/// de NetTopologySuite (IsWithinDistance, Distance) se traducen a STDistance y miden en metros.
/// </summary>
public sealed class ConsultasSqlServer(FixaDbContext db) : IConsultasMotor
{
    private const int ClaveDuplicada = 2601;
    private const int RestriccionUnicaDuplicada = 2627;

    // UPDLOCK + ROWLOCK: equivalente a FOR UPDATE de PostgreSQL.
    public Task<Caso?> BloquearCasoAsync(long casoId, CancellationToken cancelacion) =>
        db.Casos.FromSql($"SELECT * FROM casos WITH (UPDLOCK, ROWLOCK) WHERE id = {casoId}").FirstOrDefaultAsync(cancelacion);

    // HOLDLOCK evita que dos reportes simultáneos inserten la misma gestión dos veces.
    public async Task<int> SiguienteNumeroAsync(int anio, CancellationToken cancelacion)
    {
        var numero = await db.Database.SqlQuery<int>($"""
            MERGE contadores_codigo WITH (HOLDLOCK) AS contador
            USING (SELECT {anio} AS anio) AS gestion ON contador.anio = gestion.anio
            WHEN MATCHED THEN UPDATE SET ultimo = contador.ultimo + 1
            WHEN NOT MATCHED THEN INSERT (anio, ultimo) VALUES (gestion.anio, 1)
            OUTPUT inserted.ultimo AS [Value];
            """).ToListAsync(cancelacion);
        return numero[0];
    }

    // Una tabla temporal recoge los casos marcados: eventos_historial tiene claves foráneas y
    // SQL Server no permite escribir en ella directamente con OUTPUT INTO.
    public async Task<int> MarcarVencidosAsync(CancellationToken cancelacion)
    {
        var ahora = HoraBolivia.Ahora();
        ahora = ahora.AddTicks(-(ahora.Ticks % TimeSpan.TicksPerSecond));
        var finales = string.Join(',', EstadoCasoReglas.FinalesBd);

        var marcados = await db.Database.SqlQuery<int>($"""
            SET NOCOUNT ON;
            DECLARE @vencidos TABLE (id bigint, estado nvarchar(25));
            UPDATE casos SET vencido = 1
            OUTPUT inserted.id, inserted.estado INTO @vencidos
            WHERE vencido = 0
              AND fecha_objetivo IS NOT NULL
              AND fecha_objetivo < {ahora}
              AND estado NOT IN (SELECT value FROM STRING_SPLIT({finales}, ','));
            INSERT INTO eventos_historial (caso_id, fecha_hora, tipo, estado_anterior, estado_nuevo, detalle)
            SELECT id, {ahora}, {TipoEvento.Vencimiento.ABd()}, estado, estado, N'Se cumplió la fecha objetivo y el caso sigue abierto.'
            FROM @vencidos;
            SELECT COUNT(*) AS [Value] FROM @vencidos;
            """).ToListAsync(cancelacion);
        return marcados[0];
    }

    public Task<List<long>> CasosCercanosAlCasoAsync(long casoId, CancellationToken cancelacion)
    {
        var finales = EstadoCasoReglas.FinalesBd;
        return (
            from propio in db.Reportes
            where propio.CasoId == casoId
            from otro in db.Reportes
            where otro.CasoId != propio.CasoId
                  && otro.Caso.CategoriaId == propio.Caso.CategoriaId
                  && !finales.Contains(otro.Caso.Estado)
                  && propio.Ubicacion.IsWithinDistance(otro.Ubicacion, propio.Caso.Categoria.RadioCercaniaM)
            select otro.CasoId)
            .Distinct()
            .ToListAsync(cancelacion);
    }

    public Task<bool> EstaCercaDelCasoAsync(long casoId, double longitud, double latitud, CancellationToken cancelacion)
    {
        var punto = Punto(longitud, latitud);
        return db.Reportes.AnyAsync(
            reporte => reporte.CasoId == casoId
                       && reporte.Ubicacion.IsWithinDistance(punto, reporte.Caso.Categoria.RadioCercaniaM),
            cancelacion);
    }

    public async Task<List<CasoCercanoFila>> CasosCercanosAlPuntoAsync(
        string categoria, double longitud, double latitud, int maximo, CancellationToken cancelacion)
    {
        var punto = Punto(longitud, latitud);
        var finales = EstadoCasoReglas.FinalesBd;

        var filas = await db.Casos.AsNoTracking()
            .Where(caso => caso.Categoria.Codigo == categoria
                           && caso.Categoria.Activa
                           && !finales.Contains(caso.Estado))
            .Select(caso => new
            {
                caso.Id,
                caso.Estado,
                caso.FechaCreacion,
                TipoIncidencia = caso.Reportes.OrderBy(original => original.EsApoyo).ThenBy(original => original.Id)
                    .Select(original => original.TipoIncidencia).First(),
                Cercanos = caso.Reportes.Where(reporte => reporte.Ubicacion.IsWithinDistance(punto, caso.Categoria.RadioCercaniaM)),
                Reportes = caso.Reportes.Count,
            })
            .Where(fila => fila.Cercanos.Any())
            .Select(fila => new
            {
                fila.Id,
                fila.Estado,
                fila.FechaCreacion,
                fila.TipoIncidencia,
                Distancia = fila.Cercanos.Min(reporte => reporte.Ubicacion.Distance(punto)),
                fila.Reportes,
            })
            .OrderBy(fila => fila.Distancia)
            .Take(maximo)
            .ToListAsync(cancelacion);

        return filas.Select(fila => new CasoCercanoFila(
            fila.Id, fila.Estado, fila.FechaCreacion, fila.TipoIncidencia, fila.Distancia, fila.Reportes)).ToList();
    }

    // SQL Server no resta fechas como intervalo: DATEDIFF en segundos y se pasa a horas.
    public Task<double?> PromedioHorasCierreAsync(IQueryable<Caso> cerrados, CancellationToken cancelacion) =>
        cerrados.AverageAsync(c => (double?)EF.Functions.DateDiffSecond(c.FechaCreacion, c.FechaCierre!.Value) / 3600.0, cancelacion);

    public bool EsValorRepetido(DbUpdateException error) =>
        error.InnerException is SqlException { Number: ClaveDuplicada or RestriccionUnicaDuplicada };

    private static Point Punto(double longitud, double latitud) => new(longitud, latitud) { SRID = 4326 };
}
