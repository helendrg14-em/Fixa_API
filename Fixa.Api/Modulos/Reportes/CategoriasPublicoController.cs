using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Reportes.Categorias;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

/// <summary>
/// Datos públicos de las categorías para la app del ciudadano (sin login).
/// Usa el Factory Method: el controlador no sabe qué tipos tiene cada categoría,
/// se lo pregunta al creador que corresponde.
/// </summary>
[ApiController]
[Route("api/v1/publico/categorias")]
public class CategoriasPublicoController : ControllerBase
{
    // GET /api/v1/publico/categorias
    [HttpGet]
    public async Task<IActionResult> Listar([FromServices] FixaDbContext db)
    {
        var categorias = await db.Categorias
            .Where(c => c.Activa)
            .Select(c => new { c.Id, c.Codigo, c.Nombre })
            .ToListAsync();

        return Ok(categorias);
    }

    // GET /api/v1/publico/categorias/alumbrado/tipos
    [HttpGet("{codigo}/tipos")]
    public IActionResult Tipos(string codigo)
    {
        // TryParse también acepta números ("99"), por eso se exige que el valor exista en el enum.
        if (!Enum.TryParse<CodigoCategoria>(codigo, ignoreCase: true, out var categoria)
            || !Enum.IsDefined(categoria) || int.TryParse(codigo, out _))
            return NotFound(new { mensaje = "La categoría no existe." });

        var tipos = CreadorCategoria.Para(categoria).Tipos()
            .Select(t => new { valor = t.Key, texto = t.Value });

        return Ok(tipos);
    }
}