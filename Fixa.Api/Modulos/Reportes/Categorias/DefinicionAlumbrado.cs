namespace Fixa.Api.Modulos.Reportes.Categorias;

public class DefinicionAlumbrado : IDefinicionCategoria
{
    public IReadOnlyDictionary<string, string> Tipos() => new Dictionary<string, string>
    {
        ["luminaria_apagada"] = "Luminaria apagada",
        ["luminaria_intermitente"] = "Luminaria que parpadea",
        [CreadorCategoria.TipoOtro] = "Otro",
    };

    /// <summary>
    /// El supervisor usa el número de luminarias para fijar la prioridad. Si la falla no es
    /// de luminarias, el vecino elige "Otro" y la explica en la descripción.
    /// </summary>
    public IEnumerable<string> ValidarPropios(DatosReporte datos)
    {
        if (datos.TipoIncidencia != CreadorCategoria.TipoOtro && datos.NumeroLuminarias is null)
            yield return "Indica cuántas luminarias están afectadas o elige \"Otro\".";
        if (datos.NumeroLuminarias is < 1 or > 200)
            yield return "El número de luminarias debe estar entre 1 y 200.";
        if (datos.CodigoAvisoCobranza is { Length: > 30 })
            yield return "El código del aviso de cobranza tiene como máximo 30 caracteres.";
    }
}