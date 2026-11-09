namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Cabeceras de seguridad para todas las respuestas (OWASP Secure Headers). La API solo
/// devuelve JSON, así que se prohíbe incrustarla y ejecutar contenido; la página de pruebas
/// /scalar (solo en desarrollo) queda fuera de esa política porque necesita sus scripts.
/// </summary>
public static class CabecerasSeguridad
{
    public static IApplicationBuilder UseCabecerasSeguridad(this IApplicationBuilder app) =>
        app.Use(async (contexto, siguiente) =>
        {
            var cabeceras = contexto.Response.Headers;
            cabeceras.XContentTypeOptions = "nosniff";
            cabeceras.XFrameOptions = "DENY";
            cabeceras["Referrer-Policy"] = "no-referrer";
            if (!contexto.Request.Path.StartsWithSegments("/scalar"))
                cabeceras.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            await siguiente();
        });
}
