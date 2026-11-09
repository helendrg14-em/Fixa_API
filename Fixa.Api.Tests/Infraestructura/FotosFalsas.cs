using Fixa.Api.Infraestructura.Fotos;

namespace Fixa.Api.Tests.Infraestructura;

/// <summary>
/// Reemplaza a Cloudinary en las pruebas (gracias al patrón Adapter basta con registrar otra
/// clase): no sube nada, no gasta créditos y no necesita credenciales.
/// </summary>
public sealed class FotosFalsas : IAlmacenFotos
{
    public Task<FotoGuardada> GuardarAsync(Stream contenido, CancellationToken cancelacion) =>
        Task.FromResult(new FotoGuardada($"https://pruebas.fixa/{Guid.NewGuid():N}.webp", "prueba"));
}
