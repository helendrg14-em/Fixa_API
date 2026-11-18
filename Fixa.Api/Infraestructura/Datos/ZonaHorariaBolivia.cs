using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fixa.Api.Infraestructura.Datos;

/// <summary>
/// Las fechas se guardan en hora de Bolivia, sin zona horaria (convención de Laravel). Al abrir
/// cada conexión se fija la zona de la sesión, así los valores por defecto (CURRENT_TIMESTAMP)
/// también salen en hora de Bolivia. Se hace con SET y no con el parámetro Timezone de la
/// conexión porque el pooler de Supabase ignora ese parámetro y dejaba las fechas en UTC.
/// </summary>
public sealed class ZonaHorariaBolivia : DbConnectionInterceptor
{
    private const string Fijar = "SET TIME ZONE 'America/La_Paz'";

    public override void ConnectionOpened(DbConnection conexion, ConnectionEndEventData datos)
    {
        using var comando = conexion.CreateCommand();
        comando.CommandText = Fijar;
        comando.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection conexion, ConnectionEndEventData datos, CancellationToken cancelacion = default)
    {
        await using var comando = conexion.CreateCommand();
        comando.CommandText = Fijar;
        await comando.ExecuteNonQueryAsync(cancelacion);
    }
}
