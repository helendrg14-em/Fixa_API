using System.ComponentModel.DataAnnotations;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Modulos.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

/// <summary>Reporte que registra el administrador desde el panel (multipart/form-data).</summary>
public class SolicitudReporteAsistido
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

    /// <summary>Por dónde llegó el reclamo: telefono o presencial.</summary>
    [Required, StringLength(25)] public string Canal { get; set; } = "";

    /// <summary>Obligatorio cuando no se adjunta foto (RN-02).</summary>
    [StringLength(200)] public string? MotivoSinFoto { get; set; }

    [Required, StringLength(64, MinimumLength = 8)] public string ClaveIdempotencia { get; set; } = "";
    public List<IFormFile> Fotos { get; set; } = [];
}

/// <summary>
/// Registro asistido (R07) desde el panel: el administrador carga el reclamo de un vecino que
/// llamó o fue en persona. Usa el mismo ReporteBuilder que la app, así las reglas son las mismas;
/// la diferencia es el canal, quién lo registró y que la foto puede faltar con motivo.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Administrador)]
[Route("api/v1/panel/reportes")]
public class PanelReportesController(FixaDbContext db, IAlmacenFotos almacen) : ControllerBase
{
    // POST /api/v1/panel/reportes
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Registrar([FromForm] SolicitudReporteAsistido solicitud, CancellationToken cancelacion)
    {
        if (!ValoresBd.TryDesdeBd<CanalOrigen>(solicitud.Canal, out var canal) || canal is not (CanalOrigen.Telefono or CanalOrigen.Presencial))
            throw new ReglaNegocioException("El canal debe ser telefono o presencial.");

        var codigo = ValoresBd.TryDesdeBd<CodigoCategoria>(solicitud.Categoria, out var valor) ? valor.ABd() : null;
        var categoria = await db.Categorias.AsNoTracking().FirstOrDefaultAsync(c => c.Codigo == codigo && c.Activa, cancelacion)
            ?? throw new ReglaNegocioException("La categoría no existe.");

        var resultado = await new ReporteBuilder(db, almacen, categoria, canal)
            .EnUbicacion(solicitud.Latitud, solicitud.Longitud)
            .ConTipo(solicitud.TipoIncidencia)
            .ConDescripcion(solicitud.Descripcion)
            .ConLuminarias(solicitud.NumeroLuminarias)
            .ConAvisoCobranza(solicitud.CodigoAvisoCobranza)
            .ConDireccion(solicitud.Direccion, solicitud.Referencia)
            .ConContacto(solicitud.CorreoContacto, token: null)
            .ConFotos(solicitud.Fotos)
            .ConClaveIdempotencia(solicitud.ClaveIdempotencia)
            .RegistradoPor(ServicioAutenticacion.Instancia.IdUsuario(User), solicitud.MotivoSinFoto)
            .RegistrarAsync(cancelacion);

        // El funcionario le dicta al vecino el código y la clave; la clave solo se entrega una vez.
        return resultado.YaExistia
            ? Ok(new { resultado.Codigo, mensaje = "Este reporte ya estaba registrado." })
            : StatusCode(StatusCodes.Status201Created, new { resultado.Codigo, resultado.Clave, mensaje = "Entrega al vecino el código y la clave." });
    }
}
