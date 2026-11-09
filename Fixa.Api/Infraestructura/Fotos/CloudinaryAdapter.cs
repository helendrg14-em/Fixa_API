using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Fixa.Api.Infraestructura.Fotos;

/// <summary>
/// Patrón Adapter — Adaptador: traduce IAlmacenFotos a la API de Cloudinary. Guarda las fotos
/// en formato WebP y con un ancho máximo, y solo devuelve la URL segura (RNF-01).
/// Las credenciales vienen de Cloudinary:CloudName, Cloudinary:ApiKey y Cloudinary:ApiSecret.
/// </summary>
public sealed class CloudinaryAdapter(IConfiguration configuracion) : IAlmacenFotos
{
    private readonly Lazy<Cloudinary> cliente = new(() =>
    {
        var cuenta = new Account(
            configuracion["Cloudinary:CloudName"],
            configuracion["Cloudinary:ApiKey"],
            configuracion["Cloudinary:ApiSecret"]);
        if (string.IsNullOrWhiteSpace(cuenta.Cloud) || string.IsNullOrWhiteSpace(cuenta.ApiKey) || string.IsNullOrWhiteSpace(cuenta.ApiSecret))
            throw new InvalidOperationException("Faltan las credenciales de Cloudinary.");
        return new Cloudinary(cuenta) { Api = { Secure = true } };
    });

    public async Task<FotoGuardada> GuardarAsync(Stream contenido, CancellationToken cancelacion)
    {
        var parametros = new ImageUploadParams
        {
            File = new FileDescription("foto", contenido),
            Folder = "fixa/reportes",
            UniqueFilename = true,
            Overwrite = false,
            Format = "webp",
            Transformation = new Transformation().Width(1600).Crop("limit").Quality("auto"),
        };

        var resultado = await cliente.Value.UploadAsync(parametros, cancelacion);
        if (resultado.Error is not null || resultado.SecureUrl is null)
            throw new InvalidOperationException($"Cloudinary rechazó la foto: {resultado.Error?.Message}");

        return new FotoGuardada(resultado.SecureUrl.ToString(), resultado.PublicId);
    }
}
