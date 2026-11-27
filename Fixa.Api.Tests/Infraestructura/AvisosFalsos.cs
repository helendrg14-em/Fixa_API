using System.Collections.Concurrent;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Notificaciones;

namespace Fixa.Api.Tests.Infraestructura;

/// <summary>
/// Reemplaza a Firebase y Mailgun en las pruebas: no envía nada, guarda lo que se "envió" y
/// falla a propósito con los tokens que contienen "vencido", para probar el reintento.
/// </summary>
public sealed class AvisosFalsos : ICanalNotificacion
{
    public ConcurrentQueue<(string Destino, Aviso Aviso)> Enviados { get; } = new();

    public CanalNotificacion Canal => CanalNotificacion.Push;

    public Task<bool> EnviarAsync(string destino, Aviso aviso, CancellationToken cancelacion)
    {
        Enviados.Enqueue((destino, aviso));
        return Task.FromResult(!destino.Contains("vencido", StringComparison.Ordinal));
    }
}
