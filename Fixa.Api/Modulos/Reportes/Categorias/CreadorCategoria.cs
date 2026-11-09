using Fixa.Api.Dominio;

namespace Fixa.Api.Modulos.Reportes.Categorias;

/// <summary>
/// Patrón Factory Method — Creador: el formulario común (ubicación, tipo y descripción) vive
/// aquí, y cada subclase decide con CrearDefinicion() qué categoría se usa. Una categoría
/// nueva solo agrega su creador y su definición; este código no cambia.
/// </summary>
public abstract class CreadorCategoria
{
    public const string TipoOtro = "otro";

    protected abstract IDefinicionCategoria CrearDefinicion();

    public static CreadorCategoria Para(CodigoCategoria codigo) => codigo switch
    {
        CodigoCategoria.Bache => new CreadorBache(),
        CodigoCategoria.Alumbrado => new CreadorAlumbrado(),
        CodigoCategoria.Residuos => new CreadorResiduos(),
        _ => throw new ArgumentOutOfRangeException(nameof(codigo))
    };

    public IReadOnlyDictionary<string, string> Tipos() => CrearDefinicion().Tipos();

    /// <summary>Valida los datos comunes y los de la categoría; devuelve la lista de errores.</summary>
    public List<string> Validar(DatosReporte datos)
    {
        var definicion = CrearDefinicion();
        var errores = new List<string>();

        if (datos.Latitud is null or < -90 or > 90 || datos.Longitud is null or < -180 or > 180)
            errores.Add("Marca en el mapa dónde está el problema.");
        if (datos.TipoIncidencia is null || !definicion.Tipos().ContainsKey(datos.TipoIncidencia))
            errores.Add("Elige un tipo de incidencia válido.");
        if (string.IsNullOrWhiteSpace(datos.Descripcion) || datos.Descripcion.Length > 1000)
            errores.Add("Escribe una descripción de hasta 1000 caracteres.");

        errores.AddRange(definicion.ValidarPropios(datos));
        return errores;
    }
}