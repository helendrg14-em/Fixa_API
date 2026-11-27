using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Fixa.Api.Dtos;

public class SolicitudReporte
{
    [Required, StringLength(20)] public string Categoria { get; set; } = "";
    [Required] public double? Latitud { get; set; }
    [Required] public double? Longitud { get; set; }
    [Required, StringLength(40)] public string TipoIncidencia { get; set; } = "";
    [Required, StringLength(1000)] public string Descripcion { get; set; } = "";
    [Range(1, 200)] public int? NumeroLuminarias { get; set; }
    [StringLength(30)] public string? CodigoAvisoCobranza { get; set; }
    [StringLength(200)] public string? Direccion { get; set; }
    [StringLength(200)] public string? Referencia { get; set; }
    [EmailAddress, StringLength(120)] public string? CorreoContacto { get; set; }
    [StringLength(255)] public string? TokenDispositivo { get; set; }
    [Required, StringLength(64, MinimumLength = 8)] public string ClaveIdempotencia { get; set; } = "";
    public List<IFormFile> Fotos { get; set; } = [];
}

public class SolicitudReporteAsistido
{
    [Required, StringLength(20)] public string Categoria { get; set; } = "";
    [Required] public double? Latitud { get; set; }
    [Required] public double? Longitud { get; set; }
    [Required, StringLength(40)] public string TipoIncidencia { get; set; } = "";
    [Required, StringLength(1000)] public string Descripcion { get; set; } = "";
    [Range(1, 200)] public int? NumeroLuminarias { get; set; }
    [StringLength(30)] public string? CodigoAvisoCobranza { get; set; }
    [StringLength(200)] public string? Direccion { get; set; }
    [StringLength(200)] public string? Referencia { get; set; }
    [EmailAddress, StringLength(120)] public string? CorreoContacto { get; set; }
    [Required, StringLength(25)] public string Canal { get; set; } = "";
    [StringLength(200)] public string? MotivoSinFoto { get; set; }
    [Required, StringLength(64, MinimumLength = 8)] public string ClaveIdempotencia { get; set; } = "";
    public List<IFormFile> Fotos { get; set; } = [];
}

public record SolicitudSeguimiento(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave);

public record SolicitudRevisionCierre(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave,
    [Required, StringLength(1000, MinimumLength = 10)] string Motivo);

public record SolicitudConformidad(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave,
    [Required] bool? Conforme);

public record SolicitudDispositivo(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave,
    [StringLength(255)] string? TokenDispositivo);

public record FiltroCercanos(
    [Required, StringLength(20)] string Categoria,
    [Required, Range(-90, 90)] double? Latitud,
    [Required, Range(-180, 180)] double? Longitud);

public record SolicitudApoyo(
    [Required, Range(-90, 90)] double? Latitud,
    [Required, Range(-180, 180)] double? Longitud,
    [Required, StringLength(64, MinimumLength = 8)] string ClaveIdempotencia,
    [EmailAddress, StringLength(120)] string? CorreoContacto,
    [StringLength(255)] string? TokenDispositivo);

public record ReporteRegistradoSalida(
    string Codigo,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Clave,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] long? CasoId,
    string Mensaje);

public record MensajeSalida(string Mensaje);

public record SeguimientoSalida(
    string Codigo,
    string Estado,
    string Categoria,
    string? Unidad,
    DateTime FechaRegistro,
    DateTime? FechaCierre,
    DateTime? UltimaActualizacion,
    bool EsApoyo,
    bool? Conformidad,
    DateTime? RevisionDisponibleHasta,
    RevisionSalida? Revision);

public record RevisionSalida(string Estado, string? Respuesta, DateTime? FechaResolucion);

public record CasoCercanoPublico(long CasoId, string Estado, string Tipo, int DistanciaMetros, int VecinosQueReportaron, DateOnly Desde);

public record CategoriaSalida(string Codigo, string Nombre, string Descripcion);

public record TipoIncidenciaSalida(string Valor, string Texto);
