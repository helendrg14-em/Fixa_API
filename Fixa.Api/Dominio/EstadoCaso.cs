namespace Fixa.Api.Dominio;

/// <summary>Estados del caso según la máquina de estados de RN-03.</summary>
public enum EstadoCaso
{
    Recibida,
    EnRevision,
    Asignada,
    Programada,
    EnAtencion,
    PendienteValidacion,
    Cerrada,
    Rechazada,
    Duplicada,
    Derivada,
}

public static class EstadoCasoReglas
{
    // RN-03: a qué estados puede pasar cada estado. Ninguna transición borra el historial.
    private static readonly Dictionary<EstadoCaso, EstadoCaso[]> transiciones = new()
    {
        [EstadoCaso.Recibida] = [EstadoCaso.EnRevision],
        [EstadoCaso.EnRevision] = [EstadoCaso.Asignada, EstadoCaso.Rechazada, EstadoCaso.Duplicada],
        [EstadoCaso.Asignada] = [EstadoCaso.Programada, EstadoCaso.Derivada],
        [EstadoCaso.Programada] = [EstadoCaso.Programada, EstadoCaso.EnAtencion],
        [EstadoCaso.EnAtencion] = [EstadoCaso.PendienteValidacion],
        [EstadoCaso.PendienteValidacion] = [EstadoCaso.Cerrada, EstadoCaso.EnAtencion],
        [EstadoCaso.Cerrada] = [EstadoCaso.Asignada],
    };

    public static bool PuedePasarA(this EstadoCaso actual, EstadoCaso nuevo) =>
        transiciones.TryGetValue(actual, out var destinos) && destinos.Contains(nuevo);

    public static bool EsFinal(this EstadoCaso estado) =>
        estado is EstadoCaso.Cerrada or EstadoCaso.Rechazada or EstadoCaso.Duplicada or EstadoCaso.Derivada;

    /// <summary>Los estados finales tal como se guardan en la base, para usarlos en consultas SQL.</summary>
    public static readonly string[] FinalesBd = Enum.GetValues<EstadoCaso>().Where(EsFinal).Select(e => e.ABd()).ToArray();

    /// <summary>Texto que ve el vecino en el seguimiento y en los avisos.</summary>
    public static string Texto(this EstadoCaso estado) => estado switch
    {
        EstadoCaso.Recibida => "Recibida",
        EstadoCaso.EnRevision => "En revisión",
        EstadoCaso.Asignada => "Asignada",
        EstadoCaso.Programada => "Programada",
        EstadoCaso.EnAtencion => "En atención",
        EstadoCaso.PendienteValidacion => "Pendiente de validación",
        EstadoCaso.Cerrada => "Cerrada",
        EstadoCaso.Rechazada => "Rechazada",
        EstadoCaso.Duplicada => "Duplicada",
        EstadoCaso.Derivada => "Derivada a otra entidad",
        _ => estado.ToString(),
    };
}
