using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Infraestructura.Notificaciones;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fixa.Api.Tests.Infraestructura;

/// <summary>Levanta la API completa en memoria, apuntando a la base de pruebas y sin servicios externos.</summary>
public sealed class FabricaApi(string conexion) : WebApplicationFactory<Program>
{
    // Clave solo para firmar tokens dentro de las pruebas; no se usa en ningún entorno real.
    private const string ClaveJwtPruebas = "clave-de-pruebas-fixa-solo-para-tests-0123456789";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Fixa", conexion);
        builder.UseSetting("Jwt:Clave", ClaveJwtPruebas);
        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<IAlmacenFotos>();
            servicios.AddSingleton<IAlmacenFotos, FotosFalsas>();
            servicios.RemoveAll<ICanalNotificacion>();
            servicios.AddSingleton<ICanalNotificacion, AvisosFalsos>();
            servicios.AddSingleton<IStartupFilter, IpDePrueba>();
        });
    }
}
