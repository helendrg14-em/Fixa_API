using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Dtos;

public class SolicitudIntervencion
{
    [Required, StringLength(2000, MinimumLength = 10)]
    public string TrabajoRealizado { get; set; } = "";

    [Required, StringLength(15)]
    public string TipoEvidencia { get; set; } = "";

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    public List<IFormFile> Fotos { get; set; } = [];
}

public record OrdenTrabajo(
    long CasoId,
    DateOnly FechaPrevista,
    string Estado,
    string? Prioridad,
    string Categoria,
    bool RequiereVerificacionCampo,
    List<ReporteDeOrden> Reportes);

public record ReporteDeOrden(
    string TipoIncidencia,
    string Descripcion,
    string? Direccion,
    string? Referencia,
    int? NumeroLuminarias,
    double Latitud,
    double Longitud,
    List<string> Fotos);
