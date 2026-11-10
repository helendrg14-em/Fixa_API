using Fixa.Api.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fixa.Api.Modulos.Indicadores;

/// <summary>Mapa anónimo para la web informativa: sin códigos, descripciones ni fotos (RN-07).</summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/publico/mapa")]
public class MapaPublicoController(ConsultaIndicadores consulta) : ControllerBase
{
    // GET /api/v1/publico/mapa?categoria=bache&desde=2026-10-01
    [HttpGet]
    [EnableRateLimiting(Limites.Consulta)]
    public async Task<IActionResult> Mapa([FromQuery] FiltroIndicadores filtro, CancellationToken cancelacion) =>
        Ok(await consulta.MapaAsync(filtro, unidadId: null, publico: true, cancelacion));
}
