using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Dtos;

public record SolicitudEstado([Required, StringLength(25)] string Estado, [StringLength(1000)] string? Motivo);
public record SolicitudPrioridad([Required, StringLength(10)] string Prioridad, [Required, StringLength(1000, MinimumLength = 10)] string Justificacion);
public record SolicitudProgramacion([Range(1, long.MaxValue)] long CuadrillaId, [Required] DateOnly? Fecha, [StringLength(200)] string? MotivoReprogramacion);
public record SolicitudDerivacion([Range(1, long.MaxValue)] long EntidadId, [Required, StringLength(1000)] string Motivo);
public record SolicitudValidacion(bool Aprobar, [StringLength(1000)] string? Observacion);
public record SolicitudCategoria([Required, StringLength(20)] string Categoria, [Required, StringLength(1000, MinimumLength = 10)] string Motivo);
public record SolicitudVinculacion([Range(1, long.MaxValue)] long CasoPrincipalId, [StringLength(1000)] string? Motivo);
public record SolicitudRespuestaRevision(bool Aceptar, [StringLength(1000)] string? Respuesta);

public record CasoResumen(
    long Id, string Estado, string? Prioridad, string Categoria, DateTime FechaCreacion, DateTime? FechaObjetivo, bool Vencido, int Reportes);

public record CasoDetalle(
    long Id,
    string Estado,
    string? Prioridad,
    string? JustificacionPrioridad,
    DateTime FechaCreacion,
    DateTime? FechaObjetivo,
    DateTime? FechaCierre,
    bool Vencido,
    bool RequiereVerificacionCampo,
    string Categoria,
    List<ReporteDelCaso> Reportes,
    List<EventoDelCaso> Historial,
    List<IntervencionDelCaso> Intervenciones);

public record ReporteDelCaso(
    string Codigo,
    string TipoIncidencia,
    string Descripcion,
    string? Direccion,
    string? Referencia,
    int? NumeroLuminarias,
    bool EsApoyo,
    DateTime FechaRegistro,
    double Latitud,
    double Longitud,
    List<string> Fotos);

public record EventoDelCaso(DateTime FechaHora, string Tipo, string? EstadoAnterior, string? EstadoNuevo, string Detalle, string? Motivo);

public record IntervencionDelCaso(long Id, DateTime Fecha, string TrabajoRealizado, string EstadoValidacion, List<EvidenciaSalida> Evidencias);

public record EvidenciaSalida(string Tipo, string Url);

public record CasoCercano(long Id, string Estado, string? Prioridad, DateTime FechaCreacion, int Reportes);

public record RevisionPendiente(long Id, long CasoId, string Codigo, string Motivo, DateTime FechaSolicitud, DateTime? FechaCierre);
