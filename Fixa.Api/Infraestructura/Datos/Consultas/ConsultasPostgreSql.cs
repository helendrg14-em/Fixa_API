using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Modulos.Reportes;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fixa.Api.Infraestructura.Datos.Consultas;

/// <summary>Consultas para PostgreSQL + PostGIS (producción: Supabase).</summary>
public sealed class ConsultasPostgreSql(FixaDbContext db) : IConsultasMotor
{
    // FOR UPDATE: dos personas no pueden cambiar el mismo caso a la vez.
    public Task<Caso?> BloquearCasoAsync(long casoId, CancellationToken cancelacion) =>
        db.Casos.FromSql($"SELECT * FROM casos WHERE id = {casoId} FOR UPDATE").FirstOrDefaultAsync(cancelacion);

    public async Task<int> SiguienteNumeroAsync(int anio, CancellationToken cancelacion)
    {
        var numero = await db.Database.SqlQuery<int>($"""
            INSERT INTO contadores_codigo (anio, ultimo) VALUES ({anio}, 1)
            ON CONFLICT (anio) DO UPDATE SET ultimo = contadores_codigo.ultimo + 1
            RETURNING ultimo AS "Value"
            """).ToListAsync(cancelacion);
        return numero[0];
    }

    // Una sola instrucción: si dos procesos corren a la vez, PostgreSQL bloquea cada fila y
    // ningún caso se marca ni se registra dos veces.
    public Task<int> MarcarVencidosAsync(CancellationToken cancelacion) =>
        db.Database.ExecuteSqlAsync($"""
            WITH ahora AS (SELECT LOCALTIMESTAMP(0) AS t), -- hora de Bolivia: la zona de la sesión es America/La_Paz
            vencidos AS (
                UPDATE casos SET vencido = true
                WHERE NOT vencido
                  AND fecha_objetivo IS NOT NULL
                  AND fecha_objetivo < (SELECT t FROM ahora)
                  AND estado <> ALL({EstadoCasoReglas.FinalesBd})
                RETURNING id, estado
            )
            INSERT INTO eventos_historial (caso_id, fecha_hora, tipo, estado_anterior, estado_nuevo, detalle)
            SELECT id, (SELECT t FROM ahora), {TipoEvento.Vencimiento.ABd()}, estado, estado, 'Se cumplió la fecha objetivo y el caso sigue abierto.'
            FROM vencidos
            """, cancelacion);

    // ST_DWithin con geography mide en metros; el radio sale de categorias.radio_cercania_m.
    public Task<List<long>> CasosCercanosAlCasoAsync(long casoId, CancellationToken cancelacion) =>
        db.Database.SqlQuery<long>($"""
            SELECT DISTINCT otro.caso_id AS "Value"
            FROM reportes propio
            JOIN casos caso ON caso.id = propio.caso_id
            JOIN categorias cat ON cat.id = caso.categoria_id
            JOIN reportes otro ON otro.caso_id <> propio.caso_id
            JOIN casos candidato ON candidato.id = otro.caso_id
            WHERE propio.caso_id = {casoId}
              AND candidato.categoria_id = caso.categoria_id
              AND candidato.estado <> ALL({EstadoCasoReglas.FinalesBd})
              AND ST_DWithin(propio.ubicacion::geography, otro.ubicacion::geography, cat.radio_cercania_m)
            """).ToListAsync(cancelacion);

    public async Task<bool> EstaCercaDelCasoAsync(long casoId, double longitud, double latitud, CancellationToken cancelacion)
    {
        var resultado = await db.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1
                FROM reportes reporte
                JOIN casos caso ON caso.id = reporte.caso_id
                JOIN categorias categoria ON categoria.id = caso.categoria_id
                WHERE reporte.caso_id = {casoId}
                  AND ST_DWithin(
                        reporte.ubicacion::geography,
                        ST_SetSRID(ST_MakePoint({longitud}, {latitud}), 4326)::geography,
                        categoria.radio_cercania_m)
            ) AS "Value"
            """).ToListAsync(cancelacion);
        return resultado[0];
    }

    // Consulta con parámetros (FormattableString): los valores nunca se pegan como texto SQL.
    public Task<List<CasoCercanoFila>> CasosCercanosAlPuntoAsync(
        string categoria, double longitud, double latitud, int maximo, CancellationToken cancelacion) =>
        db.Database.SqlQuery<CasoCercanoFila>($"""
            SELECT caso.id AS "CasoId",
                   caso.estado AS "Estado",
                   caso.fecha_creacion AS "FechaCreacion",
                   (SELECT original.tipo_incidencia FROM reportes original
                     WHERE original.caso_id = caso.id ORDER BY original.es_apoyo, original.id LIMIT 1) AS "TipoIncidencia",
                   MIN(ST_Distance(reporte.ubicacion::geography,
                                   ST_SetSRID(ST_MakePoint({longitud}, {latitud}), 4326)::geography)) AS "DistanciaMetros",
                   (SELECT count(*) FROM reportes todos WHERE todos.caso_id = caso.id)::int AS "Reportes"
            FROM casos caso
            JOIN categorias categoria ON categoria.id = caso.categoria_id
            JOIN reportes reporte ON reporte.caso_id = caso.id
            WHERE categoria.codigo = {categoria}
              AND categoria.activa
              AND caso.estado <> ALL({EstadoCasoReglas.FinalesBd})
              AND ST_DWithin(reporte.ubicacion::geography,
                             ST_SetSRID(ST_MakePoint({longitud}, {latitud}), 4326)::geography,
                             categoria.radio_cercania_m)
            GROUP BY caso.id
            ORDER BY "DistanciaMetros"
            LIMIT {maximo}
            """).ToListAsync(cancelacion);

    public Task<double?> PromedioHorasCierreAsync(IQueryable<Caso> cerrados, CancellationToken cancelacion) =>
        cerrados.AverageAsync(c => (double?)(c.FechaCierre!.Value - c.FechaCreacion).TotalHours, cancelacion);

    public bool EsValorRepetido(DbUpdateException error) =>
        error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
