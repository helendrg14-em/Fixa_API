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
}
