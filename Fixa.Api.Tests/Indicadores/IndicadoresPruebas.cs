using System.Text.Json;
using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Indicadores;

/// <summary>Indicadores y mapa por categoría, distrito y periodo; el mapa público es anónimo (RN-07).</summary>
public sealed class IndicadoresPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    [Fact]
    public async Task Supervisor_ve_solo_su_unidad_y_el_administrador_todas()
    {
        await CasoEnAsync(EstadoCaso.Recibida);
        await RegistrarReporteAsync("alumbrado");
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var admin = await ComoAsync(Usuarios.Administrador);

        var propios = await supervisor.GetStringAsync("/api/v1/panel/indicadores");
        var todos = await admin.GetStringAsync("/api/v1/panel/indicadores");

        Assert.Contains("Baches y deterioro vial", propios);
        Assert.DoesNotContain("alumbrado", propios, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Fallas de alumbrado", todos);
        Assert.Contains("Distrito de prueba", todos);
    }

    [Fact]
    public async Task Calcula_porcentaje_dentro_de_meta_y_promedio_de_cierre()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        await supervisor.PostAsJsonAsync($"/api/v1/panel/casos/intervenciones/{await IntervencionPendienteAsync(caso)}/validacion", new { aprobar = true });

        using var json = JsonDocument.Parse(await supervisor.GetStringAsync("/api/v1/panel/indicadores"));
        var raiz = json.RootElement;

        Assert.True(raiz.GetProperty("cerrados").GetInt32() >= 1);
        Assert.True(raiz.GetProperty("cerradosDentroDeMeta").GetInt32() >= 1);
        Assert.Equal(JsonValueKind.Number, raiz.GetProperty("porcentajeDentroDeMeta").ValueKind);
        Assert.Equal(JsonValueKind.Number, raiz.GetProperty("promedioHorasCierre").ValueKind);
    }

    [Theory]
    [InlineData("?desde=2026-02-01&hasta=2026-01-01")] // al revés
    [InlineData("?desde=2024-01-01&hasta=2026-01-01")] // más de un año
    [InlineData("?categoria=drenaje")]                 // fuera de las 3 categorías
    public async Task Filtros_invalidos_son_rechazados(string filtro)
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await supervisor.GetAsync($"/api/v1/panel/indicadores{filtro}")).StatusCode);
    }

    [Fact]
    public async Task Tecnico_no_ve_indicadores()
    {
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        Assert.Equal(HttpStatusCode.Forbidden, (await tecnico.GetAsync("/api/v1/panel/indicadores")).StatusCode);
    }

    [Fact]
    public async Task Mapa_del_panel_es_geojson_con_el_id_del_caso()
    {
        var caso = (await RegistrarReporteAsync()).CasoId;
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var mapa = await supervisor.GetStringAsync("/api/v1/panel/mapa?categoria=bache");

        Assert.Contains("\"type\":\"FeatureCollection\"", mapa);
        Assert.Contains($"\"casoId\":{caso},", mapa);
    }

    [Fact]
    public async Task Mapa_publico_es_anonimo_y_con_ubicacion_aproximada()
    {
        var reporte = await RegistrarReporteAsync();

        using var json = JsonDocument.Parse(await Anonimo().GetStringAsync("/api/v1/publico/mapa"));
        var texto = json.RootElement.GetRawText();
        var punto = json.RootElement.GetProperty("features")[0].GetProperty("geometry").GetProperty("coordinates");

        Assert.DoesNotContain("casoId", texto);
        Assert.DoesNotContain(reporte.Codigo, texto);
        Assert.DoesNotContain("prioridad", texto);
        Assert.Equal(Math.Round(punto[0].GetDouble(), 3), punto[0].GetDouble());
        Assert.Equal(Math.Round(punto[1].GetDouble(), 3), punto[1].GetDouble());
    }
}
