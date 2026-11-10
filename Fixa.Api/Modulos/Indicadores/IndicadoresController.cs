using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Auth;
using Fixa.Api.Modulos.Casos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Indicadores;

/// <summary>
/// Indicadores y mapa del panel. El supervisor ve solo su unidad; el administrador, todas.
/// </summary>
[ApiController]
[Authorize(Roles = $"{Roles.Supervisor},{Roles.Administrador}")]
[Route("api/v1/panel")]
public class IndicadoresController(FixaDbContext db, ConsultaIndicadores consulta, ControlVencimientos vencimientos) : ControllerBase
{
    // GET /api/v1/panel/indicadores?desde=2026-10-01&hasta=2026-10-31&distritoId=1&categoria=bache
    [HttpGet("indicadores")]
    public async Task<IActionResult> Indicadores([FromQuery] FiltroIndicadores filtro, CancellationToken cancelacion)
    {
        await vencimientos.MarcarAsync(cancelacion);
        return Ok(await consulta.ResumenAsync(filtro, await AlcanceAsync(cancelacion), cancelacion));
    }

    // GET /api/v1/panel/mapa?categoria=alumbrado  (GeoJSON para Leaflet)
    [HttpGet("mapa")]
    public async Task<IActionResult> Mapa([FromQuery] FiltroIndicadores filtro, CancellationToken cancelacion) =>
        Ok(await consulta.MapaAsync(filtro, await AlcanceAsync(cancelacion), publico: false, cancelacion));

    /// <summary>null = todas las unidades (administrador). Un supervisor sin unidad no ve nada.</summary>
    private async Task<long?> AlcanceAsync(CancellationToken cancelacion)
    {
        if (User.IsInRole(Roles.Administrador))
            return null;
        var id = ServicioAutenticacion.Instancia.IdUsuario(User);
        return await db.Usuarios.Where(u => u.Id == id).Select(u => u.UnidadId).FirstOrDefaultAsync(cancelacion)
            ?? throw new NoEncontradoException("Tu cuenta no tiene una unidad asignada.");
    }
}
