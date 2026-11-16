namespace Fixa.Api.Modulos.Reportes.Validacion;

public sealed record DatosAValidar(
    Categorias.DatosReporte? Reporte = null,
    Dominio.CodigoCategoria? Categoria = null,
    string? ClaveIdempotencia = null,
    string? Direccion = null,
    string? Referencia = null,
    string? CorreoContacto = null,
    string? TokenDispositivo = null);

public abstract class EslabonValidacion
{
    private EslabonValidacion? siguiente;

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
