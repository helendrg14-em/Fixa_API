using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Notificaciones;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Patrón Facade: una sola puerta para todo lo que el supervisor hace con un caso. Cada
/// operación coordina por dentro la máquina de estados (RN-03), el historial inmutable,
/// el cálculo de plazos (RN-06) y los avisos al vecino, para que el controlador no repita
/// esos pasos ni pueda olvidar alguno.
/// </summary>
public sealed class GestorCasos(FixaDbContext db, PlazoAtencion plazos, ServicioNotificaciones avisos, ILogger<GestorCasos> log)
{
    // Estados que el supervisor cambia directamente; los demás llegan por su operación propia.
    private static readonly EstadoCaso[] cambiosDirectos = [EstadoCaso.EnRevision, EstadoCaso.Rechazada, EstadoCaso.Duplicada];

    /// <summary>Revisar, rechazar o marcar como duplicado (RF-06). Rechazar y duplicar exigen motivo.</summary>
    public Task CambiarEstadoAsync(long casoId, long supervisorId, EstadoCaso nuevo, string? motivo, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, cancelacion, caso =>
        {
            if (!cambiosDirectos.Contains(nuevo))
                throw new ReglaNegocioException("Ese estado se asigna con su propia operación (prioridad, programación, derivación o validación).");
            if (nuevo != EstadoCaso.EnRevision && string.IsNullOrWhiteSpace(motivo))
                throw new ReglaNegocioException("Indica el motivo.");

            return Task.FromResult(Transicion(caso, nuevo, TipoEvento.CambioEstado, $"Caso pasó a {nuevo.Texto()}.", motivo));
        });

    /// <summary>Confirmar la prioridad con justificación (RF-05) y calcular la fecha objetivo (RF-13).</summary>
    public Task ClasificarPrioridadAsync(long casoId, long supervisorId, Prioridad prioridad, string justificacion, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, cancelacion, async caso =>
        {
            var estado = Estado(caso);
            if (estado is not (EstadoCaso.EnRevision or EstadoCaso.Asignada or EstadoCaso.Programada))
                throw new ReglaNegocioException("La prioridad se fija mientras el caso está en revisión, asignado o programado.");

            caso.Prioridad = prioridad.ABd();
            caso.JustificacionPrioridad = justificacion;
            caso.FechaObjetivo = await plazos.FechaObjetivoAsync(caso.CategoriaId, prioridad, cancelacion);
            caso.Vencido = false;

            var evento = Evento(caso, TipoEvento.Priorizacion, $"Prioridad {prioridad.ABd()}.", justificacion, estado, estado);
            return estado == EstadoCaso.EnRevision
                ? Transicion(caso, EstadoCaso.Asignada, TipoEvento.CambioEstado, "Caso asignado tras confirmar la prioridad.", null)
                : evento;
        });

    /// <summary>Asignar cuadrilla y fecha (RF-08). Reprogramar exige motivo y anula la programación anterior.</summary>
    public Task ProgramarAsync(long casoId, long supervisorId, long cuadrillaId, DateOnly fecha, string? motivoReprogramacion, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, cancelacion, async caso =>
        {
            if (fecha < HoraBolivia.Hoy())
                throw new ReglaNegocioException("La fecha prevista no puede ser anterior a hoy.");
            if (!await db.Cuadrillas.AnyAsync(c => c.Id == cuadrillaId && c.UnidadId == caso.UnidadId, cancelacion))
                throw new ReglaNegocioException("La cuadrilla no pertenece a la unidad del caso.");

            var anterior = await db.Programaciones.FirstOrDefaultAsync(p => p.CasoId == caso.Id && p.Vigente, cancelacion);
            if (anterior is not null)
            {
                if (string.IsNullOrWhiteSpace(motivoReprogramacion))
                    throw new ReglaNegocioException("Indica el motivo de la reprogramación.");
                anterior.Vigente = false;
            }
            db.Programaciones.Add(new Programacion
            {
                CasoId = caso.Id,
                CuadrillaId = cuadrillaId,
                FechaPrevista = fecha,
                MotivoReprogramacion = anterior is null ? null : motivoReprogramacion,
                Vigente = true,
            });

            return Transicion(caso, EstadoCaso.Programada, TipoEvento.Programacion, $"Trabajo programado para el {fecha:dd/MM/yyyy}.", motivoReprogramacion);
        });

