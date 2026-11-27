using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Dtos;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Notificaciones;
using Fixa.Api.Modulos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Notificaciones;

/// <summary>
/// Avisos enviados a los vecinos (push y correo). El supervisor ve los de su unidad y el
/// administrador los de todas. No muestra el correo ni el token del vecino (RNF-05).
/// </summary>
[ApiController]
[Authorize(Roles = $"{Roles.Supervisor},{Roles.Administrador}")]
[Route("api/v1/panel/notificaciones")]
[Produces("application/json")]
public sealed class NotificacionesController(FixaDbContext db, ServicioNotificaciones servicio) : ControllerBase
{
    private const int Maximo = 200;

    // GET /api/v1/panel/notificaciones?estadoEntrega=no_entregada&casoId=5
    [HttpGet]
    [EndpointSummary("Listar los avisos enviados")]
    [ProducesResponseType<List<NotificacionSalida>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<List<NotificacionSalida>> Listar(string? estadoEntrega, long? casoId, CancellationToken cancelacion)
    {
        var consulta = await VisiblesAsync(cancelacion);
        if (estadoEntrega is not null)
        {
            if (!ValoresBd.TryDesdeBd<EstadoEntrega>(estadoEntrega, out var estado))
                throw new ReglaNegocioException("El estado de entrega debe ser pendiente, enviada o no_entregada.");
            var estadoBd = estado.ABd();
            consulta = consulta.Where(n => n.EstadoEntrega == estadoBd);
        }
        if (casoId is long caso)
            consulta = consulta.Where(n => n.Reporte.CasoId == caso);

        return await consulta.OrderByDescending(n => n.Id).Take(Maximo).Select(Salida).ToListAsync(cancelacion);
    }

    // GET /api/v1/panel/notificaciones/5
    [HttpGet("{id:long}")]
    [EndpointSummary("Consultar un aviso por ID")]
    [ProducesResponseType<NotificacionSalida>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NotificacionSalida> ObtenerPorId(long id, CancellationToken cancelacion) =>
        await (await VisiblesAsync(cancelacion)).Where(n => n.Id == id).Select(Salida).FirstOrDefaultAsync(cancelacion)
            ?? throw NoExiste();

    // POST /api/v1/panel/notificaciones/5/reintento
    [HttpPost("{id:long}/reintento")]
    [EndpointSummary("Reintentar un aviso que no se entregó")]
    [ProducesResponseType<NotificacionSalida>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<NotificacionSalida> Reintentar(long id, CancellationToken cancelacion)
    {
        if (!await (await VisiblesAsync(cancelacion)).AnyAsync(n => n.Id == id, cancelacion))
            throw NoExiste();

        var notificacion = await db.Notificaciones
            .Include(n => n.Reporte)
            .Include(n => n.Evento).ThenInclude(e => e.Caso)
            .FirstAsync(n => n.Id == id, cancelacion);
        await servicio.ReintentarAsync(notificacion, cancelacion);
        return await ObtenerPorId(id, cancelacion);
    }

    private static readonly System.Linq.Expressions.Expression<Func<Notificacion, NotificacionSalida>> Salida = n =>
        new NotificacionSalida(n.Id, n.Reporte.CasoId, n.Reporte.Codigo, n.Evento.EstadoNuevo ?? n.Evento.Caso.Estado,
            n.Canal, n.EstadoEntrega, n.Evento.FechaHora, n.FechaEnvio);

    private async Task<IQueryable<Notificacion>> VisiblesAsync(CancellationToken cancelacion)
    {
        var consulta = db.Notificaciones.AsNoTracking();
        if (User.IsInRole(Roles.Administrador))
            return consulta;

        var id = ServicioAutenticacion.Instancia.IdUsuario(User);
        var unidad = await db.Usuarios.Where(u => u.Id == id).Select(u => u.UnidadId).FirstOrDefaultAsync(cancelacion);
        return consulta.Where(n => unidad != null && n.Reporte.Caso.UnidadId == unidad);
    }

    private static NoEncontradoException NoExiste() => new("El aviso no existe o no es de tu unidad.");
}
