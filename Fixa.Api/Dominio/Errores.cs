namespace Fixa.Api.Dominio;

/// <summary>Una regla de negocio impide la operación (respuesta 422).</summary>
public sealed class ReglaNegocioException(string mensaje) : Exception(mensaje);

/// <summary>El recurso no existe o no pertenece al usuario (respuesta 404, sin revelar cuál de las dos).</summary>
public sealed class NoEncontradoException(string mensaje = "No se encontró el recurso.") : Exception(mensaje);
