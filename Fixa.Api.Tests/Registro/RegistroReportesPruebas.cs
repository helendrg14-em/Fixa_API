using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Registro;

/// <summary>Registro de reportes y consulta de seguimiento del ciudadano (RF-01, RF-02, RF-07, RN-02, RN-07).</summary>
public sealed class RegistroReportesPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    [Fact]
    public async Task Reporte_valido_crea_caso_recibido_con_codigo_y_evento()
    {
        var reporte = await RegistrarReporteAsync();

        Assert.Matches(@"^FX-\d{4}-\d{6}$", reporte.Codigo);
        Assert.Equal(6, reporte.Clave.Length);
        Assert.Equal("recibida", await EstadoAsync(reporte.CasoId));
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM eventos_historial WHERE caso_id = $1 AND tipo = 'registro'", reporte.CasoId));
    }

    [Fact]
    public async Task Reenviar_la_misma_solicitud_no_crea_otro_caso()
    {
        var formulario = FormularioValido();

        var primera = await EnviarReporteAsync(formulario, FotoJpg);
        var segunda = await EnviarReporteAsync(formulario, FotoJpg);

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.OK, segunda.StatusCode);
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM reportes WHERE clave_idempotencia = $1", formulario["claveIdempotencia"]));
    }

    [Fact]
    public async Task Reporte_sin_foto_es_rechazado()
    {
        var respuesta = await EnviarReporteAsync(FormularioValido());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Archivo_que_no_es_imagen_es_rechazado_aunque_diga_jpg()
    {
        var respuesta = await EnviarReporteAsync(FormularioValido(), "no soy una foto"u8.ToArray());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Ubicacion_fuera_de_los_distritos_es_rechazada()
    {
        var formulario = FormularioValido();
        formulario["latitud"] = LatitudFuera.ToString(System.Globalization.CultureInfo.InvariantCulture);
        formulario["longitud"] = LongitudFuera.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var respuesta = await EnviarReporteAsync(formulario, FotoJpg);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("drenaje")] // fuera del alcance (RN-01)
    [InlineData("seguridad")]
    public async Task Solo_se_aceptan_las_tres_categorias(string categoria)
    {
        var formulario = FormularioValido();
        formulario["categoria"] = categoria;

        var respuesta = await EnviarReporteAsync(formulario, FotoJpg);

        Assert.False(respuesta.IsSuccessStatusCode);
    }

    [Fact]
    public async Task Seguimiento_con_codigo_y_clave_correctos_muestra_el_estado()
    {
        var reporte = await RegistrarReporteAsync();

        var respuesta = await Anonimo().PostAsJsonAsync("/api/v1/publico/seguimiento", new { codigo = reporte.Codigo, clave = reporte.Clave });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains(reporte.Codigo, await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Seguimiento_con_clave_incorrecta_o_codigo_inexistente_da_el_mismo_error()
    {
        var reporte = await RegistrarReporteAsync();

        var claveMala = await Anonimo().PostAsJsonAsync("/api/v1/publico/seguimiento", new { codigo = reporte.Codigo, clave = "ZZZZZZ" });
        var codigoMalo = await Anonimo().PostAsJsonAsync("/api/v1/publico/seguimiento", new { codigo = "FX-1999-000001", clave = reporte.Clave });

        Assert.Equal(HttpStatusCode.Unauthorized, claveMala.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, codigoMalo.StatusCode);
        Assert.Equal(await claveMala.Content.ReadFromJsonAsync<Problema>(), await codigoMalo.Content.ReadFromJsonAsync<Problema>());
    }

    [Fact]
    public async Task Registro_tiene_limite_de_peticiones_por_ip()
    {
        var cliente = Anonimo(ip: "10.200.0.1");
        HttpStatusCode ultimo = default;
        for (var i = 0; i < 11; i++)
            ultimo = (await cliente.PostAsync("/api/v1/publico/reportes", Multipart(FormularioValido(), FotoJpg))).StatusCode;

        Assert.Equal(HttpStatusCode.TooManyRequests, ultimo);
    }

    private sealed record Problema(string Title, int Status);
}
