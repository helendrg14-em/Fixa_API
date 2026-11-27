using Fixa.Api.Dominio;
using Fixa.Api.Dtos;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Reportes.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

/// <summary>
/// Datos públicos de las categorías para la app del ciudadano (sin login).
/// Usa el Factory Method: el controlador no sabe qué tipos tiene cada categoría,
/// se lo pregunta al creador que corresponde.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/publico/categorias")]
public class CategoriasPublicoController(FixaDbContext db) : ControllerBase
{
    // GET /api/v1/publico/categorias
    [HttpGet]
    public async Task<ActionResult<List<CategoriaSalida>>> Listar() =>
        await db.Categorias.AsNoTracking()
            .Where(c => c.Activa)
            .OrderBy(c => c.Id)
            .Select(c => new CategoriaSalida(c.Codigo, c.Nombre, c.Descripcion))
            .ToListAsync();

    // GET /api/v1/publico/categorias/alumbrado/tipos
    [HttpGet("{codigo}/tipos")]
    public ActionResult<List<TipoIncidenciaSalida>> Tipos(string codigo)
    {
        if (!ValoresBd.TryDesdeBd<CodigoCategoria>(codigo, out var categoria))
            throw new NoEncontradoException("La categoría no existe.");

        return CreadorCategoria.Para(categoria).Tipos()
            .Select(t => new TipoIncidenciaSalida(t.Key, t.Value))
            .ToList();
    }
}
