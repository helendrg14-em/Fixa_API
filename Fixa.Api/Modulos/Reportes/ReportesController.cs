using System.ComponentModel.DataAnnotations;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Infraestructura.Seguridad;
using Fixa.Api.Modulos.Casos;
using Fixa.Api.Modulos.Reportes.AccionesVecino;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

/// <summary>Datos que envía la app al reportar (multipart/form-data, con las fotos).</summary>
public class SolicitudReporte
{
    [Required, StringLength(20)] public string Categoria { get; set; } = "";
    [Required] public double? Latitud { get; set; }
    [Required] public double? Longitud { get; set; }
    [Required, StringLength(40)] public string TipoIncidencia { get; set; } = "";
    [Required, StringLength(1000)] public string Descripcion { get; set; } = "";
    [Range(1, 200)] public int? NumeroLuminarias { get; set; }
    [StringLength(30)] public string? CodigoAvisoCobranza { get; set; }
    [StringLength(200)] public string? Direccion { get; set; }
    [StringLength(200)] public string? Referencia { get; set; }
    [EmailAddress, StringLength(120)] public string? CorreoContacto { get; set; }
    [StringLength(255)] public string? TokenDispositivo { get; set; }
    [Required, StringLength(64, MinimumLength = 8)] public string ClaveIdempotencia { get; set; } = "";
    public List<IFormFile> Fotos { get; set; } = [];
}

public record SolicitudSeguimiento(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave);

public record SolicitudRevisionCierre(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave,
    [Required, StringLength(1000, MinimumLength = 10)] string Motivo);

public record SolicitudConformidad(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave,
    [Required] bool? Conforme);

public record SolicitudDispositivo(
    [Required, StringLength(20)] string Codigo,
    [Required, StringLength(20)] string Clave,
    [StringLength(255)] string? TokenDispositivo);

/// <summary>
/// Reportes del ciudadano, sin cuenta (RF-01, RF-02, RF-07, RF-11, RF-18). Las acciones con
/// código y clave usan el Template Method (AccionDelVecino): la verificación es siempre la misma.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/publico")]
public class ReportesController(FixaDbContext db, IAlmacenFotos almacen, GestorCasos gestor) : ControllerBase
{
    // POST /api/v1/publico/reportes
    [HttpPost("reportes")]
    [Consumes("multipart/form-data")]
    [EnableRateLimiting(Limites.Registro)]
    public async Task<IActionResult> Registrar([FromForm] SolicitudReporte solicitud, CancellationToken cancelacion)
    {
        var codigo = ValoresBd.TryDesdeBd<CodigoCategoria>(solicitud.Categoria, out var valor) ? valor.ABd() : null;
        var categoria = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo && c.Activa, cancelacion)
            ?? throw new ReglaNegocioException("La categoría no existe.");

        var resultado = await new ReporteBuilder(db, almacen, categoria, CanalOrigen.AppMovil)
            .EnUbicacion(solicitud.Latitud, solicitud.Longitud)
            .ConTipo(solicitud.TipoIncidencia)
            .ConDescripcion(solicitud.Descripcion)
            .ConLuminarias(solicitud.NumeroLuminarias)
            .ConAvisoCobranza(solicitud.CodigoAvisoCobranza)
            .ConDireccion(solicitud.Direccion, solicitud.Referencia)
            .ConContacto(solicitud.CorreoContacto, solicitud.TokenDispositivo)
            .ConFotos(solicitud.Fotos)
            .ConClaveIdempotencia(solicitud.ClaveIdempotencia)
            .RegistrarAsync(cancelacion);

        // La clave solo se entrega una vez; si la solicitud se reenvió, no se vuelve a mostrar.
        return resultado.YaExistia
            ? Ok(new { resultado.Codigo, mensaje = "Este reporte ya estaba registrado." })
            : StatusCode(StatusCodes.Status201Created, new { resultado.Codigo, resultado.Clave, mensaje = "Guarda tu código y tu clave para consultar el reporte." });
    }

    // POST /api/v1/publico/seguimiento  (POST para que la clave no quede en la URL ni en los logs)
    [HttpPost("seguimiento")]
    [EnableRateLimiting(Limites.Consulta)]
    public async Task<IActionResult> Seguimiento(SolicitudSeguimiento solicitud, CancellationToken cancelacion) =>
        Ok(await new ConsultarSeguimiento(db).EjecutarAsync(solicitud.Codigo, solicitud.Clave, cancelacion));

    // POST /api/v1/publico/revisiones  (pedir que se revise un cierre)
    [HttpPost("revisiones")]
    [EnableRateLimiting(Limites.Consulta)]
    public async Task<IActionResult> SolicitarRevision(SolicitudRevisionCierre solicitud, CancellationToken cancelacion)
    {
        await new SolicitarRevisionCierre(db, gestor, solicitud.Motivo.Trim())
            .EjecutarAsync(solicitud.Codigo, solicitud.Clave, cancelacion);
        return StatusCode(StatusCodes.Status201Created, new { mensaje = "Recibimos tu solicitud. El supervisor la revisará." });
    }

    // POST /api/v1/publico/conformidad  (el vecino confirma si el problema quedó solucionado)
    [HttpPost("conformidad")]
    [EnableRateLimiting(Limites.Consulta)]
    public async Task<IActionResult> Conformidad(SolicitudConformidad solicitud, CancellationToken cancelacion)
    {
        var conforme = solicitud.Conforme!.Value;
        await new RegistrarConformidad(db, gestor, conforme).EjecutarAsync(solicitud.Codigo, solicitud.Clave, cancelacion);
        return Ok(new
        {
            mensaje = conforme
                ? "Gracias por confirmar la solución."
                : "Registramos que no estás conforme. Puedes pedir que se revise el cierre.",
        });
    }

    // PATCH /api/v1/publico/dispositivo  (guardar o quitar el token del celular para los avisos push)
    [HttpPatch("dispositivo")]
    [EnableRateLimiting(Limites.Consulta)]
    public async Task<IActionResult> Dispositivo(SolicitudDispositivo solicitud, CancellationToken cancelacion)
    {
        await new ActualizarDispositivo(db, solicitud.TokenDispositivo).EjecutarAsync(solicitud.Codigo, solicitud.Clave, cancelacion);
        return NoContent();
    }
}
