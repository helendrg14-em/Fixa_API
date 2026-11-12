namespace Fixa.Api.Modulos.Reportes.Validacion;

/// <summary>Lo que envía el vecino y que la cadena revisa. Cada eslabón mira solo lo suyo.</summary>
public sealed record DatosAValidar(
    Categorias.DatosReporte? Reporte = null,
    Dominio.CodigoCategoria? Categoria = null,
    string? ClaveIdempotencia = null,
    string? Direccion = null,
    string? Referencia = null,
    string? CorreoContacto = null,
    string? TokenDispositivo = null);

/// <summary>
/// Patrón Chain of Responsibility — Manejador: cada eslabón revisa una sola regla, anota el error
/// si la regla no se cumple y pasa los datos al siguiente. Así ninguna clase concentra todas las
/// validaciones, y cada flujo (reporte nuevo o apoyo) arma su propia cadena con los eslabones
/// que necesita. Se juntan todos los errores para mostrarle al vecino todo lo que debe corregir
/// de una sola vez.
/// </summary>
public abstract class EslabonValidacion
{
    private EslabonValidacion? siguiente;

    /// <summary>Engancha el siguiente eslabón y lo devuelve, para escribir la cadena en una sola línea.</summary>
    public EslabonValidacion Luego(EslabonValidacion eslabon)
    {
        siguiente = eslabon;
        return eslabon;
    }

    public void Validar(DatosAValidar datos, List<string> errores)
    {
        Revisar(datos, errores);
        siguiente?.Validar(datos, errores);
    }

    protected abstract void Revisar(DatosAValidar datos, List<string> errores);
}
