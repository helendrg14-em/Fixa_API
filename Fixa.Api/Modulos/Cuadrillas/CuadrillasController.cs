using System.ComponentModel.DataAnnotations;
using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Modulos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Cuadrillas;

/// <summary>Datos para crear o editar una cuadrilla. La unidad solo la elige el administrador.</summary>
public sealed record CuadrillaEntrada(
    [Required(ErrorMessage = "El nombre es obligatorio."), StringLength(60, MinimumLength = 3, ErrorMessage = "El nombre debe tener entre 3 y 60 caracteres.")] string Nombre,
    [Required(ErrorMessage = "El turno es obligatorio."), StringLength(10)] string Turno,
    [Range(1, long.MaxValue, ErrorMessage = "La unidad no es válida.")] long? UnidadId);

public sealed record CuadrillaSalida(long Id, string Nombre, string Turno, long UnidadId, string Unidad, int Tecnicos);

public sealed record TecnicoSalida(long Id, string Nombres, string Apellidos, string Correo, string? Especialidad, bool Activo);

/// <summary>
/// Cuadrillas de cada unidad responsable (RF-07: a ellas se programan los casos). El supervisor
/// administra las de su unidad y el administrador las de todas. No se borra una cuadrilla que ya
/// tiene técnicos o programaciones: el historial de los casos la necesita.
/// </summary>
[ApiController]
[Authorize(Roles = $"{Roles.Supervisor},{Roles.Administrador}")]
[Route("api/v1/panel/cuadrillas")]
[Produces("application/json")]
public sealed class CuadrillasController(FixaDbContext db) : ControllerBase
{
    // Los mismos valores que guarda el panel de Laravel.
    private static readonly string[] Turnos = ["Mañana", "Tarde"];

    private long UsuarioId => ServicioAutenticacion.Instancia.IdUsuario(User);
    private bool EsAdministrador => User.IsInRole(Roles.Administrador);

    // GET /api/v1/panel/cuadrillas
    [HttpGet]
    [EndpointSummary("Listar las cuadrillas")]
    [ProducesResponseType<List<CuadrillaSalida>>(StatusCodes.Status200OK)]
    public async Task<List<CuadrillaSalida>> Listar(CancellationToken cancelacion) =>
        await (await VisiblesAsync(cancelacion))
            .OrderBy(c => c.UnidadId).ThenBy(c => c.Nombre)
            .Select(Salida)
            .ToListAsync(cancelacion);

