namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Verifica contraseñas y claves de consulta contra su hash bcrypt (el mismo de Laravel).
/// Si no hay hash (el correo o el código no existe) igual verifica contra uno de relleno,
/// así la respuesta tarda lo mismo y no revela qué cuentas o códigos existen.
/// </summary>
public static class VerificadorClave
{
    public const int Costo = 12; // BCRYPT_ROUNDS de Laravel

    private static readonly string hashDeRelleno = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), Costo);

    public static string Hash(string valor) => BCrypt.Net.BCrypt.HashPassword(valor, Costo);

    public static bool Verificar(string valor, string? hash)
    {
        var valida = BCrypt.Net.BCrypt.Verify(valor, hash ?? hashDeRelleno);
        return hash is not null && valida;
    }
}
