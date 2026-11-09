using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Límites de peticiones por IP. Frenan la fuerza bruta en el login y en la consulta con
/// código y clave, y el abuso del registro público de reportes.
/// </summary>
public static class Limites
{
    public const string Login = "login";
    public const string Consulta = "consulta";
    public const string Registro = "registro";

    public static IServiceCollection AddLimitesDePeticiones(this IServiceCollection servicios) =>
        servicios.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            Agregar(opciones, Login, permitidas: 5, ventana: TimeSpan.FromMinutes(1));
            Agregar(opciones, Consulta, permitidas: 10, ventana: TimeSpan.FromMinutes(1));
            Agregar(opciones, Registro, permitidas: 10, ventana: TimeSpan.FromMinutes(10));
        });

    private static void Agregar(RateLimiterOptions opciones, string nombre, int permitidas, TimeSpan ventana) =>
        opciones.AddPolicy(nombre, contexto => RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = permitidas, Window = ventana, QueueLimit = 0 }));
}
