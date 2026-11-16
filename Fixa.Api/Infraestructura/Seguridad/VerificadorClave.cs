namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Verifica contraseñas y claves de consulta contra su hash bcrypt (el mismo de Laravel).
/// Si no hay hash (el correo o el código no existe) igual verifica contra uno de relleno,
/// así la respuesta tarda lo mismo y no revela qué cuentas o códigos existen.
/// </summary>
public static class VerificadorClave
{
    public const int Costo = 12; // contraseñas de funcionarios: BCRYPT_ROUNDS de Laravel

    // Claves de consulta del vecino: se verifican en cada seguimiento, y con costo 12 el servidor
    // gratuito tardaba de 2 a 4 segundos. 10 es el mínimo que recomienda OWASP y es 4 veces más
    // rápido; la fuerza bruta la frena además el límite de intentos por IP.
    public const int CostoClaveVecino = 10;

    private static readonly Dictionary<int, string> hashesDeRelleno = new[] { Costo, CostoClaveVecino }
        .ToDictionary(costo => costo, costo => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString(), costo));

    public static string Hash(string valor, int costo = Costo) => BCrypt.Net.BCrypt.HashPassword(valor, costo);

    public static bool Verificar(string valor, string? hash, int costo = Costo)
    {
        var valida = BCrypt.Net.BCrypt.Verify(valor, hash ?? hashesDeRelleno[costo]);
        return hash is not null && valida;
    }
}
