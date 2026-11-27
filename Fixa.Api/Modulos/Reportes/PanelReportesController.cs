using Fixa.Api.Dtos;
using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Modulos.Auth;
using Fixa.Api.Modulos.Casos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Modulos.Reportes;

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
        if (resultado.YaExistia)
            return Ok(new ReporteRegistradoSalida(resultado.Codigo, null, null, "Este reporte ya estaba registrado."));

        // Location apunta al caso que abrió (o al que se sumó) el reporte.
        var casoId = await db.Reportes.Where(r => r.Codigo == resultado.Codigo).Select(r => r.CasoId).FirstAsync(cancelacion);
        return CreatedAtAction(nameof(CasosController.Detalle), "Casos", new { id = casoId },
            new ReporteRegistradoSalida(resultado.Codigo, resultado.Clave, casoId, "Entrega al vecino el código y la clave."));
    }
}
