using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Estados;

/// <summary>Los casos abiertos con la fecha objetivo pasada se marcan solos como vencidos (RN-06).</summary>
public sealed class VencimientosPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private Task PasarFechaObjetivoAsync(long caso) =>
        Entorno.EjecutarAsync("UPDATE casos SET fecha_objetivo = now() - interval '2 days' WHERE id = $1", caso);

    private Task<long> EventosVencimientoAsync(long caso) =>
        Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1 AND tipo = 'vencimiento'", caso);

    [Fact]
    public async Task Caso_abierto_con_fecha_pasada_se_marca_vencido_una_sola_vez()
    {
        var caso = await CasoEnAsync(EstadoCaso.Asignada);
        await PasarFechaObjetivoAsync(caso);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        await supervisor.GetAsync("/api/v1/panel/casos");
        await supervisor.GetAsync("/api/v1/panel/casos");

        Assert.True(await Entorno.EscalarAsync<bool>("SELECT vencido FROM casos WHERE id = $1", caso));
        Assert.Equal(1L, await EventosVencimientoAsync(caso));
    }

    [Fact]
    public async Task Caso_cerrado_no_se_marca_vencido()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        await supervisor.PostAsJsonAsync($"/api/v1/panel/casos/intervenciones/{await IntervencionPendienteAsync(caso)}/validacion", new { aprobar = true });
        await PasarFechaObjetivoAsync(caso);

        await supervisor.GetAsync("/api/v1/panel/casos");

        Assert.False(await Entorno.EscalarAsync<bool>("SELECT vencido FROM casos WHERE id = $1", caso));
        Assert.Equal(0L, await EventosVencimientoAsync(caso));
    }

    [Fact]
    public async Task Volver_a_fijar_la_prioridad_recalcula_la_meta_y_quita_el_vencido()
    {
        var caso = await CasoEnAsync(EstadoCaso.Asignada);
        await PasarFechaObjetivoAsync(caso);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        await supervisor.GetAsync("/api/v1/panel/casos");

        await supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{caso}/prioridad", new { prioridad = "alta", justificacion = "Se reevalúa tras nueva información." });

        Assert.False(await Entorno.EscalarAsync<bool>("SELECT vencido FROM casos WHERE id = $1", caso));
    }
}
