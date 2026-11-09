using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Cierre;

/// <summary>Intervención del técnico y cierre con evidencia solo por el supervisor (RF-09, RF-10, RN-04).</summary>
public sealed class CierrePruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private static string Validacion(long intervencion) => $"/api/v1/panel/casos/intervenciones/{intervencion}/validacion";

    private static MultipartFormDataContent Intervencion(string tipo = "fotografia", string? descripcion = null, bool conFoto = true)
    {
        var campos = new Dictionary<string, string> { ["trabajoRealizado"] = "Se rellenó el bache con asfalto.", ["tipoEvidencia"] = tipo };
        if (descripcion is not null)
            campos["descripcion"] = descripcion;
        return conFoto ? Multipart(campos, FotoJpg) : Multipart(campos);
    }

    [Fact]
    public async Task Tecnico_no_registra_intervencion_sin_iniciar_la_atencion()
    {
        var caso = await CasoEnAsync(EstadoCaso.Programada);
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        var respuesta = await tecnico.PostAsync($"/api/v1/panel/ordenes/{caso}/intervenciones", Intervencion());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("programada", await EstadoAsync(caso));
    }

    [Theory]
    [InlineData("fotografia", null, false)]     // sin foto
    [InlineData("acta_tecnica", null, true)]    // acta sin explicación
    [InlineData("video", null, true)]           // tipo inexistente
    public async Task La_evidencia_incompleta_es_rechazada(string tipo, string? descripcion, bool conFoto)
    {
        var caso = await CasoEnAsync(EstadoCaso.EnAtencion);
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        var respuesta = await tecnico.PostAsync($"/api/v1/panel/ordenes/{caso}/intervenciones", Intervencion(tipo, descripcion, conFoto));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("en_atencion", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Intervencion_deja_el_caso_pendiente_de_validacion_y_no_cerrado()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);

        Assert.Null(await Entorno.EscalarAsync<DateTime?>("SELECT fecha_cierre FROM casos WHERE id = $1", caso));
        Assert.Equal(1L, await Entorno.EscalarAsync<long>(
            "SELECT count(*) FROM evidencias e JOIN intervenciones i ON i.id = e.intervencion_id WHERE i.caso_id = $1", caso));
    }

    [Fact]
    public async Task El_tecnico_no_puede_cerrar_su_propio_trabajo()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        var respuesta = await tecnico.PostAsJsonAsync(Validacion(await IntervencionPendienteAsync(caso)), new { aprobar = true });

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
        Assert.Equal("pendiente_validacion", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Supervisor_de_otra_unidad_no_puede_cerrar()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var ajeno = await ComoAsync(Usuarios.SupervisorAlumbrado);

        var respuesta = await ajeno.PostAsJsonAsync(Validacion(await IntervencionPendienteAsync(caso)), new { aprobar = true });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("pendiente_validacion", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Rechazar_la_evidencia_exige_observacion_y_devuelve_el_caso_a_atencion()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var intervencion = await IntervencionPendienteAsync(caso);

        var sinObservacion = await supervisor.PostAsJsonAsync(Validacion(intervencion), new { aprobar = false });
        var conObservacion = await supervisor.PostAsJsonAsync(Validacion(intervencion), new { aprobar = false, observacion = "La foto no muestra el trabajo terminado." });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinObservacion.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, conObservacion.StatusCode);
        Assert.Equal("en_atencion", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Aprobar_la_evidencia_cierra_el_caso_una_sola_vez()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var intervencion = await IntervencionPendienteAsync(caso);

        var aprobar = await supervisor.PostAsJsonAsync(Validacion(intervencion), new { aprobar = true });
        var repetir = await supervisor.PostAsJsonAsync(Validacion(intervencion), new { aprobar = true });

        Assert.Equal(HttpStatusCode.NoContent, aprobar.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, repetir.StatusCode);
        Assert.Equal("cerrada", await EstadoAsync(caso));
        Assert.NotNull(await Entorno.EscalarAsync<DateTime?>("SELECT fecha_cierre FROM casos WHERE id = $1", caso));
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1 AND estado_nuevo = 'cerrada'", caso));
    }

    [Fact]
    public async Task El_ciudadano_ve_su_reporte_cerrado_en_el_seguimiento()
    {
        var reporte = await ReporteEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        await supervisor.PostAsJsonAsync(Validacion(await IntervencionPendienteAsync(reporte.CasoId)), new { aprobar = true });

        var seguimiento = await Anonimo().PostAsJsonAsync("/api/v1/publico/seguimiento", new { codigo = reporte.Codigo, clave = reporte.Clave });

        Assert.Equal(HttpStatusCode.OK, seguimiento.StatusCode);
        Assert.Contains("Cerrada", await seguimiento.Content.ReadAsStringAsync());
    }
}
