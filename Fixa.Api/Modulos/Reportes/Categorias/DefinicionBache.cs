namespace Fixa.Api.Modulos.Reportes.Categorias;

public class DefinicionBache : IDefinicionCategoria
{
    public IReadOnlyDictionary<string, string> Tipos() => new Dictionary<string, string>
    {
        ["bache"] = "Bache",
        ["hundimiento"] = "Hundimiento",
        ["calle_deteriorada"] = "Calle deteriorada",
        [CreadorCategoria.TipoOtro] = "Otro",
    };

    public IEnumerable<string> ValidarPropios(DatosReporte datos)
    {
        if (datos.NumeroLuminarias is not null || datos.CodigoAvisoCobranza is not null)
            yield return "Ese dato no corresponde a esta categoría.";
    }
}