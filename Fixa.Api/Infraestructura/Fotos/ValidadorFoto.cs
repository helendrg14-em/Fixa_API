using Fixa.Api.Dominio;

namespace Fixa.Api.Infraestructura.Fotos;

/// <summary>
/// Revisa cada foto antes de subirla. El tipo se comprueba por los primeros bytes del archivo
/// y no por su nombre ni por el Content-Type, que el cliente puede falsificar.
/// </summary>
public static class ValidadorFoto
{
    public const int MaximoFotos = 3;
    public const long MaximoBytes = 3 * 1024 * 1024;

    private static readonly byte[][] firmas =
    [
        [0xFF, 0xD8, 0xFF],             // JPEG
        [0x89, 0x50, 0x4E, 0x47],       // PNG
    ];

    public static void Validar(IReadOnlyCollection<IFormFile> fotos, bool obligatoria)
    {
        if (obligatoria && fotos.Count == 0)
            throw new ReglaNegocioException("Agrega una foto del problema.");
        if (fotos.Count > MaximoFotos)
            throw new ReglaNegocioException($"Puedes enviar como máximo {MaximoFotos} fotos.");

        foreach (var foto in fotos)
        {
            if (foto.Length is 0 or > MaximoBytes)
                throw new ReglaNegocioException("Cada foto debe pesar como máximo 3 MB.");
            if (!EsImagen(foto))
                throw new ReglaNegocioException("Solo se aceptan fotos JPG, PNG o WebP.");
        }
    }

    private static bool EsImagen(IFormFile foto)
    {
        Span<byte> cabecera = stackalloc byte[12];
        using var flujo = foto.OpenReadStream();
        var leidos = flujo.ReadAtLeast(cabecera, cabecera.Length, throwOnEndOfStream: false);
        var inicio = cabecera[..leidos];

        if (leidos == 12 && inicio[..4].SequenceEqual("RIFF"u8) && inicio[8..12].SequenceEqual("WEBP"u8))
            return true;
        foreach (var firma in firmas)
            if (inicio.StartsWith(firma))
                return true;
        return false;
    }
}
