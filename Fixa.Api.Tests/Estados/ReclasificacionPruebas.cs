using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Estados;

/// <summary>Corregir la categoría con motivo: el caso pasa a la unidad de la nueva categoría (RF-04).</summary>
public sealed class ReclasificacionPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private static string Ruta(long caso) => $"/api/v1/panel/casos/{caso}/categoria";

    [Fact]
    public async Task Corregir_la_categoria_pasa_el_caso_a_la_otra_unidad()
    {
        var caso = await CasoEnAsync(EstadoCaso.EnRevision);
        var vialidad = await ComoAsync(Usuarios.SupervisorVialidad);
        var alumbrado = await ComoAsync(Usuarios.SupervisorAlumbrado);

        var respuesta = await vialidad.PatchAsJsonAsync(Ruta(caso), new { categoria = "alumbrado", motivo = "Es una luminaria apagada, no un bache." });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(2L, await Entorno.EscalarAsync<long>("SELECT unidad_id FROM casos WHERE id = $1", caso));
        Assert.Equal(HttpStatusCode.OK, (await alumbrado.GetAsync($"/api/v1/panel/casos/{caso}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await vialidad.GetAsync($"/api/v1/panel/casos/{caso}")).StatusCode);
        Assert.Equal("en_revision", await EstadoAsync(caso));
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1 AND tipo = 'reasignacion'", caso));
    }

    [Fact]
    public async Task Al_corregir_se_borran_la_prioridad_y_la_meta_de_la_unidad_anterior()
    {
        var caso = await CasoEnAsync(EstadoCaso.Asignada);
        var vialidad = await ComoAsync(Usuarios.SupervisorVialidad);

        await vialidad.PatchAsJsonAsync(Ruta(caso), new { categoria = "residuos", motivo = "Es un microbasural, no un bache." });

        Assert.Null(await Entorno.EscalarAsync<string>("SELECT prioridad FROM casos WHERE id = $1", caso));
        Assert.Null(await Entorno.EscalarAsync<DateTime?>("SELECT fecha_objetivo FROM casos WHERE id = $1", caso));
    }

    [Fact]
    public async Task No_se_corrige_sin_motivo_ni_a_la_misma_categoria()
    {
        var caso = await CasoEnAsync(EstadoCaso.EnRevision);
        var vialidad = await ComoAsync(Usuarios.SupervisorVialidad);

        var sinMotivo = await vialidad.PatchAsJsonAsync(Ruta(caso), new { categoria = "alumbrado" });
        var misma = await vialidad.PatchAsJsonAsync(Ruta(caso), new { categoria = "bache", motivo = "Motivo cualquiera de prueba." });

        Assert.Equal(HttpStatusCode.BadRequest, sinMotivo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, misma.StatusCode);
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT unidad_id FROM casos WHERE id = $1", caso));
    }

    [Fact]
    public async Task No_se_corrige_despues_de_programar_el_trabajo()
    {
        var caso = await CasoEnAsync(EstadoCaso.Programada);
        var vialidad = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await vialidad.PatchAsJsonAsync(Ruta(caso), new { categoria = "alumbrado", motivo = "Es una luminaria apagada, no un bache." });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }
}
