using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Infraestructura.Notificaciones;

namespace Fixa.Api.Infraestructura;

/// <summary>Registra los adaptadores de servicios externos en un solo lugar.</summary>
public static class ServiciosExternos
{
    public static IServiceCollection AddServiciosExternos(this IServiceCollection servicios)
    {
        servicios.AddSingleton<IAlmacenFotos, CloudinaryAdapter>();
        servicios.AddSingleton<ICanalNotificacion, FirebaseAdapter>();
        servicios.AddHttpClient<ICanalNotificacion, MailgunAdapter>(http => http.Timeout = TimeSpan.FromSeconds(10));
        servicios.AddScoped<ServicioNotificaciones>();
        return servicios;
    }
}
