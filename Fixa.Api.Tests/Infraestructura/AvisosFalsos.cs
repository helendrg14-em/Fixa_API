using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Notificaciones;

namespace Fixa.Api.Tests.Infraestructura;

/// <summary>Reemplaza a Firebase y Mailgun en las pruebas: acepta el aviso sin enviarlo.</summary>
public sealed class AvisosFalsos : ICanalNotificacion
{
    public CanalNotificacion Canal => CanalNotificacion.Push;

    public Task<bool> EnviarAsync(string destino, Aviso aviso, CancellationToken cancelacion) => Task.FromResult(true);
}
