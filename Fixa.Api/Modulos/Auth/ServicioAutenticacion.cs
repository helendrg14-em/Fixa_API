using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fixa.Api.Dominio.Entidades;
using Microsoft.IdentityModel.Tokens;

namespace Fixa.Api.Modulos.Auth;

/// <summary>
/// Patrón Singleton: toda la autenticación de funcionarios pasa por un único punto de acceso,
/// así las reglas de acceso no se repiten en cada controlador.
/// </summary>
public sealed class ServicioAutenticacion
{
    // Lazy crea la instancia la primera vez que se usa y es seguro con varias peticiones a la vez.
    private static readonly Lazy<ServicioAutenticacion> instancia = new(() => new ServicioAutenticacion());

    public static ServicioAutenticacion Instancia => instancia.Value;

    public static readonly TimeSpan DuracionSesion = TimeSpan.FromMinutes(15); // RNF-06

    private SymmetricSecurityKey? clave;

    // Constructor privado: nadie puede hacer "new ServicioAutenticacion()".
    private ServicioAutenticacion() { }

    /// <summary>Se llama una sola vez al arrancar la API, con la clave de User Secrets.</summary>
    public void Configurar(string claveSecreta)
    {
        if (claveSecreta.Length < 32)
            throw new InvalidOperationException("Jwt:Clave debe tener al menos 32 caracteres.");
        clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta));
    }

    public SymmetricSecurityKey Clave =>
        clave ?? throw new InvalidOperationException("Falta configurar Jwt:Clave.");

    /// <summary>La cuenta se busca por correo, sin espacios y sin distinguir mayúsculas.</summary>
    public string NormalizarCorreo(string correo) => correo.Trim().ToLowerInvariant();

    /// <summary>Compara la contraseña con el hash bcrypt que guardó Laravel.</summary>
    public bool PasswordValida(Usuario usuario, string password) =>
        BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);

    /// <summary>Igual que en Laravel: cuenta activa y perfil con acceso al panel (el operador es versión futura).</summary>
    public bool PuedeEntrar(Usuario usuario) =>
        usuario.Activo && usuario.Tipo != "operador";

    /// <summary>Crea el token JWT que el panel envía en cada petición.</summary>
    public (string Token, DateTime Expira) CrearToken(Usuario usuario)
    {
        var expira = DateTime.UtcNow.Add(DuracionSesion);
        var datos = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.Role, usuario.Tipo),
            new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.Apellidos}"),
        };
        var token = new JwtSecurityToken(
            claims: datos,
            expires: expira,
            signingCredentials: new SigningCredentials(Clave, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
