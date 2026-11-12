using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Seguridad;
using Microsoft.IdentityModel.Tokens;

namespace Fixa.Api.Modulos.Auth;

public sealed class ServicioAutenticacion
{
    // Lazy crea la instancia la primera vez que se usa y es seguro con varias peticiones a la vez.
    private static readonly Lazy<ServicioAutenticacion> instancia = new(() => new ServicioAutenticacion());

    public static ServicioAutenticacion Instancia => instancia.Value;

    public static readonly TimeSpan DuracionSesion = TimeSpan.FromMinutes(15); // RNF-06

    private SymmetricSecurityKey? clave;

    private ServicioAutenticacion() { }

    /// <summary>Se llama una sola vez al arrancar la API, con la clave de User Secrets.</summary>
    public void Configurar(string? claveSecreta)
    {
        if (claveSecreta is null || Encoding.UTF8.GetByteCount(claveSecreta) < 32)
            throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres.");
        clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
    }

    public SymmetricSecurityKey Clave =>
        clave ?? throw new InvalidOperationException("Falta configurar Jwt:Clave.");

    public TokenValidationParameters ParametrosValidacion() => new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = Clave,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero, // el token vence exactamente a los 15 minutos
    };

    /// <summary>La cuenta se busca por correo, sin espacios y sin distinguir mayúsculas.</summary>
    public string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    /// <summary>Verifica la contraseña contra el hash que guardó Laravel, en tiempo constante exista o no el usuario.</summary>
    public bool Credenciales(Usuario? usuario, string password) =>
        VerificadorClave.Verificar(password, usuario?.PasswordHash);

    /// <summary>Igual que en Laravel: cuenta activa, contraseña ya registrada y perfil con acceso al panel.</summary>
    public bool PuedeEntrar(Usuario usuario) =>
        usuario.Activo
        && !usuario.DebeRegistrarPassword
        && ValoresBd.TryDesdeBd<TipoUsuario>(usuario.Tipo, out var tipo)
        && tipo != TipoUsuario.Operador;

    /// <summary>Crea el token JWT que el panel envía en cada petición.</summary>
    public (string Token, DateTime Expira) CrearToken(Usuario usuario)
    {
        var expira = DateTime.UtcNow.Add(DuracionSesion);
        var datos = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, usuario.Tipo),
            new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.Apellidos}"),
        };
        var token = new JwtSecurityToken(
            claims: datos,
            expires: expira,
            signingCredentials: new SigningCredentials(Clave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }

    /// <summary>Id del funcionario dueño del token.</summary>
    public long IdUsuario(ClaimsPrincipal principal) =>
        long.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : throw new UnauthorizedAccessException();
}
