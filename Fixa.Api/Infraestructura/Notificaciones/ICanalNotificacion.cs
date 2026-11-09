using Fixa.Api.Dominio;

namespace Fixa.Api.Infraestructura.Notificaciones;

public record Aviso(string Titulo, string Mensaje);

/// <summary>
/// Patrón Adapter — Objetivo: la interfaz común para avisar al vecino. Firebase (push) y
/// Mailgun (correo) tienen APIs distintas; cada uno se adapta a este mismo contrato.
/// </summary>
public interface ICanalNotificacion
{
    CanalNotificacion Canal { get; }

    /// <summary>Devuelve false si el aviso no se pudo entregar; nunca corta el flujo del caso.</summary>
    Task<bool> EnviarAsync(string destino, Aviso aviso, CancellationToken cancelacion);
}
