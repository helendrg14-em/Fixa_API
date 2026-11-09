namespace Fixa.Api.Modulos.Reportes.Categorias;

/// <summary>Datos del formulario que dependen de la categoría.</summary>
public record DatosReporte(
    double? Latitud,
    double? Longitud,
    string? TipoIncidencia,
    string? Descripcion,
    int? NumeroLuminarias,
    string? CodigoAvisoCobranza);