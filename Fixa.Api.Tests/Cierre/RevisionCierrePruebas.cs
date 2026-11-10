using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Cierre;

/// <summary>El vecino pide revisar un cierre dentro de 15 días; si se acepta, el caso vuelve a Asignada (RN-03).</summary>
public sealed class RevisionCierrePruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private const string Solicitar = "/api/v1/publico/revisiones";
    private const string MotivoVecino = "El bache sigue igual que antes.";

    private async Task<ReporteCreado> ReporteCerradoAsync()
    {
        var reporte = await ReporteEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var respuesta = await supervisor.PostAsJsonAsync(
            $"/api/v1/panel/casos/intervenciones/{await IntervencionPendienteAsync(reporte.CasoId)}/validacion", new { aprobar = true });
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        return reporte;
    }

    private Task<HttpResponseMessage> PedirAsync(ReporteCreado reporte, string? clave = null) =>
        Anonimo().PostAsJsonAsync(Solicitar, new { codigo = reporte.Codigo, clave = clave ?? reporte.Clave, motivo = MotivoVecino });

    private Task<long> SolicitudAsync(ReporteCreado reporte) =>
        Entorno.EscalarAsync<long>("SELECT s.id FROM solicitudes_revision s JOIN reportes r ON r.id = s.reporte_id WHERE r.codigo = $1", reporte.Codigo);

    [Fact]
    public async Task Aceptar_la_revision_reabre_el_caso_sin_borrar_el_historial()
    {
        var reporte = await ReporteCerradoAsync();
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var eventosAntes = await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1", reporte.CasoId);

        Assert.Equal(HttpStatusCode.Created, (await PedirAsync(reporte)).StatusCode);
        Assert.Contains(reporte.Codigo, await supervisor.GetStringAsync("/api/v1/panel/casos/revisiones"));

        var respuesta = await supervisor.PostAsJsonAsync($"/api/v1/panel/casos/revisiones/{await SolicitudAsync(reporte)}", new { aceptar = true });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("asignada", await EstadoAsync(reporte.CasoId));
        Assert.Null(await Entorno.EscalarAsync<DateTime?>("SELECT fecha_cierre FROM casos WHERE id = $1", reporte.CasoId));
        Assert.Equal(0L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM programaciones WHERE caso_id = $1 AND vigente", reporte.CasoId));
        Assert.Equal(eventosAntes + 2, await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1", reporte.CasoId));

        var seguimiento = await Anonimo().PostAsJsonAsync("/api/v1/publico/seguimiento", new { codigo = reporte.Codigo, clave = reporte.Clave });
        Assert.Contains("\"estado\":\"aprobada\"", await seguimiento.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task No_aceptar_exige_respuesta_y_el_caso_sigue_cerrado()
    {
        var reporte = await ReporteCerradoAsync();
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        await PedirAsync(reporte);
        var ruta = $"/api/v1/panel/casos/revisiones/{await SolicitudAsync(reporte)}";

        var sinRespuesta = await supervisor.PostAsJsonAsync(ruta, new { aceptar = false });
        var conRespuesta = await supervisor.PostAsJsonAsync(ruta, new { aceptar = false, respuesta = "La foto muestra el bache reparado." });
        var otraVez = await supervisor.PostAsJsonAsync(ruta, new { aceptar = true });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinRespuesta.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, conRespuesta.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, otraVez.StatusCode);
        Assert.Equal("cerrada", await EstadoAsync(reporte.CasoId));
    }

    [Fact]
    public async Task Solo_se_pide_revision_de_un_caso_cerrado_con_la_clave_correcta_y_una_a_la_vez()
    {
        var abierto = await ReporteEnAsync(EstadoCaso.Asignada);
        var cerrado = await ReporteCerradoAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PedirAsync(abierto)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await PedirAsync(cerrado, clave: "ZZZZZZ")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PedirAsync(cerrado)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PedirAsync(cerrado)).StatusCode);
    }

    [Fact]
    public async Task Despues_de_15_dias_ya_no_se_puede_pedir_revision()
    {
        var reporte = await ReporteCerradoAsync();
        await Entorno.EjecutarAsync("UPDATE casos SET fecha_cierre = fecha_cierre - interval '16 days' WHERE id = $1", reporte.CasoId);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PedirAsync(reporte)).StatusCode);
    }

    [Fact]
    public async Task Supervisor_de_otra_unidad_no_responde_la_revision()
    {
        var reporte = await ReporteCerradoAsync();
        await PedirAsync(reporte);
        var ajeno = await ComoAsync(Usuarios.SupervisorAlumbrado);

        var respuesta = await ajeno.PostAsJsonAsync($"/api/v1/panel/casos/revisiones/{await SolicitudAsync(reporte)}", new { aceptar = true });

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.DoesNotContain(reporte.Codigo, await ajeno.GetStringAsync("/api/v1/panel/casos/revisiones"));
    }
}
