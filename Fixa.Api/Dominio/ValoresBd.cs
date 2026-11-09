using System.Text.RegularExpressions;

namespace Fixa.Api.Dominio;

/// <summary>
/// La base guarda los enums en snake_case, igual que Laravel (EnRevision → "en_revision").
/// Esta conversión es la única que existe en la API, así ningún valor se escribe a mano.
/// </summary>
public static partial class ValoresBd
{
    public static string ABd<T>(this T valor) where T : struct, Enum =>
        MayusculaInterna().Replace(valor.ToString(), "$1_$2").ToLowerInvariant();

    public static T DesdeBd<T>(string valor) where T : struct, Enum =>
        TryDesdeBd<T>(valor, out var resultado)
            ? resultado
            : throw new InvalidOperationException($"Valor '{valor}' desconocido para {typeof(T).Name}.");

    public static bool TryDesdeBd<T>(string? valor, out T resultado) where T : struct, Enum
    {
        resultado = default;
        if (string.IsNullOrWhiteSpace(valor) || valor.Any(char.IsDigit))
            return false;
        return Enum.TryParse(valor.Replace("_", ""), ignoreCase: true, out resultado) && Enum.IsDefined(resultado);
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex MayusculaInterna();
}
