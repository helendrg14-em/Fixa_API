using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Marca como vencidos los casos abiertos cuya fecha objetivo ya pasó (RN-06) y deja un evento
/// en el historial de cada uno. Es una sola instrucción SQL: si dos procesos corren a la vez,
/// PostgreSQL bloquea cada fila y ningún caso se marca ni se registra dos veces.
/// </summary>
public sealed class ControlVencimientos(FixaDbContext db)
{
    public Task<int> MarcarAsync(CancellationToken cancelacion) =>
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
}
