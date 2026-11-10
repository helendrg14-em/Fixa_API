using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Modulos.Indicadores;

/// <summary>Filtros de la URL. Sin fechas se usan los últimos 30 días; el periodo máximo es un año.</summary>
public record FiltroIndicadores(
    DateOnly? Desde,
    DateOnly? Hasta,
    [Range(1, long.MaxValue)] long? DistritoId,
    [StringLength(20)] string? Categoria);
