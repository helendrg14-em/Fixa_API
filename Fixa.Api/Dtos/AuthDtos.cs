using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Dtos;

public record SolicitudLogin(
    [Required, EmailAddress, StringLength(120)] string Correo,
    [Required, StringLength(128)] string Password);

public record RespuestaLogin(string Token, DateTime Expira, string Rol, string Nombre);

public record UsuarioActual(long Id, string? Nombre, string? Rol);
