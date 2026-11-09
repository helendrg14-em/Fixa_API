using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Auth;

public record SolicitudLogin(
    [Required, EmailAddress, StringLength(120)] string Correo,
    [Required, StringLength(128)] string Password);

/// <summary>Login de funcionarios (R01). Usa el Singleton ServicioAutenticacion.</summary>
[ApiController]
[Route("api/v1/panel/auth")]
public class AuthController(FixaDbContext db) : ControllerBase
{
    // POST /api/v1/panel/auth/login
    [AllowAnonymous]
    [EnableRateLimiting(Limites.Login)]
    [HttpPost("login")]
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
        return Ok(new { token, expira, tipo = usuario.Tipo, nombre = $"{usuario.Nombres} {usuario.Apellidos}" });
    }

    // GET /api/v1/panel/auth/yo
    [HttpGet("yo")]
    public IActionResult Yo() => Ok(new
    {
        id = ServicioAutenticacion.Instancia.IdUsuario(User),
        nombre = User.Identity?.Name,
        tipo = User.FindFirstValue(ClaimTypes.Role),
    });
}
