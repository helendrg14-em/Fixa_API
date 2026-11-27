using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Dtos;

public record FiltroIndicadores(
    DateOnly? Desde,
    DateOnly? Hasta,
    [Range(1, long.MaxValue)] long? DistritoId,
    [StringLength(20)] string? Categoria);

public record IndicadoresSalida(
    PeriodoSalida Periodo,
    int? Total,
    int? Cerrados,
    int? VencidosAbiertos,
    int? CerradosDentroDeMeta,
    double? PorcentajeDentroDeMeta,
    double? PromedioHorasCierre,
    List<ConteoPorEstado> PorEstado,
    List<ConteoPorCategoria> PorCategoria,
    List<ConteoPorDistrito> PorDistrito);

public record PeriodoSalida(DateOnly Desde, DateOnly Hasta);

public record ConteoPorEstado(string Estado, int Cantidad);

public record ConteoPorCategoria(string Categoria, int Cantidad);

public record ConteoPorDistrito(string Distrito, int Cantidad);

public record MapaSalida(string Type, List<PuntoMapa> Features);

public record PuntoMapa(string Type, GeometriaPunto Geometry, object Properties);

public record GeometriaPunto(string Type, double[] Coordinates);

public record PropiedadesPublicas(string Categoria, string Estado, string? Foto);

public record PropiedadesPanel(long CasoId, string Categoria, string Estado, string? Prioridad, bool Vencido);
