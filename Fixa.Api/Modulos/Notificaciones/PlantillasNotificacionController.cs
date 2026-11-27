using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Dtos;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Notificaciones;

/// <summary>
/// Textos de los avisos al vecino, uno por estado del caso. Solo el administrador los cambia.
/// En el cuerpo, {codigo} y {estado} se reemplazan al enviar.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/v1/panel/plantillas-notificacion")]
[Produces("application/json")]
public sealed class PlantillasNotificacionController(FixaDbContext db) : ControllerBase
{
    // GET /api/v1/panel/plantillas-notificacion
    [HttpGet]
    [EndpointSummary("Listar las plantillas de aviso")]
    [ProducesResponseType<List<PlantillaNotificacionSalida>>(StatusCodes.Status200OK)]
    public async Task<List<PlantillaNotificacionSalida>> Listar(CancellationToken cancelacion) =>
        await db.PlantillasNotificacion.AsNoTracking()
            .OrderBy(p => p.Id)
            .Select(p => new PlantillaNotificacionSalida(p.Id, p.Estado, p.Titulo, p.Cuerpo, p.Activa, p.FechaActualizacion))
            .ToListAsync(cancelacion);

    // GET /api/v1/panel/plantillas-notificacion/5
    [HttpGet("{id:long}")]
    [EndpointSummary("Consultar una plantilla de aviso por ID")]
    [ProducesResponseType<PlantillaNotificacionSalida>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<PlantillaNotificacionSalida> ObtenerPorId(long id, CancellationToken cancelacion) =>
        await db.PlantillasNotificacion.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PlantillaNotificacionSalida(p.Id, p.Estado, p.Titulo, p.Cuerpo, p.Activa, p.FechaActualizacion))
            .FirstOrDefaultAsync(cancelacion)
        ?? throw NoExiste();

    // POST /api/v1/panel/plantillas-notificacion
    [HttpPost]
    [EndpointSummary("Crear la plantilla de aviso de un estado")]
    [ProducesResponseType<PlantillaNotificacionSalida>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(PlantillaNotificacionEntrada entrada, CancellationToken cancelacion)
    {
        var estado = EstadoValido(entrada.Estado);
        if (await db.PlantillasNotificacion.AnyAsync(p => p.Estado == estado, cancelacion))
            throw new ReglaNegocioException("Ese estado ya tiene una plantilla; edítala en lugar de crear otra.");

        var plantilla = new PlantillaNotificacion { Estado = estado };
        Aplicar(plantilla, entrada);
        db.PlantillasNotificacion.Add(plantilla);
        await db.SaveChangesAsync(cancelacion);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = plantilla.Id }, await ObtenerPorId(plantilla.Id, cancelacion));
    }

    // PUT /api/v1/panel/plantillas-notificacion/5
    [HttpPut("{id:long}")]
    [EndpointSummary("Editar una plantilla de aviso")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Editar(long id, PlantillaNotificacionEntrada entrada, CancellationToken cancelacion)
    {
        var plantilla = await db.PlantillasNotificacion.FirstOrDefaultAsync(p => p.Id == id, cancelacion) ?? throw NoExiste();
        var estado = EstadoValido(entrada.Estado);
        if (await db.PlantillasNotificacion.AnyAsync(p => p.Estado == estado && p.Id != id, cancelacion))
            throw new ReglaNegocioException("Ese estado ya tiene otra plantilla.");

        plantilla.Estado = estado;
        Aplicar(plantilla, entrada);
        await db.SaveChangesAsync(cancelacion);
        return NoContent();
    }

    // DELETE /api/v1/panel/plantillas-notificacion/5  (sin plantilla, el estado usa el texto por defecto)
    [HttpDelete("{id:long}")]
    [EndpointSummary("Eliminar una plantilla de aviso")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancelacion)
    {
        var plantilla = await db.PlantillasNotificacion.FirstOrDefaultAsync(p => p.Id == id, cancelacion) ?? throw NoExiste();
        db.PlantillasNotificacion.Remove(plantilla);
        await db.SaveChangesAsync(cancelacion);
        return NoContent();
    }

    private static void Aplicar(PlantillaNotificacion plantilla, PlantillaNotificacionEntrada entrada)
    {
        plantilla.Titulo = entrada.Titulo.Trim();
        plantilla.Cuerpo = entrada.Cuerpo.Trim();
        plantilla.Activa = entrada.Activa;
        plantilla.FechaActualizacion = HoraBolivia.Ahora();
    }

    private static string EstadoValido(string estado) =>
        ValoresBd.TryDesdeBd<EstadoCaso>(estado.Trim(), out var valor)
            ? valor.ABd()
            : throw new ReglaNegocioException("El estado no existe.");

    private static NoEncontradoException NoExiste() => new("La plantilla no existe.");
}
