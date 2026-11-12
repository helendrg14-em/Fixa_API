using Microsoft.AspNetCore.HttpOverrides;

namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// En Render la API está detrás de un proxy: sin esto, todas las peticiones llegarían con la IP
/// del proxy y los límites por IP (Limites) bloquearían a todos los vecinos a la vez.
///
/// Se activa solo con Proxy:DetrasDeRender=true (variable de entorno Proxy__DetrasDeRender en
/// Render). Fuera de Render queda apagado, así nadie puede falsificar su IP enviando la cabecera.
/// ForwardLimit = 1: solo se cree el último valor de X-Forwarded-For, el que agrega el proxy;
/// lo que el cliente haya escrito antes en esa cabecera se ignora.
/// </summary>
public static class ProxyDeConfianza
{
    private const string Interruptor = "Proxy:DetrasDeRender";

    public static IServiceCollection AddProxyDeConfianza(this IServiceCollection servicios, IConfiguration configuracion)
    {
        if (!configuracion.GetValue<bool>(Interruptor))
            return servicios;

        return servicios.Configure<ForwardedHeadersOptions>(opciones =>
        {
            opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opciones.ForwardLimit = 1;
            // Render no publica IPs fijas para su proxy; la API solo es accesible a través de él.
            opciones.KnownIPNetworks.Clear();
            opciones.KnownProxies.Clear();
        });
    }

    public static IApplicationBuilder UseProxyDeConfianza(this WebApplication app) =>
        app.Configuration.GetValue<bool>(Interruptor) ? app.UseForwardedHeaders() : app;
}
