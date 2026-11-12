using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes.AccionesVecino;

/// <summary>
/// Patrón Template Method — Clase abstracta: toda acción del vecino sobre su reporte sigue la
/// misma receta, definida una sola vez en EjecutarAsync:
///   1. verificar el código y la clave (siempre igual, no se puede saltar),
///   2. revisar las reglas propias de la acción (paso opcional),
///   3. realizar la acción (paso que cada subclase escribe).
/// Así ninguna acción nueva puede olvidar la verificación de la clave.
/// </summary>
public abstract class AccionDelVecino<TResultado>(FixaDbContext db)
{
    protected FixaDbContext Db => db;

    /// <summary>El método plantilla: fija el orden de los pasos. Las subclases no lo cambian.</summary>
    public async Task<TResultado> EjecutarAsync(string codigo, string clave, CancellationToken cancelacion)
    {
        var reporteId = await VerificarCodigoYClaveAsync(codigo, clave, cancelacion)
            ?? throw new AccesoVecinoDenegadoException();

        await RevisarReglasAsync(reporteId, cancelacion);
        return await RealizarAsync(reporteId, cancelacion);
    }

    /// <summary>Paso opcional: reglas propias de la acción. Por defecto no hay ninguna.</summary>
    protected virtual Task RevisarReglasAsync(long reporteId, CancellationToken cancelacion) => Task.CompletedTask;

    /// <summary>Paso obligatorio: lo que hace la acción una vez verificado el vecino.</summary>
    protected abstract Task<TResultado> RealizarAsync(long reporteId, CancellationToken cancelacion);

    /// <summary>
    /// Id del reporte si el código y la clave coinciden. Se verifica en tiempo constante y con el
    /// mismo resultado exista o no el código, para no revelar qué códigos existen (RN-07).
    /// </summary>
    private async Task<long?> VerificarCodigoYClaveAsync(string codigo, string clave, CancellationToken cancelacion)
    {
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();
        var reporte = await db.Reportes.AsNoTracking()
            .Where(registro => registro.Codigo == codigoNormalizado)
            .Select(registro => new { registro.Id, registro.ClaveHash })
            .FirstOrDefaultAsync(cancelacion);

        var claveCorrecta = VerificadorClave.Verificar(clave.Trim().ToUpperInvariant(), reporte?.ClaveHash);
        return claveCorrecta ? reporte!.Id : null;
    }
}
