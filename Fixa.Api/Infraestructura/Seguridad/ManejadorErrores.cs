using Fixa.Api.Dominio;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Convierte las excepciones en respuestas ProblemDetails. Las reglas de negocio muestran su
/// mensaje; cualquier otro error responde un texto genérico y el detalle solo va al log,
/// para no exponer rutas, consultas ni la estructura interna.
/// </summary>
public sealed class ManejadorErrores(IProblemDetailsService problemas, ILogger<ManejadorErrores> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception error, CancellationToken cancelacion)
    {
        var (estado, titulo) = error switch
        {
            ReglaNegocioException => (StatusCodes.Status422UnprocessableEntity, error.Message),
            NoEncontradoException => (StatusCodes.Status404NotFound, error.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "No tienes permiso para esta acción."),
            _ => (StatusCodes.Status500InternalServerError, "Ocurrió un error inesperado. Intenta de nuevo."),
        };
        if (estado == StatusCodes.Status500InternalServerError)
            log.LogError(error, "Error no controlado en {Ruta}", contexto.Request.Path);

        contexto.Response.StatusCode = estado;
        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = new ProblemDetails { Status = estado, Title = titulo },
            Exception = error,
        });
    }
}
