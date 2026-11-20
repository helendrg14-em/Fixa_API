using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Infraestructura.Datos;

/// <summary>
/// Elige el motor con BaseDatos:Proveedor. Por defecto PostgreSQL (producción en Render y
/// Supabase, esquema de Laravel); con "SqlServer" la API usa SQL Server y crea el esquema con
/// sus migraciones de EF Core.
/// </summary>
public static class ConfiguracionBaseDatos
{
    private static readonly string[] ServidoresLocales = ["localhost", ".", "(local)"];
    private static readonly string[] AlternativasLocales = [@".\SQLEXPRESS", @"(localdb)\MSSQLLocalDB"];

    public static bool UsaSqlServer(IConfiguration configuracion) =>
        string.Equals(configuracion["BaseDatos:Proveedor"], "SqlServer", StringComparison.OrdinalIgnoreCase);

    public static void Configurar(DbContextOptionsBuilder opciones, IConfiguration configuracion, string? conexionSqlServer)
    {
        if (conexionSqlServer is not null)
        {
            opciones.UseSqlServer(conexionSqlServer, sql => sql
                .UseNetTopologySuite()
                .MigrationsHistoryTable("ef_migraciones"));
            return;
        }

        // Las fechas se guardan en hora local de Bolivia (convención de Laravel): ZonaHorariaBolivia
        // fija la zona de cada sesión, aunque el servidor de PostgreSQL esté en UTC (Docker, Supabase).
        opciones
            .UseNpgsql(configuracion.GetConnectionString("Fixa"), npgsql => npgsql.UseNetTopologySuite())
            .AddInterceptors(new ZonaHorariaBolivia());
    }

    /// <summary>
    /// Cadena de SQL Server lista para usar. Si apunta a "localhost" y ahí no hay servidor, prueba
    /// SQL Express y LocalDB, así el proyecto corre en otra PC sin editar appsettings.
    /// </summary>
    public static string ConexionSqlServer(IConfiguration configuracion)
    {
        var cadena = configuracion.GetConnectionString("FixaSqlServer")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:FixaSqlServer.");
        var original = new SqlConnectionStringBuilder(cadena);
        if (!ServidoresLocales.Contains(original.DataSource, StringComparer.OrdinalIgnoreCase) || Responde(original))
            return cadena;

        foreach (var alternativa in AlternativasLocales)
        {
            var prueba = new SqlConnectionStringBuilder(cadena) { DataSource = alternativa };
            if (Responde(prueba))
                return prueba.ConnectionString;
        }
        return cadena; // ninguno responde: el error claro sale al aplicar las migraciones
    }

    private static bool Responde(SqlConnectionStringBuilder cadena)
    {
        // Se prueba contra master porque la base de FIXA puede no existir todavía.
        var prueba = new SqlConnectionStringBuilder(cadena.ConnectionString) { InitialCatalog = "master", ConnectTimeout = 5 };
        try
        {
            using var conexion = new SqlConnection(prueba.ConnectionString);
            conexion.Open();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }
}
