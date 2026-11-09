using System.Net.Http.Headers;
using System.Text;
using Fixa.Api.Dominio;

namespace Fixa.Api.Infraestructura.Notificaciones;

/// <summary>
/// Patrón Adapter — Adaptador: envía el aviso por correo con la API HTTP de Mailgun (RF-22),
/// solo a los vecinos que dejaron su correo. Usa Mailgun:Dominio, Mailgun:ApiKey,
/// Mailgun:Remitente y, opcional, Mailgun:UrlBase (la región de la cuenta).
/// </summary>
public sealed class MailgunAdapter(HttpClient http, IConfiguration configuracion, ILogger<MailgunAdapter> log) : ICanalNotificacion
{
    public CanalNotificacion Canal => CanalNotificacion.Correo;

    public async Task<bool> EnviarAsync(string destino, Aviso aviso, CancellationToken cancelacion)
    {
        var dominio = configuracion["Mailgun:Dominio"];
        var clave = configuracion["Mailgun:ApiKey"];
        var remitente = configuracion["Mailgun:Remitente"];
        if (string.IsNullOrWhiteSpace(dominio) || string.IsNullOrWhiteSpace(clave) || string.IsNullOrWhiteSpace(remitente))
        {
            log.LogWarning("Mailgun no está configurado; el aviso por correo no se envió.");
            return false;
        }

        var urlBase = configuracion["Mailgun:UrlBase"] ?? "https://api.mailgun.net";
        using var solicitud = new HttpRequestMessage(HttpMethod.Post, $"{urlBase}/v3/{Uri.EscapeDataString(dominio)}/messages")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["from"] = remitente,
                ["to"] = destino,
                ["subject"] = aviso.Titulo,
                ["text"] = aviso.Mensaje,
            }),
        };
        solicitud.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{clave}")));

        using var respuesta = await http.SendAsync(solicitud, cancelacion);
        if (!respuesta.IsSuccessStatusCode)
            log.LogWarning("Mailgun rechazó el correo: {Estado}", (int)respuesta.StatusCode);
        return respuesta.IsSuccessStatusCode;
    }
}
