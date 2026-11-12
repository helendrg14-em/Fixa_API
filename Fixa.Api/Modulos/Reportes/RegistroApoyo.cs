using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Fixa.Api.Modulos.Reportes.Validacion;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Fixa.Api.Modulos.Reportes;

/// <summary>Lo que envía la app cuando el vecino toca «Apoyar reporte» (sin foto).</summary>
public sealed record DatosApoyo(
    long CasoId,
    double Latitud,
    double Longitud,
    string ClaveIdempotencia,
    string? CorreoContacto,
    string? TokenDispositivo);

/// <summary>
/// Un vecino se suma a un caso abierto cercano en lugar de crear un duplicado (RF-21). El apoyo
/// es un reporte más del caso, con su propio código y clave, para que también reciba avisos.
/// No cambia el estado del caso; queda en el historial y suma a la cantidad de vecinos afectados.
/// </summary>
public sealed class RegistroApoyo(FixaDbContext db)
{
    public async Task<ReporteRegistrado> RegistrarAsync(DatosApoyo apoyo, CancellationToken cancelacion)
    {
        var correo = Limpiar(apoyo.CorreoContacto)?.ToLowerInvariant();
        var token = Limpiar(apoyo.TokenDispositivo);
        CadenasDeValidacion.ParaApoyo().Exigir(new DatosAValidar(
            ClaveIdempotencia: apoyo.ClaveIdempotencia, CorreoContacto: correo, TokenDispositivo: token));

        if (await BuscarExistenteAsync(apoyo.ClaveIdempotencia, cancelacion) is { } existente)
            return existente;

        try
        {
            return await GuardarAsync(apoyo, correo, token, cancelacion);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dos envíos simultáneos con la misma clave: el otro ganó y este devuelve su apoyo.
            db.ChangeTracker.Clear();
            return await BuscarExistenteAsync(apoyo.ClaveIdempotencia, cancelacion)
                ?? throw new InvalidOperationException("No se pudo registrar el apoyo.", error);
        }
    }

    private async Task<ReporteRegistrado> GuardarAsync(DatosApoyo apoyo, string? correo, string? token, CancellationToken cancelacion)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);

        // FOR UPDATE: el caso no puede cerrarse mientras alguien lo apoya.
        var caso = await db.Casos.FromSql($"SELECT * FROM casos WHERE id = {apoyo.CasoId} FOR UPDATE").FirstOrDefaultAsync(cancelacion)
            ?? throw new NoEncontradoException("El caso no existe.");
        if (ValoresBd.DesdeBd<EstadoCaso>(caso.Estado).EsFinal())
            throw new ReglaNegocioException("Este caso ya no está abierto; si el problema sigue, registra un reporte nuevo.");
        if (!await EstaCercaDelCasoAsync(apoyo, cancelacion))
            throw new ReglaNegocioException("Solo puedes apoyar un caso que esté cerca de tu ubicación.");

        // El apoyo toma la ubicación, el distrito y el tipo del reporte que abrió el caso.
        var original = await db.Reportes.AsNoTracking()
            .Where(reporte => reporte.CasoId == caso.Id && !reporte.EsApoyo)
            .OrderBy(reporte => reporte.Id)
            .Select(reporte => new { reporte.DistritoId, reporte.Ubicacion, reporte.TipoIncidencia })
            .FirstAsync(cancelacion);

        var clave = CodigoSeguimiento.NuevaClave();
        var nuevoApoyo = new Reporte
        {
            CasoId = caso.Id,
            DistritoId = original.DistritoId,
            Ubicacion = original.Ubicacion,
            TipoIncidencia = original.TipoIncidencia,
            Codigo = await CodigoSeguimiento.SiguienteAsync(db, cancelacion),
            ClaveHash = VerificadorClave.Hash(clave),
            Descripcion = "Apoyo de un vecino al caso existente.",
            CanalOrigen = CanalOrigen.AppMovil.ABd(),
            EsApoyo = true,
            CorreoContacto = correo,
            TokenDispositivo = token,
            ClaveIdempotencia = apoyo.ClaveIdempotencia,
        };
        db.Reportes.Add(nuevoApoyo);
        db.EventosHistorial.Add(new EventoHistorial
        {
            CasoId = caso.Id,
            Tipo = TipoEvento.Apoyo.ABd(),
            EstadoAnterior = caso.Estado,
            EstadoNuevo = caso.Estado,
            Detalle = $"Un vecino apoyó el caso con el reporte {nuevoApoyo.Codigo}.",
        });

        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return new ReporteRegistrado(nuevoApoyo.Codigo, clave, YaExistia: false);
    }

    /// <summary>
    /// El vecino debe estar dentro del radio de cercanía de la categoría (en metros, con geography).
    /// Evita que alguien infle los apoyos de cualquier caso desde lejos (OWASP API6).
    /// </summary>
    private async Task<bool> EstaCercaDelCasoAsync(DatosApoyo apoyo, CancellationToken cancelacion)
    {
        var resultado = await db.Database.SqlQuery<bool>($"""
            SELECT EXISTS (
                SELECT 1
                FROM reportes reporte
                JOIN casos caso ON caso.id = reporte.caso_id
                JOIN categorias categoria ON categoria.id = caso.categoria_id
                WHERE reporte.caso_id = {apoyo.CasoId}
                  AND ST_DWithin(
                        reporte.ubicacion::geography,
                        ST_SetSRID(ST_MakePoint({apoyo.Longitud}, {apoyo.Latitud}), 4326)::geography,
                        categoria.radio_cercania_m)
            ) AS "Value"
            """).ToListAsync(cancelacion);
        return resultado[0];
    }

    private Task<ReporteRegistrado?> BuscarExistenteAsync(string claveIdempotencia, CancellationToken cancelacion) =>
        CodigoSeguimiento.YaRegistradoAsync(db, claveIdempotencia, cancelacion);

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
