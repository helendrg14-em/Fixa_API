using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Microsoft.IdentityModel.Tokens;

namespace Fixa.Api.Modulos.Auth;

/// <summary>
/// Patrón Singleton: toda la autenticación de funcionarios pasa por un único punto de acceso,
/// así las reglas de acceso no se repiten en el login, en la validación del token ni en los módulos.
///
/// La instancia es compartida por todas las peticiones, por eso no guarda al usuario en un
/// atributo: cada método recibe el usuario que necesita, y un funcionario nunca recibe la
/// sesión de otro.
/// </summary>
public sealed class ServicioAutenticacion
{
    // Lazy crea la instancia la primera vez que se usa y es seguro con varias peticiones a la vez.
    private static readonly Lazy<ServicioAutenticacion> instancia = new(() => new ServicioAutenticacion());

    public static ServicioAutenticacion Instancia => instancia.Value;

    public static readonly TimeSpan DuracionSesion = TimeSpan.FromMinutes(15); // RNF-06

    // Hash de relleno: si el correo no existe se verifica igual, así la respuesta tarda lo mismo
    // y no revela qué correos están registrados.
    private static readonly string hashDeRelleno = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), 12);

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

    /// <summary>
    /// Verifica la contraseña contra el hash bcrypt que guardó Laravel. Con usuario null igual
    /// gasta el mismo tiempo, para no revelar si el correo existe.
    /// </summary>
    public bool Credenciales(Usuario? usuario, string password)
    {
        var valida = BCrypt.Net.BCrypt.Verify(password, usuario?.PasswordHash ?? hashDeRelleno);
        return usuario is not null && valida;
    }

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
