namespace Fixa.Api.Dominio;

/// <summary>
/// Identificador estable de cada categoría (RN-01). El Factory Method lo usa para
/// elegir el creador de la categoría.
/// </summary>
public enum CodigoCategoria
{
    Bache,
    Alumbrado,
    Residuos
}