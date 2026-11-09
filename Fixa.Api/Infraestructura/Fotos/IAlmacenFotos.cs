namespace Fixa.Api.Infraestructura.Fotos;

public record FotoGuardada(string Url, string PublicId);

/// <summary>
/// Patrón Adapter — Objetivo: la interfaz que usa FIXA para guardar fotos. Los módulos solo
/// conocen esta interfaz; cambiar de proveedor (Cloudinary hoy) es escribir otro adaptador.
/// </summary>
public interface IAlmacenFotos
{
    Task<FotoGuardada> GuardarAsync(Stream contenido, CancellationToken cancelacion);
}