    /// <summary>Derivar a una entidad externa (RF-12). El caso queda Derivado, nunca como resuelto.</summary>
    public Task DerivarAsync(long casoId, long supervisorId, long entidadId, string motivo, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, cancelacion, async caso =>
        {
            var entidad = await db.EntidadesExternas.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entidadId, cancelacion)
                ?? throw new ReglaNegocioException("La entidad externa no existe.");

            db.Derivaciones.Add(new Derivacion
            {
                CasoId = caso.Id,
                EntidadId = entidad.Id,
                ResponsableId = supervisorId,
                Motivo = motivo,
                EstadoSeguimiento = "en_seguimiento",
            });
            return Transicion(caso, EstadoCaso.Derivada, TipoEvento.Derivacion, $"Derivado a {entidad.Nombre}.", motivo);
        });

    /// <summary>
    /// Validar la intervención del técnico (RF-10, RN-04). Solo el supervisor cierra, y solo
    /// con la evidencia aprobada; si la rechaza, el caso vuelve a En atención.
    /// </summary>
    public async Task ValidarIntervencionAsync(long intervencionId, long supervisorId, bool aprobar, string? observacion, CancellationToken cancelacion)
    {
        var casoId = await db.Intervenciones.Where(i => i.Id == intervencionId).Select(i => (long?)i.CasoId).FirstOrDefaultAsync(cancelacion)
            ?? throw new NoEncontradoException();

        await EjecutarAsync(casoId, supervisorId, cancelacion, async caso =>
        {
            var intervencion = await db.Intervenciones.FirstAsync(i => i.Id == intervencionId, cancelacion);
            if (intervencion.EstadoValidacion != EstadoResolucion.Pendiente.ABd())
                throw new ReglaNegocioException("Esta intervención ya fue validada.");
            if (!aprobar && string.IsNullOrWhiteSpace(observacion))
                throw new ReglaNegocioException("Indica la observación para el técnico.");

            intervencion.EstadoValidacion = (aprobar ? EstadoResolucion.Aprobada : EstadoResolucion.Rechazada).ABd();
            intervencion.SupervisorId = supervisorId;
            intervencion.ObservacionValidacion = observacion;

            if (!aprobar)
                return Transicion(caso, EstadoCaso.EnAtencion, TipoEvento.Validacion, "Evidencia rechazada.", observacion);

            caso.FechaCierre = HoraBolivia.Ahora();
            return Transicion(caso, EstadoCaso.Cerrada, TipoEvento.Validacion, "Evidencia aprobada y caso cerrado.", observacion);
        });
    }

    /// <summary>
    /// Pasos comunes de toda operación: bloquear el caso, comprobar que es de la unidad del
    /// supervisor, aplicar el cambio, guardar todo en una transacción y avisar a los vecinos.
    /// </summary>
    private async Task EjecutarAsync(long casoId, long supervisorId, CancellationToken cancelacion, Func<Caso, Task<EventoHistorial>> operacion)
    {
        var unidad = await db.Usuarios.Where(u => u.Id == supervisorId).Select(u => u.UnidadId).FirstOrDefaultAsync(cancelacion);

        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        // FOR UPDATE: dos supervisores no pueden cambiar el mismo caso a la vez.
        var caso = await db.Casos.FromSql($"SELECT * FROM casos WHERE id = {casoId} FOR UPDATE").FirstOrDefaultAsync(cancelacion);
        if (caso is null || unidad is null || caso.UnidadId != unidad)
            throw new NoEncontradoException("El caso no existe o no es de tu unidad.");

        var evento = await operacion(caso);
        evento.UsuarioId = supervisorId;
        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);

        if (evento.EstadoNuevo != evento.EstadoAnterior)
        {
            try
            {
                await avisos.NotificarAsync(evento, Estado(caso), cancelacion);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                // El cambio ya quedó guardado; un fallo al avisar no debe deshacerlo.
                log.LogError(error, "No se pudo notificar el cambio del caso {CasoId}", caso.Id);
            }
        }
    }

    private EventoHistorial Transicion(Caso caso, EstadoCaso nuevo, TipoEvento tipo, string detalle, string? motivo)
    {
        var actual = Estado(caso);
        if (!actual.PuedePasarA(nuevo))
            throw new ReglaNegocioException($"Un caso {actual.Texto()} no puede pasar a {nuevo.Texto()}.");

        caso.Estado = nuevo.ABd();
        return Evento(caso, tipo, detalle, motivo, actual, nuevo);
    }

    private EventoHistorial Evento(Caso caso, TipoEvento tipo, string detalle, string? motivo, EstadoCaso anterior, EstadoCaso nuevo)
    {
        var evento = new EventoHistorial
        {
            CasoId = caso.Id,
            Tipo = tipo.ABd(),
            EstadoAnterior = anterior.ABd(),
            EstadoNuevo = nuevo.ABd(),
            Detalle = detalle,
            Motivo = motivo,
        };
        db.EventosHistorial.Add(evento);
        return evento;
    }

    private static EstadoCaso Estado(Caso caso) => ValoresBd.DesdeBd<EstadoCaso>(caso.Estado);
}
