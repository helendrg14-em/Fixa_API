using Fixa.Api.Dominio;
using Fixa.Api.Infraestructura.Notificaciones;
using Fixa.Api.Tests.Infraestructura;
using Microsoft.Extensions.DependencyInjection;

namespace Fixa.Api.Tests.Notificaciones;

/// <summary>
/// Plantillas de los avisos (CRUD del administrador), avisos enviados al vecino y reintento de
/// los que no se entregaron. También el rol del usuario en el login.
/// </summary>
public sealed class NotificacionesPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private const string Plantillas = "/api/v1/panel/plantillas-notificacion";
    private const string Avisos = "/api/v1/panel/notificaciones";

    private sealed record PlantillaJson(long Id, string Estado, string Titulo, string Cuerpo, bool Activa);
    private sealed record AvisoJson(long Id, long CasoId, string CodigoReporte, string EstadoCaso, string Canal, string EstadoEntrega);
    private sealed record RegistroJson(string Codigo, string Clave);

    private AvisosFalsos Canal => Entorno.Api.Services.GetServices<ICanalNotificacion>().OfType<AvisosFalsos>().Single();

    private async Task<PlantillaJson> PlantillaDeAsync(HttpClient admin, string estado) =>
        (await admin.GetFromJsonAsync<List<PlantillaJson>>(Plantillas))!.Single(p => p.Estado == estado);

    /// <summary>Reporte de la app con el token de celular indicado; devuelve código, clave y caso.</summary>
    private async Task<ReporteCreado> ReporteConTokenAsync(string token)
    {
        var formulario = FormularioValido();
        formulario["tokenDispositivo"] = token;
        var respuesta = await EnviarReporteAsync(formulario, FotoJpg);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var registro = (await respuesta.Content.ReadFromJsonAsync<RegistroJson>())!;
        var casoId = await Entorno.EscalarAsync<long>("SELECT caso_id FROM reportes WHERE codigo = $1", registro.Codigo);
        return new ReporteCreado(registro.Codigo, registro.Clave, casoId);
    }

    private async Task PasarARevisionAsync(long casoId)
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var respuesta = await supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{casoId}/estado", new { estado = "en_revision" });
        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
    }

    // ---------------------------------------------------------------- Rol en el login

    [Fact]
    public async Task El_login_devuelve_el_rol_del_usuario()
    {
        var respuesta = await Anonimo().PostAsJsonAsync("/api/v1/panel/auth/login",
            new { correo = Usuarios.SupervisorVialidad, password = EntornoPruebas.Password });

        var cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Contains("\"rol\":\"supervisor\"", cuerpo);
        Assert.DoesNotContain("\"tipo\"", cuerpo);
    }

    // ---------------------------------------------------------------- Plantillas

    [Fact]
    public async Task Hay_una_plantilla_inicial_por_cada_estado_del_caso()
    {
        var admin = await ComoAsync(Usuarios.Administrador);
        var plantillas = (await admin.GetFromJsonAsync<List<PlantillaJson>>(Plantillas))!;

        foreach (var estado in Enum.GetValues<EstadoCaso>())
            Assert.Contains(plantillas, p => p.Estado == estado.ABd());
    }

    [Fact]
    public async Task Solo_el_administrador_gestiona_las_plantillas()
    {
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        Assert.Equal(HttpStatusCode.Forbidden, (await supervisor.GetAsync(Plantillas)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Anonimo().GetAsync(Plantillas)).StatusCode);
    }

    [Fact]
    public async Task Crear_editar_y_eliminar_una_plantilla()
    {
        var admin = await ComoAsync(Usuarios.Administrador);
        var derivada = await PlantillaDeAsync(admin, "derivada");

        // Un estado solo tiene una plantilla.
        var repetida = await admin.PostAsJsonAsync(Plantillas, new { estado = "derivada", titulo = "Otra", cuerpo = "Texto de prueba para el estado.", activa = true });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, repetida.StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"{Plantillas}/{derivada.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Plantillas}/{derivada.Id}")).StatusCode);

        var creada = await admin.PostAsJsonAsync(Plantillas, new
        {
            estado = "derivada",
            titulo = "FIXA: tu reporte cambió de estado",
            cuerpo = "Tu reporte {codigo} pasó a la entidad que corresponde.",
            activa = true,
        });
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        Assert.NotNull(creada.Headers.Location);
        var nueva = (await creada.Content.ReadFromJsonAsync<PlantillaJson>())!;

        var editada = await admin.PutAsJsonAsync($"{Plantillas}/{nueva.Id}", new
        {
            estado = "derivada",
            titulo = "FIXA: tu reporte cambió de estado",
            cuerpo = "Tu reporte {codigo} fue derivado a la entidad que corresponde.",
            activa = true,
        });
        Assert.Equal(HttpStatusCode.NoContent, editada.StatusCode);
        Assert.Equal("Tu reporte {codigo} fue derivado a la entidad que corresponde.", (await PlantillaDeAsync(admin, "derivada")).Cuerpo);
    }

    [Fact]
    public async Task Una_plantilla_invalida_se_rechaza()
    {
        var admin = await ComoAsync(Usuarios.Administrador);

        var sinTitulo = await admin.PostAsJsonAsync(Plantillas, new { estado = "recibida", titulo = "", cuerpo = "Texto suficiente para la prueba." });
        Assert.Equal(HttpStatusCode.BadRequest, sinTitulo.StatusCode);

        var estadoInexistente = await admin.PostAsJsonAsync(Plantillas, new { estado = "perdida", titulo = "Título", cuerpo = "Texto suficiente para la prueba." });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, estadoInexistente.StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"{Plantillas}/999999",
            new { estado = "recibida", titulo = "Título", cuerpo = "Texto suficiente para la prueba." })).StatusCode);
    }

    // ---------------------------------------------------------------- Avisos y reintento

    [Fact]
    public async Task El_aviso_usa_el_texto_de_la_plantilla_y_se_puede_reintentar()
    {
        var admin = await ComoAsync(Usuarios.Administrador);
        var enRevision = await PlantillaDeAsync(admin, "en_revision");
        await admin.PutAsJsonAsync($"{Plantillas}/{enRevision.Id}", new
        {
            estado = "en_revision",
            titulo = "FIXA: tu reporte cambió de estado",
            cuerpo = "Estamos revisando tu reporte {codigo}. Estado: {estado}.",
            activa = true,
        });

        var token = $"token-vencido-{Guid.NewGuid():N}";
        var reporte = await ReporteConTokenAsync(token);
        await PasarARevisionAsync(reporte.CasoId);

        // El vecino recibió el texto de la plantilla, con su código y el estado.
        Assert.Contains(Canal.Enviados, envio => envio.Destino == token
            && envio.Aviso.Mensaje == $"Estamos revisando tu reporte {reporte.Codigo}. Estado: En revisión.");

        // El token vencido no recibió el aviso: queda como no entregado.
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var fallidos = (await supervisor.GetFromJsonAsync<List<AvisoJson>>($"{Avisos}?estadoEntrega=no_entregada&casoId={reporte.CasoId}"))!;
        var fallido = Assert.Single(fallidos);
        Assert.Equal(reporte.Codigo, fallido.CodigoReporte);

        // Sin cambiar el contacto, el reintento vuelve a fallar.
        var reintento = await supervisor.PostAsync($"{Avisos}/{fallido.Id}/reintento", null);
        Assert.Equal("no_entregada", (await reintento.Content.ReadFromJsonAsync<AvisoJson>())!.EstadoEntrega);

        // El vecino registra su celular nuevo y el reintento usa ese token.
        var dispositivo = await Anonimo().PatchAsJsonAsync("/api/v1/publico/dispositivo",
            new { codigo = reporte.Codigo, clave = reporte.Clave, tokenDispositivo = "token-nuevo-del-celular" });
        Assert.Equal(HttpStatusCode.NoContent, dispositivo.StatusCode);

        var exitoso = await supervisor.PostAsync($"{Avisos}/{fallido.Id}/reintento", null);
        Assert.Equal(HttpStatusCode.OK, exitoso.StatusCode);
        Assert.Equal("enviada", (await exitoso.Content.ReadFromJsonAsync<AvisoJson>())!.EstadoEntrega);

        // Un aviso ya entregado no se reintenta.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await supervisor.PostAsync($"{Avisos}/{fallido.Id}/reintento", null)).StatusCode);
    }

    [Fact]
    public async Task Los_avisos_no_muestran_el_contacto_del_vecino()
    {
        var token = $"token-privado-{Guid.NewGuid():N}";
        var reporte = await ReporteConTokenAsync(token);
        await PasarARevisionAsync(reporte.CasoId);

        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var cuerpo = await supervisor.GetStringAsync($"{Avisos}?casoId={reporte.CasoId}");

        Assert.Contains(reporte.Codigo, cuerpo);
        Assert.DoesNotContain(token, cuerpo);
    }

    [Fact]
    public async Task El_supervisor_de_otra_unidad_no_ve_los_avisos()
    {
        var reporte = await ReporteConTokenAsync($"token-{Guid.NewGuid():N}");
        await PasarARevisionAsync(reporte.CasoId);
        var admin = await ComoAsync(Usuarios.Administrador);
        var aviso = (await admin.GetFromJsonAsync<List<AvisoJson>>($"{Avisos}?casoId={reporte.CasoId}"))!.First();

        var otro = await ComoAsync(Usuarios.SupervisorAlumbrado);
        Assert.Equal(HttpStatusCode.NotFound, (await otro.GetAsync($"{Avisos}/{aviso.Id}")).StatusCode);
        Assert.Empty((await otro.GetFromJsonAsync<List<AvisoJson>>($"{Avisos}?casoId={reporte.CasoId}"))!);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{Avisos}/999999")).StatusCode);
    }

    [Fact]
    public async Task Con_la_plantilla_desactivada_no_se_avisa_ese_estado()
    {
        var admin = await ComoAsync(Usuarios.Administrador);
        var asignada = await PlantillaDeAsync(admin, "asignada");
        await admin.PutAsJsonAsync($"{Plantillas}/{asignada.Id}", new { estado = "asignada", asignada.Titulo, asignada.Cuerpo, activa = false });
        try
        {
            var reporte = await ReporteConTokenAsync($"token-{Guid.NewGuid():N}");
            await PasarARevisionAsync(reporte.CasoId);
            var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
            await supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{reporte.CasoId}/prioridad",
                new { prioridad = "alta", justificacion = "Afecta a una avenida principal." });

            var avisos = (await admin.GetFromJsonAsync<List<AvisoJson>>($"{Avisos}?casoId={reporte.CasoId}"))!;
            Assert.Contains(avisos, a => a.EstadoCaso == "en_revision");
            Assert.DoesNotContain(avisos, a => a.EstadoCaso == "asignada");
        }
        finally
        {
            await admin.PutAsJsonAsync($"{Plantillas}/{asignada.Id}", new { estado = "asignada", asignada.Titulo, asignada.Cuerpo, activa = true });
        }
    }
}
