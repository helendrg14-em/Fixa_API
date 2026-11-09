using System.ComponentModel.DataAnnotations;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Fixa.Api.Modulos.Auth;
using Fixa.Api.Modulos.Casos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Intervenciones;

/// <summary>Formulario multipart con el trabajo realizado y sus fotos de evidencia.</summary>
public class SolicitudIntervencion
{
    [Required, StringLength(2000, MinimumLength = 10)]
    public string TrabajoRealizado { get; set; } = "";

    [Required, StringLength(15)]
    public string TipoEvidencia { get; set; } = "";

    [StringLength(1000)]
    public string? Descripcion { get; set; }

    public List<IFormFile> Fotos { get; set; } = [];
}

/// <summary>
/// Órdenes de trabajo del técnico (RF-09). Solo ve los casos programados para su cuadrilla,
/// sin datos de contacto del vecino. Registra el trabajo, pero no cierra (RN-04).
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Tecnico)]
[Route("api/v1/panel/ordenes")]
public class OrdenesController(FixaDbContext db, GestorCasos gestor) : ControllerBase
{
    private static readonly string[] estadosAbiertos = [EstadoCaso.Programada.ABd(), EstadoCaso.EnAtencion.ABd()];

    private long TecnicoId => ServicioAutenticacion.Instancia.IdUsuario(User);

    // GET /api/v1/panel/ordenes
    [HttpGet]
    public async Task<IActionResult> Pendientes(CancellationToken cancelacion)
    {
        var cuadrilla = await db.Usuarios.Where(u => u.Id == TecnicoId).Select(u => u.CuadrillaId).FirstOrDefaultAsync(cancelacion);
        var ordenes = await db.Programaciones.AsNoTracking()
            .Where(p => p.Vigente && cuadrilla != null && p.CuadrillaId == cuadrilla && estadosAbiertos.Contains(p.Caso.Estado))
            .OrderBy(p => p.FechaPrevista).ThenBy(p => p.CasoId)
            .Select(p => new
            {
                casoId = p.CasoId,
                p.FechaPrevista,
                p.Caso.Estado,
                p.Caso.Prioridad,
                categoria = p.Caso.Categoria.Nombre,
                p.Caso.RequiereVerificacionCampo,
                reportes = p.Caso.Reportes.Select(r => new
                {
                    r.TipoIncidencia, r.Descripcion, r.Direccion, r.Referencia, r.NumeroLuminarias,
                    latitud = r.Ubicacion.Y, longitud = r.Ubicacion.X,
                    fotos = r.FotosReporte.Select(f => f.Url),
                }),
            })
            .ToListAsync(cancelacion);
        return Ok(ordenes);
    }

    // POST /api/v1/panel/ordenes/5/inicio
    [HttpPost("{casoId:long}/inicio")]
    public async Task<IActionResult> Iniciar(long casoId, CancellationToken cancelacion)
    {
        await gestor.IniciarAtencionAsync(casoId, TecnicoId, cancelacion);
        return NoContent();
    }

    // POST /api/v1/panel/ordenes/5/intervenciones (multipart/form-data)
    [HttpPost("{casoId:long}/intervenciones")]
    [EnableRateLimiting(Limites.Registro)]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> RegistrarIntervencion(long casoId, [FromForm] SolicitudIntervencion solicitud, CancellationToken cancelacion)
    {
        if (!ValoresBd.TryDesdeBd<TipoEvidencia>(solicitud.TipoEvidencia, out var tipo))
            throw new ReglaNegocioException("El tipo de evidencia debe ser fotografia o acta_tecnica.");

        await gestor.RegistrarIntervencionAsync(
            casoId, TecnicoId, solicitud.TrabajoRealizado.Trim(), tipo, solicitud.Descripcion?.Trim(), solicitud.Fotos, cancelacion);
        return NoContent();
    }
}
