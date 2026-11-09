using System.Net.Http.Headers;
using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Permisos;

/// <summary>Permisos verificados en el servidor: token, rol, unidad y cuadrilla (RNF-05, RNF-06).</summary>
public sealed class PermisosPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    [Theory]
    [InlineData("/api/v1/panel/casos")]
    [InlineData("/api/v1/panel/ordenes")]
    [InlineData("/api/v1/panel/auth/yo")]
    public async Task Sin_token_no_se_entra_al_panel(string ruta)
    {
        var respuesta = await Anonimo().GetAsync(ruta);

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Token_falsificado_es_rechazado()
    {
        var cliente = Anonimo();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxIn0.firma-falsa");

        var respuesta = await cliente.GetAsync("/api/v1/panel/casos");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Contrasena_incorrecta_no_entrega_token()
    {
        var respuesta = await Anonimo().PostAsJsonAsync("/api/v1/panel/auth/login", new { correo = Usuarios.SupervisorVialidad, password = "otra-clave" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task Login_tiene_limite_de_intentos_por_ip()
    {
        var cliente = Anonimo(ip: "10.200.0.2");
        HttpStatusCode ultimo = default;
        for (var i = 0; i < 6; i++)
            ultimo = (await cliente.PostAsJsonAsync("/api/v1/panel/auth/login", new { correo = Usuarios.SupervisorVialidad, password = "otra-clave" })).StatusCode;

        Assert.Equal(HttpStatusCode.TooManyRequests, ultimo);
    }

    [Fact]
    public async Task Tecnico_no_entra_a_la_bandeja_del_supervisor()
    {
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        Assert.Equal(HttpStatusCode.Forbidden, (await tecnico.GetAsync("/api/v1/panel/casos")).StatusCode);
    }

    [Fact]
    public async Task Supervisor_no_entra_a_las_ordenes_del_tecnico()
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        Assert.Equal(HttpStatusCode.Forbidden, (await supervisor.GetAsync("/api/v1/panel/ordenes")).StatusCode);
    }

    [Fact]
    public async Task Supervisor_de_otra_unidad_no_ve_ni_cambia_el_caso()
    {
        var caso = (await RegistrarReporteAsync()).CasoId; // bache: unidad de vialidad
        var ajeno = await ComoAsync(Usuarios.SupervisorAlumbrado);

        var detalle = await ajeno.GetAsync($"/api/v1/panel/casos/{caso}");
        var cambio = await ajeno.PatchAsJsonAsync($"/api/v1/panel/casos/{caso}/estado", new { estado = "en_revision" });

        Assert.Equal(HttpStatusCode.NotFound, detalle.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, cambio.StatusCode);
        Assert.Equal("recibida", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Tecnico_de_otra_cuadrilla_no_ve_ni_inicia_la_orden()
    {
        var caso = await CasoEnAsync(EstadoCaso.Programada); // programado para la cuadrilla A
        var ajeno = await ComoAsync(Usuarios.TecnicoCuadrillaB);

        var ordenes = await ajeno.GetStringAsync("/api/v1/panel/ordenes");
        var inicio = await ajeno.PostAsync($"/api/v1/panel/ordenes/{caso}/inicio", null);

        Assert.DoesNotContain($"\"casoId\":{caso},", ordenes);
        Assert.Equal(HttpStatusCode.NotFound, inicio.StatusCode);
        Assert.Equal("programada", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Ordenes_del_tecnico_no_incluyen_datos_de_contacto_del_vecino()
    {
        var caso = await CasoEnAsync(EstadoCaso.Programada);
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        var ordenes = await tecnico.GetStringAsync("/api/v1/panel/ordenes");

        Assert.Contains($"\"casoId\":{caso},", ordenes);
        Assert.DoesNotContain("correo", ordenes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", ordenes, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("clave", ordenes, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cuenta_desactivada_pierde_el_acceso_aunque_su_token_siga_vigente()
    {
        var correo = $"desactivar.{Guid.NewGuid():N}@pruebas.fixa";
        var id = await Entorno.CrearUsuarioAsync(correo, "supervisor", unidad: 1, cuadrilla: null);
        var supervisor = await ComoAsync(correo);
        Assert.Equal(HttpStatusCode.OK, (await supervisor.GetAsync("/api/v1/panel/casos")).StatusCode);

        await Entorno.EjecutarAsync("UPDATE usuarios SET activo = false WHERE id = $1", id);

        Assert.Equal(HttpStatusCode.Unauthorized, (await supervisor.GetAsync("/api/v1/panel/casos")).StatusCode);
    }

    [Fact]
    public async Task Operador_no_puede_entrar_al_panel()
    {
        var correo = $"operador.{Guid.NewGuid():N}@pruebas.fixa";
        await Entorno.CrearUsuarioAsync(correo, "operador", unidad: 1, cuadrilla: null);

        var respuesta = await Anonimo().PostAsJsonAsync("/api/v1/panel/auth/login", new { correo, password = EntornoPruebas.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }
}
