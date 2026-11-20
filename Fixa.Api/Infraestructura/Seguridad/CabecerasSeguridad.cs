namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Cabeceras de seguridad para todas las respuestas (OWASP Secure Headers). La API solo
/// devuelve JSON, así que se prohíbe incrustarla y ejecutar contenido; las páginas de pruebas
/// /scalar y /swagger (solo en desarrollo) quedan fuera de esa política porque necesitan sus scripts.
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
            var esPaginaDePruebas = contexto.Request.Path.StartsWithSegments("/scalar")
                                    || contexto.Request.Path.StartsWithSegments("/swagger");
            if (!esPaginaDePruebas)
                cabeceras.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
            await siguiente();
        });
}
