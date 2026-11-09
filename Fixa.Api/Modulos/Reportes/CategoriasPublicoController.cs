using Fixa.Api.Dominio;
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
    public async Task<IActionResult> Listar()
    {
        var categorias = await db.Categorias.AsNoTracking()
            .Where(c => c.Activa)
            .OrderBy(c => c.Id)
            .Select(c => new { c.Codigo, c.Nombre, c.Descripcion })
            .ToListAsync();

        return Ok(categorias);
    }

    // GET /api/v1/publico/categorias/alumbrado/tipos
    [HttpGet("{codigo}/tipos")]
    public IActionResult Tipos(string codigo)
    {
        if (!ValoresBd.TryDesdeBd<CodigoCategoria>(codigo, out var categoria))
            throw new NoEncontradoException("La categoría no existe.");

        var tipos = CreadorCategoria.Para(categoria).Tipos()
            .Select(t => new { valor = t.Key, texto = t.Value });

        return Ok(tipos);
    }
}
