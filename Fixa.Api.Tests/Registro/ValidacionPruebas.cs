using System.Text;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Registro;

/// <summary>Salud de la API y mensajes de validación en español, sin detalles internos.</summary>
public sealed class ValidacionPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private const string Seguimiento = "/api/v1/publico/seguimiento";

    [Fact]
    public async Task Salud_responde_sin_token_cuando_la_base_esta_disponible()
    {
        var respuesta = await Anonimo().GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("ok", await respuesta.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Campos_faltantes_se_explican_en_espanol()
    {
        var respuesta = await Anonimo().PostAsJsonAsync(Seguimiento, new { });
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Revisa los datos enviados.", cuerpo);
        Assert.Contains("El campo código es obligatorio.", cuerpo);
        Assert.DoesNotContain("required", cuerpo);
    }

    [Fact]
    public async Task Json_mal_formado_no_muestra_detalles_internos()
    {
        var respuesta = await Anonimo().PostAsync(Seguimiento, new StringContent("{\"codigo\": 5, ", Encoding.UTF8, "application/json"));
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.DoesNotContain("System.", cuerpo);
        Assert.DoesNotContain("LineNumber", cuerpo);
        Assert.DoesNotContain("could not be converted", cuerpo);
    }

    [Fact]
    public async Task Numero_invalido_en_el_reporte_se_explica_en_espanol()
    {
        var formulario = FormularioValido();
        formulario["latitud"] = "abc";

        var respuesta = await Anonimo().PostAsync("/api/v1/publico/reportes", Multipart(formulario, FotoJpg));
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.DoesNotContain("is not valid", cuerpo);
    }

    [Fact]
    public async Task La_clave_del_vecino_se_guarda_con_el_costo_rapido_y_sigue_sirviendo()
    {
        var reporte = await RegistrarReporteAsync();

        var hash = await Entorno.EscalarAsync<string>("SELECT clave_hash FROM reportes WHERE codigo = $1", reporte.Codigo);
        var respuesta = await Anonimo().PostAsJsonAsync(Seguimiento, new { codigo = reporte.Codigo, clave = reporte.Clave });

        Assert.StartsWith("$2a$10$", hash);
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }
}
