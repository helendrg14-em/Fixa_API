using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Vecino;

/// <summary>
/// Acciones del vecino sin cuenta: casos cercanos y apoyo (RF-20, RF-21), conformidad con el
/// cierre (RF-11) y token del celular para los avisos (RF-18).
/// </summary>
public sealed class AccionesVecinoPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private const string Conformidad = "/api/v1/publico/conformidad";
    private const string Dispositivo = "/api/v1/publico/dispositivo";

    // El radio de cercanía de prueba es 30 m; 0,001° de latitud son unos 110 m.
    private const double DesplazamientoLejano = 0.001;

    /// <summary>
    /// Punto propio de cada prueba dentro del distrito de prueba: las pruebas comparten la base y
    /// no deben encontrarse entre sí como «cercanas».
    /// </summary>
    private static (double Latitud, double Longitud) PuntoPropio() =>
        (-17.795 + Random.Shared.NextDouble() * 0.03, -63.195 + Random.Shared.NextDouble() * 0.03);

    private static string Decimal(double valor) => valor.ToString(System.Globalization.CultureInfo.InvariantCulture);

    private Task<HttpResponseMessage> ApoyarAsync(long casoId, double latitud, double longitud, string? claveIdempotencia = null) =>
        Anonimo().PostAsJsonAsync($"/api/v1/publico/casos/{casoId}/apoyos", new
        {
            latitud,
            longitud,
            claveIdempotencia = claveIdempotencia ?? Guid.NewGuid().ToString("N"),
        });

    private async Task<ReporteCreado> ReporteCerradoAsync()
    {
        var reporte = await ReporteEnAsync(EstadoCaso.PendienteValidacion);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var respuesta = await supervisor.PostAsJsonAsync(
            $"/api/v1/panel/casos/intervenciones/{await IntervencionPendienteAsync(reporte.CasoId)}/validacion", new { aprobar = true });
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        return reporte;
    }

    // ---------------------------------------------------------------- Casos cercanos y apoyo

    [Fact]
    public async Task Cercanos_muestra_el_caso_abierto_sin_datos_personales()
    {
        var (latitud, longitud) = PuntoPropio();
        var reporte = await RegistrarReporteAsync(latitud: latitud, longitud: longitud);

        var respuesta = await Anonimo().GetStringAsync(
            $"/api/v1/publico/casos/cercanos?categoria=bache&latitud={Decimal(latitud)}&longitud={Decimal(longitud)}");

        Assert.Contains($"\"casoId\":{reporte.CasoId}", respuesta);
        Assert.DoesNotContain(reporte.Codigo, respuesta);       // RN-07: sin códigos
        Assert.DoesNotContain("Reporte de prueba", respuesta);  // ni descripciones
    }

    [Fact]
    public async Task Apoyar_un_caso_cercano_suma_un_reporte_sin_cambiar_el_estado()
    {
        var (latitud, longitud) = PuntoPropio();
        var reporte = await RegistrarReporteAsync(latitud: latitud, longitud: longitud);

        var respuesta = await ApoyarAsync(reporte.CasoId, latitud, longitud);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM reportes WHERE caso_id = $1 AND es_apoyo", reporte.CasoId));
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1 AND tipo = 'apoyo'", reporte.CasoId));
        Assert.Equal("recibida", await EstadoAsync(reporte.CasoId));
    }

    [Fact]
    public async Task Apoyar_desde_lejos_es_rechazado()
    {
        var (latitud, longitud) = PuntoPropio();
        var reporte = await RegistrarReporteAsync(latitud: latitud, longitud: longitud);

        var respuesta = await ApoyarAsync(reporte.CasoId, latitud + DesplazamientoLejano, longitud);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal(0L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM reportes WHERE caso_id = $1 AND es_apoyo", reporte.CasoId));
    }

    [Fact]
    public async Task Reenviar_el_mismo_apoyo_no_lo_duplica()
    {
        var (latitud, longitud) = PuntoPropio();
        var reporte = await RegistrarReporteAsync(latitud: latitud, longitud: longitud);
        var clave = Guid.NewGuid().ToString("N");

        var primera = await ApoyarAsync(reporte.CasoId, latitud, longitud, clave);
        var segunda = await ApoyarAsync(reporte.CasoId, latitud, longitud, clave);

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM reportes WHERE caso_id = $1 AND es_apoyo", reporte.CasoId));
    }

    // ---------------------------------------------------------------- Conformidad

    [Fact]
    public async Task Conformidad_en_un_caso_cerrado_se_registra_una_sola_vez()
    {
        var reporte = await ReporteCerradoAsync();
        var datos = new { codigo = reporte.Codigo, clave = reporte.Clave, conforme = true };

        var primera = await Anonimo().PostAsJsonAsync(Conformidad, datos);
        var segunda = await Anonimo().PostAsJsonAsync(Conformidad, datos);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, segunda.StatusCode);
        Assert.True(await Entorno.EscalarAsync<bool>("SELECT conformidad FROM reportes WHERE codigo = $1", reporte.Codigo));
        Assert.Equal("cerrada", await EstadoAsync(reporte.CasoId));
    }

    [Fact]
    public async Task Conformidad_en_un_caso_abierto_es_rechazada()
    {
        var reporte = await RegistrarReporteAsync();

        var respuesta = await Anonimo().PostAsJsonAsync(Conformidad, new { codigo = reporte.Codigo, clave = reporte.Clave, conforme = true });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Clave_incorrecta_responde_401_igual_que_un_codigo_inexistente()
    {
        var reporte = await RegistrarReporteAsync();

        var claveIncorrecta = await Anonimo().PostAsJsonAsync(Conformidad, new { codigo = reporte.Codigo, clave = "XXXXXX", conforme = true });
        var codigoInexistente = await Anonimo().PostAsJsonAsync(Conformidad, new { codigo = "FX-1999-000001", clave = reporte.Clave, conforme = true });

        Assert.Equal(HttpStatusCode.Unauthorized, claveIncorrecta.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, codigoInexistente.StatusCode);
        // Mismo mensaje en los dos casos (solo cambia el traceId de cada petición).
        var tituloClaveIncorrecta = (await claveIncorrecta.Content.ReadFromJsonAsync<Dictionary<string, object>>())!["title"].ToString();
        var tituloCodigoInexistente = (await codigoInexistente.Content.ReadFromJsonAsync<Dictionary<string, object>>())!["title"].ToString();
        Assert.Equal(AccesoVecinoDenegadoException.Mensaje, tituloClaveIncorrecta);
        Assert.Equal(tituloClaveIncorrecta, tituloCodigoInexistente);
    }

    // ---------------------------------------------------------------- Token del celular

    [Fact]
    public async Task El_vecino_puede_actualizar_y_quitar_el_token_del_celular()
    {
        var reporte = await RegistrarReporteAsync();

        var guardar = await Anonimo().PatchAsJsonAsync(Dispositivo, new { codigo = reporte.Codigo, clave = reporte.Clave, tokenDispositivo = "token-de-prueba" });
        Assert.Equal(HttpStatusCode.NoContent, guardar.StatusCode);
        Assert.Equal("token-de-prueba", await Entorno.EscalarAsync<string>("SELECT token_dispositivo FROM reportes WHERE codigo = $1", reporte.Codigo));

        var quitar = await Anonimo().PatchAsJsonAsync(Dispositivo, new { codigo = reporte.Codigo, clave = reporte.Clave, tokenDispositivo = (string?)null });
        Assert.Equal(HttpStatusCode.NoContent, quitar.StatusCode);
        Assert.Null(await Entorno.EscalarAsync<string>("SELECT token_dispositivo FROM reportes WHERE codigo = $1", reporte.Codigo));
    }
}
