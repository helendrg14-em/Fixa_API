namespace Fixa.Api.Modulos.Reportes.Categorias;

public class DefinicionResiduos : IDefinicionCategoria
{
    public IReadOnlyDictionary<string, string> Tipos() => new Dictionary<string, string>
    {
        ["basura_en_calle"] = "Basura en la calle",
        ["basura_en_espacio_publico"] = "Basura en un espacio público",
        [CreadorCategoria.TipoOtro] = "Otro",
    };

    public IEnumerable<string> ValidarPropios(DatosReporte datos)
    {
        if (datos.NumeroLuminarias is not null || datos.CodigoAvisoCobranza is not null)
            yield return "Ese dato no corresponde a esta categoría.";
    }
}