    // GET /api/v1/panel/cuadrillas/5
    [HttpGet("{id:long}")]
    [EndpointSummary("Consultar una cuadrilla por ID")]
    [ProducesResponseType<CuadrillaSalida>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CuadrillaSalida> ObtenerPorId(long id, CancellationToken cancelacion) =>
        await (await VisiblesAsync(cancelacion)).Where(c => c.Id == id).Select(Salida).FirstOrDefaultAsync(cancelacion)
            ?? throw NoExiste();

    // GET /api/v1/panel/cuadrillas/5/tecnicos
    [HttpGet("{id:long}/tecnicos")]
    [EndpointSummary("Listar los técnicos de una cuadrilla")]
    [ProducesResponseType<List<TecnicoSalida>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<List<TecnicoSalida>> Tecnicos(long id, CancellationToken cancelacion)
    {
        if (!await (await VisiblesAsync(cancelacion)).AnyAsync(c => c.Id == id, cancelacion))
            throw NoExiste();

        var tecnico = TipoUsuario.Tecnico.ABd();
        return await db.Usuarios.AsNoTracking()
            .Where(u => u.CuadrillaId == id && u.Tipo == tecnico)
            .OrderBy(u => u.Apellidos)
            .Select(u => new TecnicoSalida(u.Id, u.Nombres, u.Apellidos, u.Correo, u.Especialidad, u.Activo))
            .ToListAsync(cancelacion);
    }

    // POST /api/v1/panel/cuadrillas
    [HttpPost]
    [EndpointSummary("Crear una cuadrilla")]
    [ProducesResponseType<CuadrillaSalida>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Crear(CuadrillaEntrada entrada, CancellationToken cancelacion)
    {
        var unidadId = await UnidadParaCrearAsync(entrada.UnidadId, cancelacion);
        var nombre = entrada.Nombre.Trim();
        var turno = TurnoValido(entrada.Turno);
        await ExigirNombreLibreAsync(unidadId, nombre, idActual: null, cancelacion);

        var cuadrilla = new Cuadrilla { UnidadId = unidadId, Nombre = nombre, Turno = turno };
        db.Cuadrillas.Add(cuadrilla);
        await db.SaveChangesAsync(cancelacion);

        return CreatedAtAction(nameof(ObtenerPorId), new { id = cuadrilla.Id }, await ObtenerPorId(cuadrilla.Id, cancelacion));
    }

    // PUT /api/v1/panel/cuadrillas/5
    [HttpPut("{id:long}")]
    [EndpointSummary("Editar el nombre y el turno de una cuadrilla")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Editar(long id, CuadrillaEntrada entrada, CancellationToken cancelacion)
    {
        var cuadrilla = await (await VisiblesAsync(cancelacion)).AsTracking().FirstOrDefaultAsync(c => c.Id == id, cancelacion)
            ?? throw NoExiste();
        var nombre = entrada.Nombre.Trim();
        var turno = TurnoValido(entrada.Turno);
        // La unidad no cambia: sus técnicos y programaciones son de esa unidad.
        await ExigirNombreLibreAsync(cuadrilla.UnidadId, nombre, cuadrilla.Id, cancelacion);

        cuadrilla.Nombre = nombre;
        cuadrilla.Turno = turno;
        await db.SaveChangesAsync(cancelacion);
        return NoContent();
    }

    // DELETE /api/v1/panel/cuadrillas/5
    [HttpDelete("{id:long}")]
    [EndpointSummary("Eliminar una cuadrilla sin técnicos ni programaciones")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Eliminar(long id, CancellationToken cancelacion)
    {
        var cuadrilla = await (await VisiblesAsync(cancelacion)).AsTracking().FirstOrDefaultAsync(c => c.Id == id, cancelacion)
            ?? throw NoExiste();
        if (await db.Usuarios.AnyAsync(u => u.CuadrillaId == id, cancelacion))
            throw new ReglaNegocioException("La cuadrilla tiene técnicos asignados; muévelos a otra cuadrilla antes de eliminarla.");
        if (await db.Programaciones.AnyAsync(p => p.CuadrillaId == id, cancelacion))
            throw new ReglaNegocioException("La cuadrilla ya tiene casos programados y forma parte del historial; no se puede eliminar.");

        db.Cuadrillas.Remove(cuadrilla);
        await db.SaveChangesAsync(cancelacion);
        return NoContent();
    }

    private static readonly string TipoTecnico = TipoUsuario.Tecnico.ABd();

    private static readonly System.Linq.Expressions.Expression<Func<Cuadrilla, CuadrillaSalida>> Salida = c =>
        new CuadrillaSalida(c.Id, c.Nombre, c.Turno, c.UnidadId, c.Unidad.Nombre,
            c.Usuarios.Count(u => u.Tipo == TipoTecnico));

    /// <summary>Las cuadrillas que puede ver y editar quien hace la petición.</summary>
    private async Task<IQueryable<Cuadrilla>> VisiblesAsync(CancellationToken cancelacion)
    {
        if (EsAdministrador)
            return db.Cuadrillas.AsNoTracking();
        var unidad = await UnidadDelSupervisorAsync(cancelacion);
        return db.Cuadrillas.AsNoTracking().Where(c => c.UnidadId == unidad);
    }

    private async Task<long> UnidadParaCrearAsync(long? pedida, CancellationToken cancelacion)
    {
        if (!EsAdministrador)
            return await UnidadDelSupervisorAsync(cancelacion); // el supervisor solo crea en su unidad
        if (pedida is not long unidadId)
            throw new ReglaNegocioException("Indica la unidad de la cuadrilla.");
        if (!await db.UnidadesResponsables.AnyAsync(u => u.Id == unidadId, cancelacion))
            throw new ReglaNegocioException("La unidad indicada no existe.");
        return unidadId;
    }

    private async Task<long> UnidadDelSupervisorAsync(CancellationToken cancelacion) =>
        await db.Usuarios.Where(u => u.Id == UsuarioId).Select(u => u.UnidadId).FirstOrDefaultAsync(cancelacion)
            ?? throw new ReglaNegocioException("Tu cuenta no tiene una unidad asignada.");

    private async Task ExigirNombreLibreAsync(long unidadId, string nombre, long? idActual, CancellationToken cancelacion)
    {
        if (await db.Cuadrillas.AnyAsync(c => c.UnidadId == unidadId && c.Nombre == nombre && c.Id != idActual, cancelacion))
            throw new ReglaNegocioException("Ya existe una cuadrilla con ese nombre en la unidad.");
    }

    private static string TurnoValido(string turno) =>
        Turnos.FirstOrDefault(t => string.Equals(t, turno.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new ReglaNegocioException("El turno debe ser Mañana o Tarde.");

    private static NoEncontradoException NoExiste() => new("La cuadrilla no existe o no es de tu unidad.");
}
