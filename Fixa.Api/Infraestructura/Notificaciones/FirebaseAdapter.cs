using Fixa.Api.Dominio;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace Fixa.Api.Infraestructura.Notificaciones;

/// <summary>
/// Patrón Adapter — Adaptador: envía el aviso como notificación push con Firebase Cloud
/// Messaging (RF-18) al token del celular que hizo el reporte. La cuenta de servicio va en
/// Firebase:CredencialesJson (el contenido del JSON, nunca en el repositorio).
/// </summary>
public sealed class FirebaseAdapter(IConfiguration configuracion, ILogger<FirebaseAdapter> log) : ICanalNotificacion
{
    private readonly Lazy<FirebaseMessaging?> mensajeria = new(() =>
    {
        var credenciales = configuracion["Firebase:CredencialesJson"];
        if (string.IsNullOrWhiteSpace(credenciales))
            return null;
        var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
        {
            Credential = CredentialFactory.FromJson<ServiceAccountCredential>(credenciales).ToGoogleCredential(),
        });
        return FirebaseMessaging.GetMessaging(app);
    });

    public CanalNotificacion Canal => CanalNotificacion.Push;

    public async Task<bool> EnviarAsync(string destino, Aviso aviso, CancellationToken cancelacion)
    {
        if (mensajeria.Value is null)
        {
            log.LogWarning("Firebase no está configurado; el aviso push no se envió.");
            return false;
        }
        try
        {
            // La app Flutter entrega el token de registro de FCM; el SDK lo marca como antiguo, pero sigue vigente.
#pragma warning disable CS0618
            await mensajeria.Value.SendAsync(new Message
            {
                Token = destino,
#pragma warning restore CS0618
                Notification = new Notification { Title = aviso.Titulo, Body = aviso.Mensaje },
            }, cancelacion);
            return true;
        }
        catch (FirebaseMessagingException error)
        {
            log.LogWarning("Firebase rechazó el aviso push: {Codigo}", error.MessagingErrorCode);
            return false;
        }
    }
}
