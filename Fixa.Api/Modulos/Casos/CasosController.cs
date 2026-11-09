using System.ComponentModel.DataAnnotations;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

public record SolicitudEstado([Required, StringLength(25)] string Estado, [StringLength(1000)] string? Motivo);
public record SolicitudPrioridad([Required, StringLength(10)] string Prioridad, [Required, StringLength(1000, MinimumLength = 10)] string Justificacion);
public record SolicitudProgramacion([Range(1, long.MaxValue)] long CuadrillaId, [Required] DateOnly? Fecha, [StringLength(200)] string? MotivoReprogramacion);
public record SolicitudDerivacion([Range(1, long.MaxValue)] long EntidadId, [Required, StringLength(1000)] string Motivo);
public record SolicitudValidacion(bool Aprobar, [StringLength(1000)] string? Observacion);

/// <summary>
/// Gestión de casos del supervisor (RF-04 a RF-13). Cada acción delega en la fachada
/// GestorCasos; el controlador solo traduce la petición y la respuesta.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Supervisor)]
[Route("api/v1/panel/casos")]
public class CasosController(FixaDbContext db, GestorCasos gestor) : ControllerBase
{
    private const int TamanoPagina = 20;

    private long SupervisorId => ServicioAutenticacion.Instancia.IdUsuario(User);

    private Task<long?> UnidadAsync(CancellationToken cancelacion) =>
        db.Usuarios.Where(u => u.Id == SupervisorId).Select(u => u.UnidadId).FirstOrDefaultAsync(cancelacion);

    // GET /api/v1/panel/casos?estado=en_revision&pagina=1
    [HttpGet]
    public async Task<IActionResult> Bandeja(string? estado, [Range(1, 1000)] int pagina = 1, CancellationToken cancelacion = default)
    {
        var unidad = await UnidadAsync(cancelacion);
        var consulta = db.Casos.AsNoTracking().Where(c => c.UnidadId == unidad && unidad != null);
        if (estado is not null)
        {
            if (!ValoresBd.TryDesdeBd<EstadoCaso>(estado, out var filtro))
                throw new ReglaNegocioException("El estado no existe.");
            consulta = consulta.Where(c => c.Estado == filtro.ABd());
        }

        var casos = await consulta
            .OrderByDescending(c => c.Vencido).ThenBy(c => c.FechaObjetivo).ThenBy(c => c.Id)
            .Skip((pagina - 1) * TamanoPagina).Take(TamanoPagina)
            .Select(c => new { c.Id, c.Estado, c.Prioridad, categoria = c.Categoria.Nombre, c.FechaCreacion, c.FechaObjetivo, c.Vencido, reportes = c.Reportes.Count })
            .ToListAsync(cancelacion);
        return Ok(casos);
    }

    // GET /api/v1/panel/casos/5
    [HttpGet("{id:long}")]
    public async Task<IActionResult> Detalle(long id, CancellationToken cancelacion)
    {
        var unidad = await UnidadAsync(cancelacion);
        var caso = await db.Casos.AsNoTracking()
            .Where(c => c.Id == id && c.UnidadId == unidad && unidad != null)
            .Select(c => new
            {
                c.Id, c.Estado, c.Prioridad, c.JustificacionPrioridad, c.FechaCreacion, c.FechaObjetivo, c.FechaCierre, c.Vencido,
                c.RequiereVerificacionCampo,
                categoria = c.Categoria.Nombre,
                // Sin correo, token ni clave del vecino: el panel no necesita sus datos de contacto (RNF-05).
                reportes = c.Reportes.Select(r => new
                {
                    r.Codigo, r.TipoIncidencia, r.Descripcion, r.Direccion, r.Referencia, r.NumeroLuminarias, r.EsApoyo, r.FechaRegistro,
                    latitud = r.Ubicacion.Y, longitud = r.Ubicacion.X,
                    fotos = r.FotosReporte.Select(f => f.Url),
                }),
                historial = c.EventosHistorial.OrderBy(e => e.FechaHora).Select(e => new { e.FechaHora, e.Tipo, e.EstadoAnterior, e.EstadoNuevo, e.Detalle, e.Motivo }),
                intervenciones = c.Intervenciones.Select(i => new { i.Id, i.Fecha, i.TrabajoRealizado, i.EstadoValidacion, evidencias = i.Evidencias.Select(e => new { e.Tipo, e.Url }) }),
            })
            .FirstOrDefaultAsync(cancelacion);
        return caso is null ? throw new NoEncontradoException("El caso no existe o no es de tu unidad.") : Ok(caso);
    }

    // PATCH /api/v1/panel/casos/5/estado
    [HttpPatch("{id:long}/estado")]
    public async Task<IActionResult> CambiarEstado(long id, SolicitudEstado solicitud, CancellationToken cancelacion)
    {
        if (!ValoresBd.TryDesdeBd<EstadoCaso>(solicitud.Estado, out var estado))
            throw new ReglaNegocioException("El estado no existe.");
        await gestor.CambiarEstadoAsync(id, SupervisorId, estado, solicitud.Motivo, cancelacion);
        return NoContent();
    }

    // PATCH /api/v1/panel/casos/5/prioridad
    [HttpPatch("{id:long}/prioridad")]
    public async Task<IActionResult> ClasificarPrioridad(long id, SolicitudPrioridad solicitud, CancellationToken cancelacion)
    {
        if (!ValoresBd.TryDesdeBd<Prioridad>(solicitud.Prioridad, out var prioridad))
            throw new ReglaNegocioException("La prioridad debe ser critica, alta, media o baja.");
        await gestor.ClasificarPrioridadAsync(id, SupervisorId, prioridad, solicitud.Justificacion.Trim(), cancelacion);
        return NoContent();
    }

    // POST /api/v1/panel/casos/5/programaciones
    [HttpPost("{id:long}/programaciones")]
    public async Task<IActionResult> Programar(long id, SolicitudProgramacion solicitud, CancellationToken cancelacion)
    {
        await gestor.ProgramarAsync(id, SupervisorId, solicitud.CuadrillaId, solicitud.Fecha!.Value, solicitud.MotivoReprogramacion?.Trim(), cancelacion);
        return NoContent();
    }

    // POST /api/v1/panel/casos/5/derivaciones
    [HttpPost("{id:long}/derivaciones")]
    public async Task<IActionResult> Derivar(long id, SolicitudDerivacion solicitud, CancellationToken cancelacion)
    {
        await gestor.DerivarAsync(id, SupervisorId, solicitud.EntidadId, solicitud.Motivo.Trim(), cancelacion);
        return NoContent();
    }

    // POST /api/v1/panel/casos/intervenciones/7/validacion
    [HttpPost("intervenciones/{id:long}/validacion")]
    public async Task<IActionResult> ValidarIntervencion(long id, SolicitudValidacion solicitud, CancellationToken cancelacion)
    {
        await gestor.ValidarIntervencionAsync(id, SupervisorId, solicitud.Aprobar, solicitud.Observacion?.Trim(), cancelacion);
        return NoContent();
    }
}
