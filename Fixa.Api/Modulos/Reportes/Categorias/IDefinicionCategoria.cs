namespace Fixa.Api.Modulos.Reportes.Categorias;

/// <summary>
/// Patrón Factory Method — Producto: lo que cambia entre una categoría y otra al reportar.
/// </summary>
public interface IDefinicionCategoria
{
    /// <summary>Tipos de incidencia que el vecino puede elegir (valor → texto visible).</summary>
    IReadOnlyDictionary<string, string> Tipos();

    /// <summary>Errores de los datos que solo pide esta categoría.</summary>
    IEnumerable<string> ValidarPropios(DatosReporte datos);
}