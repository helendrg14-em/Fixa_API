namespace Fixa.Api.Dominio;

/// <summary>
/// Laravel guarda las fechas en hora de Bolivia (APP_TIMEZONE=America/La_Paz) en columnas
/// sin zona horaria. Toda fecha que escriba la API sale de aquí para que ambos coincidan.
/// </summary>
public static class HoraBolivia
{
    private static readonly TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");

    public static DateTime Ahora() =>
        DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zona), DateTimeKind.Unspecified);

    public static DateOnly Hoy() => DateOnly.FromDateTime(Ahora());
}
