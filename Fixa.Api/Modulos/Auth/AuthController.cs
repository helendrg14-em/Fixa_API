using Fixa.Api.Dtos;
using System.Security.Claims;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Auth;

/// <summary>Login de funcionarios (R01). Usa el Singleton ServicioAutenticacion.</summary>
[ApiController]
[Route("api/v1/panel/auth")]
public class AuthController(FixaDbContext db) : ControllerBase
{
    // POST /api/v1/panel/auth/login
    [AllowAnonymous]
    [EnableRateLimiting(Limites.Login)]
    [HttpPost("login")]
    [ProducesResponseType<RespuestaLogin>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(SolicitudLogin solicitud)
    {
        var auth = ServicioAutenticacion.Instancia;
        var correo = auth.NormalizarCorreo(solicitud.Correo);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);

        // Mismo mensaje en todos los casos: no se revela si el correo existe ni por qué falló.
        if (!auth.Credenciales(usuario, solicitud.Password) || !auth.PuedeEntrar(usuario!))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Correo o contraseña incorrectos, o cuenta sin acceso.");

        usuario!.UltimoAcceso = HoraBolivia.Ahora();
        await db.SaveChangesAsync();

        var (token, expira) = auth.CrearToken(usuario);
        return Ok(new RespuestaLogin(token, expira, usuario.Rol, $"{usuario.Nombres} {usuario.Apellidos}"));
    }

    // GET /api/v1/panel/auth/yo
    [HttpGet("yo")]
    public ActionResult<UsuarioActual> Yo() => new UsuarioActual(
        ServicioAutenticacion.Instancia.IdUsuario(User),
        User.Identity?.Name,
        User.FindFirstValue(ClaimTypes.Role));
}
