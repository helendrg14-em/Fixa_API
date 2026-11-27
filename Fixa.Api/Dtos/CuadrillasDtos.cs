using System.ComponentModel.DataAnnotations;

namespace Fixa.Api.Dtos;

public sealed record CuadrillaEntrada(
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 60 caracteres.")] string Nombre,
    [Required(ErrorMessage = "El turno es obligatorio."), StringLength(10)] string Turno,
    [Range(1, long.MaxValue, ErrorMessage = "La unidad no es válida.")] long? UnidadId);

public sealed record CuadrillaSalida(long Id, string Nombre, string Turno, long UnidadId, string Unidad, int Tecnicos);

public sealed record TecnicoSalida(long Id, string Nombres, string Apellidos, string Correo, string? Especialidad, bool Activo);
