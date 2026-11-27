using System.Security.Cryptography;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

/// <summary>
/// Código visible y clave privada de cada reporte (RF-02). Lo usan el reporte nuevo y el apoyo a
/// un caso existente, así la regla se escribe una sola vez.
/// </summary>
public static class CodigoSeguimiento
{
    private const string AlfabetoClave = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // sin 0/O ni 1/I/L, para no confundirlos
    private const int LargoClave = 6;

    /// <summary>Clave aleatoria con generador criptográfico (no predecible).</summary>
    public static string NuevaClave() => RandomNumberGenerator.GetString(AlfabetoClave, LargoClave);

    /// <summary>
    /// Si la app reenvió una solicitud ya guardada (misma clave de idempotencia), devuelve ese
    /// reporte sin su clave: la clave privada se muestra una sola vez.
    /// </summary>
    public static async Task<ReporteRegistrado?> YaRegistradoAsync(FixaDbContext db, string? claveIdempotencia, CancellationToken cancelacion)
    {
        var codigo = await db.Reportes.AsNoTracking()
            .Where(reporte => reporte.ClaveIdempotencia == claveIdempotencia)
            .Select(reporte => reporte.Codigo)
            .FirstOrDefaultAsync(cancelacion);
        return codigo is null ? null : new ReporteRegistrado(codigo, Clave: null, YaExistia: true);
    }

    /// <summary>
    /// Código FX-AAAA-000123, correlativo por gestión. Debe llamarse dentro de una transacción:
    /// el contador queda bloqueado hasta confirmarla, así dos reportes nunca reciben el mismo número.
    /// </summary>
    public static async Task<string> SiguienteAsync(FixaDbContext db, CancellationToken cancelacion)
    {
        var anio = HoraBolivia.Hoy().Year;
        var numero = await db.Database.SqlQuery<int>($"""
            INSERT INTO contadores_codigo (anio, ultimo) VALUES ({anio}, 1)
            ON CONFLICT (anio) DO UPDATE SET ultimo = contadores_codigo.ultimo + 1
            RETURNING ultimo AS "Value"
            """).ToListAsync(cancelacion);
        return $"FX-{anio}-{numero[0]:D6}";
    }
}
