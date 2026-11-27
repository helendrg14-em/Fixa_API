using System.ComponentModel.DataAnnotations;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Fixa.Api.Modulos.Reportes.Categorias;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

public record FiltroCercanos(
    [Required, StringLength(20)] string Categoria,
    [Required, Range(-90, 90)] double? Latitud,
    [Required, Range(-180, 180)] double? Longitud);

public record SolicitudApoyo(
    [Required, Range(-90, 90)] double? Latitud,
    [Required, Range(-180, 180)] double? Longitud,
    [Required, StringLength(64, MinimumLength = 8)] string ClaveIdempotencia,
    [EmailAddress, StringLength(120)] string? CorreoContacto,
    [StringLength(255)] string? TokenDispositivo);

/// <summary>Fila de la consulta de cercanos (los alias de la SQL coinciden con estas propiedades).</summary>
public sealed record CasoCercanoFila(long CasoId, string Estado, DateTime FechaCreacion, string TipoIncidencia, double DistanciaMetros, int Reportes);

/// <summary>
/// Casos abiertos cerca del vecino antes de reportar (RF-20) y apoyo a uno de ellos (RF-21).
/// Sin login. No devuelve descripciones, fotos, códigos ni datos de contacto (RN-07).
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/publico/casos")]
public class CasosPublicoController(FixaDbContext db) : ControllerBase
{
    private const int MaximoResultados = 10;

    // GET /api/v1/publico/casos/cercanos?categoria=bache&latitud=-17.78&longitud=-63.18
    [HttpGet("cercanos")]
    [EnableRateLimiting(Limites.Consulta)]
    public async Task<IActionResult> Cercanos([FromQuery] FiltroCercanos filtro, CancellationToken cancelacion)
    {
        if (!ValoresBd.TryDesdeBd<CodigoCategoria>(filtro.Categoria, out var categoria))
            throw new ReglaNegocioException("La categoría no existe.");

        var categoriaBd = categoria.ABd();
        var latitud = filtro.Latitud!.Value;
        var longitud = filtro.Longitud!.Value;

        // Consulta con parámetros (FormattableString): los valores nunca se pegan como texto SQL.
        // El radio en metros sale de categorias.radio_cercania_m; ST_DWithin con geography mide en metros.
        var filas = await db.Database.SqlQuery<CasoCercanoFila>($"""
            SELECT caso.id AS "CasoId",
                   caso.estado AS "Estado",
                   caso.fecha_creacion AS "FechaCreacion",
                   (SELECT original.tipo_incidencia FROM reportes original
                     WHERE original.caso_id = caso.id ORDER BY original.es_apoyo, original.id LIMIT 1) AS "TipoIncidencia",
                   MIN(ST_Distance(reporte.ubicacion::geography,
                                   ST_SetSRID(ST_MakePoint({longitud}, {latitud}), 4326)::geography)) AS "DistanciaMetros",
                   (SELECT count(*) FROM reportes todos WHERE todos.caso_id = caso.id)::int AS "Reportes"
            FROM casos caso
            JOIN categorias categoria ON categoria.id = caso.categoria_id
            JOIN reportes reporte ON reporte.caso_id = caso.id
            WHERE categoria.codigo = {categoriaBd}
              AND categoria.activa
              AND caso.estado <> ALL({EstadoCasoReglas.FinalesBd})
              AND ST_DWithin(reporte.ubicacion::geography,
                             ST_SetSRID(ST_MakePoint({longitud}, {latitud}), 4326)::geography,
                             categoria.radio_cercania_m)
            GROUP BY caso.id
            ORDER BY "DistanciaMetros"
            LIMIT {MaximoResultados}
            """).ToListAsync(cancelacion);

        var tiposDeLaCategoria = CreadorCategoria.Para(categoria).Tipos();
        var cercanos = filas.Select(fila => new
        {
            casoId = fila.CasoId,
            estado = ValoresBd.DesdeBd<EstadoCaso>(fila.Estado).Texto(),
            tipo = tiposDeLaCategoria.GetValueOrDefault(fila.TipoIncidencia, fila.TipoIncidencia),
            distanciaMetros = (int)Math.Round(fila.DistanciaMetros),
            vecinosQueReportaron = fila.Reportes,
            desde = DateOnly.FromDateTime(fila.FechaCreacion),
        });
        return Ok(cercanos);
    }

    // POST /api/v1/publico/casos/5/apoyos  (botón «Apoyar reporte»; no pide foto)
    [HttpPost("{casoId:long}/apoyos")]
    [EnableRateLimiting(Limites.Registro)]
    public async Task<IActionResult> Apoyar(long casoId, SolicitudApoyo solicitud, CancellationToken cancelacion)
    {
        var resultado = await new RegistroApoyo(db).RegistrarAsync(new DatosApoyo(
            casoId,
            solicitud.Latitud!.Value,
            solicitud.Longitud!.Value,
            solicitud.ClaveIdempotencia,
            solicitud.CorreoContacto,
            solicitud.TokenDispositivo), cancelacion);

        // La clave solo se entrega una vez; si la solicitud se reenvió, no se vuelve a mostrar.
        return resultado.YaExistia
            ? Ok(new { resultado.Codigo, mensaje = "Este apoyo ya estaba registrado." })
            : StatusCode(StatusCodes.Status201Created, new { resultado.Codigo, resultado.Clave, mensaje = "Gracias por apoyar. Guarda tu código y tu clave para seguir el caso." });
    }
}
