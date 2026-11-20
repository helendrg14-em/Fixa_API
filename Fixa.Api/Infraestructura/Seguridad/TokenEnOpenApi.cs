using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Declara el token JWT en el documento OpenAPI, así Swagger UI y Scalar muestran el botón
/// para pegar el token y probar los endpoints del panel.
/// </summary>
public sealed class TokenEnOpenApi : IOpenApiDocumentTransformer
{
    private const string Esquema = "Bearer";

    public Task TransformAsync(OpenApiDocument documento, OpenApiDocumentTransformerContext contexto, CancellationToken cancelacion)
    {
        documento.Components ??= new OpenApiComponents();
        documento.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        documento.Components.SecuritySchemes[Esquema] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Token de POST /api/v1/panel/auth/login.",
        };
        documento.Security ??= [];
        documento.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(Esquema, documento)] = [],
        });
        return Task.CompletedTask;
    }
}
