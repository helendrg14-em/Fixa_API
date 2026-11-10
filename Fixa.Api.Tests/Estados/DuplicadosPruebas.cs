using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Estados;

/// <summary>Vincular duplicados: los reportes pasan al caso principal como apoyo.</summary>
public sealed class DuplicadosPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    // Punto dentro del distrito, a más de 1 km del punto de prueba.
    private const double LatitudLejos = -17.7700, LongitudLejos = -63.1700;

    private static string Vinculacion(long caso) => $"/api/v1/panel/casos/{caso}/vinculacion";

    [Fact]
    public async Task Vincular_mueve_el_reporte_al_caso_principal_y_marca_duplicado()
    {
        var principal = await CasoEnAsync(EstadoCaso.Asignada);
        var duplicado = await ReporteEnAsync(EstadoCaso.EnRevision);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await supervisor.PostAsJsonAsync(Vinculacion(duplicado.CasoId), new { casoPrincipalId = principal, motivo = "Mismo bache." });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("duplicada", await EstadoAsync(duplicado.CasoId));
        Assert.Equal(principal, await Entorno.EscalarAsync<long>("SELECT caso_id FROM reportes WHERE codigo = $1", duplicado.Codigo));
        Assert.True(await Entorno.EscalarAsync<bool>("SELECT es_apoyo FROM reportes WHERE codigo = $1", duplicado.Codigo));
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1 AND tipo = 'vinculacion'", principal));

        // El vecino del duplicado conserva su código y ve el avance del caso principal.
        var seguimiento = await Anonimo().PostAsJsonAsync("/api/v1/publico/seguimiento", new { codigo = duplicado.Codigo, clave = duplicado.Clave });
        Assert.Contains("Asignada", await seguimiento.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task No_se_vincula_consigo_mismo_ni_a_un_caso_cerrado_ni_desde_recibida()
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var enRevision = await CasoEnAsync(EstadoCaso.EnRevision);
        var rechazado = await CasoEnAsync(EstadoCaso.EnRevision);
        await supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{rechazado}/estado", new { estado = "rechazada", motivo = "Fuera de alcance." });
        var recibido = await CasoEnAsync(EstadoCaso.Recibida);

        var consigo = await supervisor.PostAsJsonAsync(Vinculacion(enRevision), new { casoPrincipalId = enRevision });
        var aFinal = await supervisor.PostAsJsonAsync(Vinculacion(enRevision), new { casoPrincipalId = rechazado });
        var desdeRecibida = await supervisor.PostAsJsonAsync(Vinculacion(recibido), new { casoPrincipalId = enRevision });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, consigo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, aFinal.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, desdeRecibida.StatusCode);
        Assert.Equal("en_revision", await EstadoAsync(enRevision));
    }

    [Fact]
    public async Task No_se_vincula_con_un_caso_de_otra_categoria()
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var duplicado = await CasoEnAsync(EstadoCaso.EnRevision);
        var otro = await CasoEnAsync(EstadoCaso.EnRevision);
        await supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{otro}/categoria", new { categoria = "alumbrado", motivo = "Es una luminaria, no un bache." });

        var respuesta = await supervisor.PostAsJsonAsync(Vinculacion(duplicado), new { casoPrincipalId = otro });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Cercanos_sugiere_casos_abiertos_dentro_del_radio_y_no_los_lejanos()
    {
        var caso = (await RegistrarReporteAsync()).CasoId;
        var vecino = (await RegistrarReporteAsync()).CasoId;
        var lejano = (await RegistrarReporteAsync(latitud: LatitudLejos, longitud: LongitudLejos)).CasoId;
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var cercanos = await supervisor.GetStringAsync($"/api/v1/panel/casos/{caso}/cercanos");

        Assert.Contains($"\"id\":{vecino},", cercanos);
        Assert.DoesNotContain($"\"id\":{lejano},", cercanos);
        Assert.DoesNotContain($"\"id\":{caso},", cercanos);
    }
}
