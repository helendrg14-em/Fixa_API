using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Fixa.Api.Tests.Infraestructura;

/// <summary>
/// El servidor de pruebas no tiene IP de cliente, así que todas las peticiones caerían en el
/// mismo límite de peticiones. Cada petición recibe una IP al azar, salvo que la prueba fije
/// una con la cabecera X-Ip-Prueba (así se prueba el propio límite).
/// </summary>
public sealed class IpDePrueba : IStartupFilter
{
    public const string Cabecera = "X-Ip-Prueba";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> siguiente) => app =>
    {
        app.Use((contexto, continuar) =>
        {
            contexto.Connection.RemoteIpAddress = contexto.Request.Headers.TryGetValue(Cabecera, out var ip)
                ? IPAddress.Parse(ip.ToString())
                : new IPAddress(Random.Shared.Next(0x0A000001, 0x0AFFFFFE));
            return continuar(contexto);
        });
        siguiente(app);
    };
}
