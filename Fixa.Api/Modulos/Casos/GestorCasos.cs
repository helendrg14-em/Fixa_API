using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Infraestructura.Notificaciones;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Patrón Facade: una sola puerta para todo lo que el supervisor, el técnico y el vecino hacen
/// con un caso. Cada operación coordina por dentro la máquina de estados (RN-03), el historial
/// inmutable, el cálculo de plazos (RN-06), las fotos de evidencia y los avisos al vecino,
/// para que los controladores no repitan esos pasos ni puedan olvidar alguno.
/// </summary>
public sealed class GestorCasos(
    FixaDbContext db, PlazoAtencion plazos, IAlmacenFotos almacen, ServicioNotificaciones avisos, ILogger<GestorCasos> log)
{
    /// <summary>Días corridos que tiene el vecino para pedir que se revise un cierre.</summary>
    public const int DiasParaRevision = 15;

    // Estados que el supervisor cambia directamente; los demás llegan por su operación propia.
    private static readonly EstadoCaso[] cambiosDirectos = [EstadoCaso.EnRevision, EstadoCaso.Rechazada];

    // Permiso de cada operación: recibe el id del caso ya bloqueado.
    private delegate Task<bool> Permiso(long casoId, CancellationToken cancelacion);

    // ---------------------------------------------------------------- Supervisor

    /// <summary>Pasar a revisión o rechazar con motivo (RF-06). Duplicar tiene su propia operación (vincular).</summary>
    public Task CambiarEstadoAsync(long casoId, long supervisorId, EstadoCaso nuevo, string? motivo, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, caso =>
        {
            if (!cambiosDirectos.Contains(nuevo))
                throw new ReglaNegocioException("Ese estado se asigna con su propia operación (prioridad, programación, derivación, vinculación o validación).");
            if (nuevo != EstadoCaso.EnRevision && string.IsNullOrWhiteSpace(motivo))
                throw new ReglaNegocioException("Indica el motivo.");

            return Task.FromResult(Transicion(caso, nuevo, TipoEvento.CambioEstado, $"Caso pasó a {nuevo.Texto()}.", motivo));
        });

    /// <summary>
    /// Corregir la categoría (RF-04, reasignar con motivo). Cada categoría pertenece a una unidad,
    /// así que el caso pasa a la unidad correcta; la prioridad y la meta se vuelven a fijar allí.
    /// </summary>
    public Task ReclasificarAsync(long casoId, long supervisorId, CodigoCategoria codigo, string motivo, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
        {
            var estado = Estado(caso);
            if (estado is not (EstadoCaso.Recibida or EstadoCaso.EnRevision or EstadoCaso.Asignada))
                throw new ReglaNegocioException("La categoría solo se corrige antes de programar el trabajo.");

            var nueva = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo.ABd() && c.Activa, cancelacion)
                ?? throw new ReglaNegocioException("La categoría no existe.");
            if (nueva.Id == caso.CategoriaId)
                throw new ReglaNegocioException("El caso ya tiene esa categoría.");
            var anterior = await db.Categorias.Where(c => c.Id == caso.CategoriaId).Select(c => c.Nombre).FirstAsync(cancelacion);

            caso.CategoriaId = nueva.Id;
            caso.UnidadId = nueva.UnidadId;
            caso.Prioridad = null;
            caso.JustificacionPrioridad = null;
            caso.FechaObjetivo = null;
            caso.Vencido = false;
            return Evento(caso, TipoEvento.Reasignacion, $"Categoría corregida: {anterior} → {nueva.Nombre}; el caso pasa a su unidad.", motivo, estado, estado);
        });

    /// <summary>Confirmar la prioridad con justificación (RF-05) y calcular la fecha objetivo (RF-13).</summary>
    public Task ClasificarPrioridadAsync(long casoId, long supervisorId, Prioridad prioridad, string justificacion, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
        {
            var estado = Estado(caso);
            if (estado is not (EstadoCaso.EnRevision or EstadoCaso.Asignada or EstadoCaso.Programada))
                throw new ReglaNegocioException("La prioridad se fija mientras el caso está en revisión, asignado o programado.");

            caso.Prioridad = prioridad.ABd();
            caso.JustificacionPrioridad = justificacion;
            await RecalcularMetaAsync(caso, cancelacion);

            var evento = Evento(caso, TipoEvento.Priorizacion, $"Prioridad {prioridad.ABd()}.", justificacion, estado, estado);
            return estado == EstadoCaso.EnRevision
                ? Transicion(caso, EstadoCaso.Asignada, TipoEvento.CambioEstado, "Caso asignado tras confirmar la prioridad.", null)
                : evento;
        });

    /// <summary>
    /// Marcar un caso como duplicado de otro. Sus reportes pasan al caso principal como
    /// apoyo: los vecinos conservan su código y ven el avance del caso principal.
    /// </summary>
    public Task VincularAsync(long casoId, long supervisorId, long principalId, string? motivo, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
        {
            if (principalId == caso.Id)
                throw new ReglaNegocioException("Un caso no puede ser duplicado de sí mismo.");
            var principal = await db.Casos.FromSql($"SELECT * FROM casos WHERE id = {principalId} FOR UPDATE").FirstOrDefaultAsync(cancelacion)
                ?? throw new ReglaNegocioException("El caso principal no existe.");
            if (principal.UnidadId != caso.UnidadId || principal.CategoriaId != caso.CategoriaId)
                throw new ReglaNegocioException("El caso principal debe ser de la misma categoría y unidad.");
            if (Estado(principal).EsFinal())
                throw new ReglaNegocioException("El caso principal ya está cerrado, rechazado, derivado o duplicado.");

            var reportes = await db.Reportes.Where(r => r.CasoId == caso.Id).ToListAsync(cancelacion);
            foreach (var reporte in reportes)
            {
                reporte.CasoId = principal.Id;
                reporte.EsApoyo = true;
            }
            var estadoPrincipal = Estado(principal);
            Evento(principal, TipoEvento.Vinculacion, $"Se sumaron {reportes.Count} reporte(s) de apoyo del caso {caso.Id}.", motivo, estadoPrincipal, estadoPrincipal)
                .UsuarioId = supervisorId;
            return Transicion(caso, EstadoCaso.Duplicada, TipoEvento.Vinculacion, $"Duplicado del caso {principal.Id}.", motivo);
        });

    /// <summary>Asignar cuadrilla y fecha (RF-08). Reprogramar exige motivo y anula la programación anterior.</summary>
    public Task ProgramarAsync(long casoId, long supervisorId, long cuadrillaId, DateOnly fecha, string? motivoReprogramacion, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
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
        EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
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

        await EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
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
    /// Responder la solicitud de revisión del vecino. Si se acepta, el caso cerrado
    /// vuelve a Asignada sin borrar su historial (RN-03); si no, la respuesta es obligatoria.
    /// </summary>
    public async Task ResolverRevisionAsync(long solicitudId, long supervisorId, bool aceptar, string? respuesta, CancellationToken cancelacion)
    {
        var casoId = await db.SolicitudesRevision.Where(s => s.Id == solicitudId).Select(s => (long?)s.Reporte.CasoId).FirstOrDefaultAsync(cancelacion)
            ?? throw new NoEncontradoException();

        await EjecutarAsync(casoId, supervisorId, DeSuUnidad(supervisorId), cancelacion, async caso =>
        {
            var solicitud = await db.SolicitudesRevision.FirstAsync(s => s.Id == solicitudId, cancelacion);
            if (solicitud.Estado != EstadoResolucion.Pendiente.ABd())
                throw new ReglaNegocioException("Esta solicitud ya fue respondida.");
            if (!aceptar && string.IsNullOrWhiteSpace(respuesta))
                throw new ReglaNegocioException("Explica al vecino por qué no se reabre el caso.");

            solicitud.Estado = (aceptar ? EstadoResolucion.Aprobada : EstadoResolucion.Rechazada).ABd();
            solicitud.SupervisorId = supervisorId;
            solicitud.Respuesta = respuesta;
            solicitud.FechaResolucion = HoraBolivia.Ahora();

            if (!aceptar)
                return Evento(caso, TipoEvento.Revision, "Revisión no aceptada; el caso sigue cerrado.", respuesta, EstadoCaso.Cerrada, EstadoCaso.Cerrada);

            caso.FechaCierre = null;
            await foreach (var programacion in db.Programaciones.Where(p => p.CasoId == caso.Id && p.Vigente).AsAsyncEnumerable().WithCancellation(cancelacion))
                programacion.Vigente = false;
            await RecalcularMetaAsync(caso, cancelacion);
            return Transicion(caso, EstadoCaso.Asignada, TipoEvento.Revision, "Revisión aceptada: el caso se reabre.", respuesta);
        });
    }

    // ---------------------------------------------------------------- Técnico

    /// <summary>El técnico llega al lugar e inicia el trabajo programado para su cuadrilla.</summary>
    public Task IniciarAtencionAsync(long casoId, long tecnicoId, CancellationToken cancelacion) =>
        EjecutarAsync(casoId, tecnicoId, DeSuCuadrilla(tecnicoId), cancelacion, caso =>
            Task.FromResult(Transicion(caso, EstadoCaso.EnAtencion, TipoEvento.CambioEstado, "El técnico inició el trabajo.", null)));

    /// <summary>
    /// Documentar la intervención con evidencia (RF-09). Las fotos se suben antes de bloquear
    /// el caso; el técnico no cierra: el caso queda Pendiente de validación (RN-04).
    /// </summary>
    public async Task RegistrarIntervencionAsync(
        long casoId, long tecnicoId, string trabajo, TipoEvidencia tipo, string? descripcion, IReadOnlyCollection<IFormFile> fotos, CancellationToken cancelacion)
    {
        if (!await DeSuCuadrilla(tecnicoId)(casoId, cancelacion))
            throw new NoEncontradoException("El caso no existe o no está asignado a tu cuadrilla.");
        // Se revisa antes de subir las fotos para no gastar almacenamiento en un envío que se rechazará.
        if (!await db.Casos.AnyAsync(c => c.Id == casoId && c.Estado == EstadoCaso.EnAtencion.ABd(), cancelacion))
            throw new ReglaNegocioException("Primero inicia la atención del caso.");
        if (tipo == TipoEvidencia.ActaTecnica && string.IsNullOrWhiteSpace(descripcion))
            throw new ReglaNegocioException("Explica por qué se adjunta un acta técnica en lugar de una foto del trabajo.");
        ValidadorFoto.Validar(fotos, obligatoria: true);

        var guardadas = new List<FotoGuardada>();
        foreach (var foto in fotos)
        {
            await using var contenido = foto.OpenReadStream();
            guardadas.Add(await almacen.GuardarAsync(contenido, cancelacion));
        }

        await EjecutarAsync(casoId, tecnicoId, DeSuCuadrilla(tecnicoId), cancelacion, async caso =>
        {
            if (await db.Intervenciones.AnyAsync(i => i.CasoId == caso.Id && i.EstadoValidacion == EstadoResolucion.Pendiente.ABd(), cancelacion))
                throw new ReglaNegocioException("Ya hay una intervención esperando la validación del supervisor.");

            db.Intervenciones.Add(new Intervencion
            {
                CasoId = caso.Id,
                TecnicoId = tecnicoId,
                TrabajoRealizado = trabajo,
                EstadoValidacion = EstadoResolucion.Pendiente.ABd(),
                Evidencias = guardadas.Select(f => new Evidencia { Tipo = tipo.ABd(), Url = f.Url, Descripcion = descripcion }).ToList(),
            });
            return Transicion(caso, EstadoCaso.PendienteValidacion, TipoEvento.Intervencion, "Intervención registrada; espera la validación del supervisor.", null);
        });
    }

    // ---------------------------------------------------------------- Vecino

    /// <summary>
    /// El vecino pide revisar un cierre con el que no está conforme. El controlador ya
    /// verificó su código y clave; aquí se aplican el plazo y la regla de una solicitud a la vez.
    /// </summary>
    public async Task SolicitarRevisionAsync(long reporteId, string motivo, CancellationToken cancelacion)
    {
        var casoId = await db.Reportes.Where(r => r.Id == reporteId).Select(r => r.CasoId).FirstAsync(cancelacion);

        await EjecutarAsync(casoId, usuarioId: null, SinRestriccion, cancelacion, async caso =>
        {
            if (Estado(caso) != EstadoCaso.Cerrada || caso.FechaCierre is null)
                throw new ReglaNegocioException("Solo se puede pedir la revisión de un reporte cerrado.");
            if (HoraBolivia.Ahora() > caso.FechaCierre.Value.AddDays(DiasParaRevision))
                throw new ReglaNegocioException($"Pasaron más de {DiasParaRevision} días desde el cierre; ya no se puede pedir la revisión.");
            if (await db.SolicitudesRevision.AnyAsync(s => s.Reporte.CasoId == caso.Id && s.Estado == EstadoResolucion.Pendiente.ABd(), cancelacion))
                throw new ReglaNegocioException("Ya hay una solicitud de revisión pendiente para este caso.");

            db.SolicitudesRevision.Add(new SolicitudRevision
            {
                ReporteId = reporteId,
                Motivo = motivo,
                Estado = EstadoResolucion.Pendiente.ABd(),
                FechaSolicitud = HoraBolivia.Ahora(),
            });
            return Evento(caso, TipoEvento.Revision, "El vecino pidió revisar el cierre.", motivo, EstadoCaso.Cerrada, EstadoCaso.Cerrada);
        });
    }

    // ---------------------------------------------------------------- Pasos comunes

    /// <summary>
    /// Pasos de toda operación: bloquear el caso, comprobar el permiso, aplicar el cambio,
    /// guardar todo en una transacción y avisar a los vecinos si cambió el estado.
    /// </summary>
    private async Task EjecutarAsync(
        long casoId, long? usuarioId, Permiso permitido, CancellationToken cancelacion, Func<Caso, Task<EventoHistorial>> operacion)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);
        // FOR UPDATE: dos personas no pueden cambiar el mismo caso a la vez.
        var caso = await db.Casos.FromSql($"SELECT * FROM casos WHERE id = {casoId} FOR UPDATE").FirstOrDefaultAsync(cancelacion);
        if (caso is null || !await permitido(caso.Id, cancelacion))
            throw new NoEncontradoException("El caso no existe o no te corresponde.");

        var evento = await operacion(caso);
        evento.UsuarioId = usuarioId;
        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);

        if (evento.EstadoNuevo == evento.EstadoAnterior)
            return;
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

    /// <summary>El supervisor solo trabaja casos de su unidad.</summary>
    private Permiso DeSuUnidad(long supervisorId) => (casoId, cancelacion) =>
        db.Casos.AnyAsync(c => c.Id == casoId && db.Usuarios.Any(u => u.Id == supervisorId && u.UnidadId != null && u.UnidadId == c.UnidadId), cancelacion);

    /// <summary>El técnico solo trabaja casos programados para su cuadrilla.</summary>
    private Permiso DeSuCuadrilla(long tecnicoId) => (casoId, cancelacion) =>
        db.Programaciones.AnyAsync(p => p.CasoId == casoId && p.Vigente
            && db.Usuarios.Any(u => u.Id == tecnicoId && u.CuadrillaId != null && u.CuadrillaId == p.CuadrillaId), cancelacion);

    /// <summary>Para el vecino, cuyo código y clave ya verificó el controlador.</summary>
    private static readonly Permiso SinRestriccion = (_, _) => Task.FromResult(true);

    /// <summary>Fecha objetivo según la meta vigente de la categoría y prioridad del caso (RN-06).</summary>
    private async Task RecalcularMetaAsync(Caso caso, CancellationToken cancelacion)
    {
        caso.FechaObjetivo = caso.Prioridad is null
            ? null
            : await plazos.FechaObjetivoAsync(caso.CategoriaId, ValoresBd.DesdeBd<Prioridad>(caso.Prioridad), cancelacion);
        caso.Vencido = false;
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
