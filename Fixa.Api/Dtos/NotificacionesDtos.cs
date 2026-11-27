using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Dtos;

public record PlantillaNotificacionEntrada(
    [Required(ErrorMessage = "El estado es obligatorio."), StringLength(25)] string Estado,
    [Required(ErrorMessage = "El título es obligatorio."), StringLength(80, MinimumLength = 3, ErrorMessage = "El título debe tener entre 3 y 80 caracteres.")] string Titulo,
    [Required(ErrorMessage = "El texto es obligatorio."), StringLength(500, MinimumLength = 10, ErrorMessage = "El texto debe tener entre 10 y 500 caracteres.")] string Cuerpo,
    bool Activa = true);

public record PlantillaNotificacionSalida(long Id, string Estado, string Titulo, string Cuerpo, bool Activa, DateTime FechaActualizacion);

public record NotificacionSalida(
    long Id,
    long CasoId,
    string CodigoReporte,
    string EstadoCaso,
    string Canal,
    string EstadoEntrega,
    DateTime FechaEvento,
    DateTime? FechaEnvio);
