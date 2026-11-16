using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Permisos;

/// <summary>
/// El administrador hace todo lo del supervisor en cualquier unidad, incluido cerrar casos, y
/// registra reportes desde el panel (requerimientos v1.2, pedido del docente).
/// </summary>
public sealed class AdministradorPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private const string RutaReportes = "/api/v1/panel/reportes";

    private static Dictionary<string, string> FormularioAsistido(string canal = "telefono", string? motivoSinFoto = "El vecino llamó y no tiene cámara.")
    {
        var formulario = FormularioValido();
        formulario["canal"] = canal;
        if (motivoSinFoto is not null)
            formulario["motivoSinFoto"] = motivoSinFoto;
        return formulario;
    }

    private Task<long> IdAsync(string correo) =>
        Entorno.EscalarAsync<long>("SELECT id FROM usuarios WHERE correo = $1", correo);

    [Fact]
    public async Task Administrador_ve_los_casos_de_todas_las_unidades()
    {
        var bache = (await RegistrarReporteAsync("bache")).CasoId;
        var alumbrado = (await RegistrarReporteAsync("alumbrado")).CasoId;
        var admin = await ComoAsync(Usuarios.Administrador);

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/panel/casos/{bache}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync($"/api/v1/panel/casos/{alumbrado}")).StatusCode);
    }

    [Fact]
    public async Task Administrador_cambia_el_estado_de_un_caso_de_cualquier_unidad()
    {
        var caso = (await RegistrarReporteAsync("alumbrado")).CasoId;
        var admin = await ComoAsync(Usuarios.Administrador);

        var respuesta = await admin.PatchAsJsonAsync($"/api/v1/panel/casos/{caso}/estado", new { estado = "en_revision" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("en_revision", await EstadoAsync(caso));
        Assert.Equal(await IdAsync(Usuarios.Administrador), await Entorno.EscalarAsync<long>(
            "SELECT usuario_id FROM eventos_historial WHERE caso_id = $1 AND estado_nuevo = 'en_revision'", caso));
    }

    [Fact]
    public async Task Administrador_puede_cerrar_el_caso_aprobando_la_evidencia()
    {
        var caso = await CasoEnAsync(EstadoCaso.PendienteValidacion);
        var admin = await ComoAsync(Usuarios.Administrador);
        var intervencion = await IntervencionPendienteAsync(caso);

        var respuesta = await admin.PostAsJsonAsync($"/api/v1/panel/casos/intervenciones/{intervencion}/validacion", new { aprobar = true });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("cerrada", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Administrador_inactivo_no_trabaja_casos()
    {
        var correo = $"admin.inactivo.{Guid.NewGuid():N}@pruebas.fixa";
        await Entorno.CrearUsuarioAsync(correo, "administrador", unidad: null, cuadrilla: null);
        var admin = await ComoAsync(correo);
        await Entorno.EjecutarAsync("UPDATE usuarios SET activo = false WHERE correo = $1", correo);
        var caso = (await RegistrarReporteAsync()).CasoId;

        var respuesta = await admin.PatchAsJsonAsync($"/api/v1/panel/casos/{caso}/estado", new { estado = "en_revision" });

        // Una cuenta desactivada pierde el acceso al instante, aunque su token siga vigente.
        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("recibida", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Administrador_registra_un_reporte_por_telefono_sin_foto_con_motivo()
    {
        var admin = await ComoAsync(Usuarios.Administrador);

        var respuesta = await admin.PostAsync(RutaReportes, Multipart(FormularioAsistido()));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = (await respuesta.Content.ReadFromJsonAsync<Creado>())!;
        Assert.False(string.IsNullOrEmpty(cuerpo.Clave));
        Assert.Equal("telefono", await Entorno.EscalarAsync<string>("SELECT canal_origen FROM reportes WHERE codigo = $1", cuerpo.Codigo));
        Assert.Equal(await IdAsync(Usuarios.Administrador), await Entorno.EscalarAsync<long>("SELECT operador_id FROM reportes WHERE codigo = $1", cuerpo.Codigo));
        Assert.Equal("El vecino llamó y no tiene cámara.", await Entorno.EscalarAsync<string>("SELECT motivo_sin_foto FROM reportes WHERE codigo = $1", cuerpo.Codigo));
        Assert.True(await Entorno.EscalarAsync<bool>(
            "SELECT c.requiere_verificacion_campo FROM casos c JOIN reportes r ON r.caso_id = c.id WHERE r.codigo = $1", cuerpo.Codigo));
    }

    [Fact]
    public async Task Administrador_registra_un_reporte_presencial_con_foto()
    {
        var admin = await ComoAsync(Usuarios.Administrador);

        var respuesta = await admin.PostAsync(RutaReportes, Multipart(FormularioAsistido("presencial", motivoSinFoto: null), FotoJpg));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
    }

    [Fact]
    public async Task Reporte_del_panel_sin_foto_exige_motivo()
    {
        var admin = await ComoAsync(Usuarios.Administrador);

        var respuesta = await admin.PostAsync(RutaReportes, Multipart(FormularioAsistido(motivoSinFoto: null)));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("app_movil")]
    [InlineData("correo")]
    public async Task Reporte_del_panel_solo_acepta_telefono_o_presencial(string canal)
    {
        var admin = await ComoAsync(Usuarios.Administrador);

        var respuesta = await admin.PostAsync(RutaReportes, Multipart(FormularioAsistido(canal), FotoJpg));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Supervisor_no_registra_reportes_desde_el_panel()
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await supervisor.PostAsync(RutaReportes, Multipart(FormularioAsistido()));

        Assert.Equal(HttpStatusCode.Forbidden, respuesta.StatusCode);
    }

    [Fact]
    public async Task La_app_sigue_exigiendo_foto_aunque_mande_motivo()
    {
        var formulario = FormularioValido();
        formulario["motivoSinFoto"] = "No tengo cámara.";

        var respuesta = await EnviarReporteAsync(formulario);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    private sealed record Creado(string Codigo, string Clave);
}
