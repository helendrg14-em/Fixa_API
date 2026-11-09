using Fixa.Api.Infraestructura.Datos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Auth;

public record SolicitudLogin(string Correo, string Password);

/// <summary>Login de funcionarios (R01). Usa el Singleton ServicioAutenticacion.</summary>
[ApiController]
[Route("api/v1/panel/auth")]
public class AuthController(FixaDbContext db) : ControllerBase
{
    private static readonly TimeZoneInfo zonaBolivia = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    // POST /api/v1/panel/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(SolicitudLogin solicitud)
    {
        var auth = ServicioAutenticacion.Instancia;
        var correo = auth.NormalizarCorreo(solicitud.Correo);
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo);

        // Mismo mensaje en todos los casos: no se revela si el correo existe.
        if (usuario is null || !auth.PasswordValida(usuario, solicitud.Password) || !auth.PuedeEntrar(usuario))
            return Unauthorized(new { mensaje = "Correo o contraseña incorrectos, o cuenta sin acceso." });

        // Laravel guarda las fechas en hora de Bolivia.
        usuario.UltimoAcceso = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaBolivia);
        await db.SaveChangesAsync();

        var (token, expira) = auth.CrearToken(usuario);
        return Ok(new { token, expira, tipo = usuario.Tipo, nombre = $"{usuario.Nombres} {usuario.Apellidos}" });
    }

    // GET /api/v1/panel/auth/yo  → prueba de que el token funciona
    [Authorize]
    [HttpGet("yo")]
    public IActionResult Yo() => Ok(new
    {
        id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        nombre = User.Identity?.Name,
        tipo = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value,
    });
}