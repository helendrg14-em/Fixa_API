using Fixa.Api.Dominio;
using Fixa.Api.Tests.Infraestructura;

namespace Fixa.Api.Tests.Estados;

/// <summary>Máquina de estados del caso (RN-03), prioridad (RN-05) y programación (RF-08).</summary>
public sealed class MaquinaEstadosPruebas(EntornoPruebas entorno) : PruebaApi(entorno)
{
    private const string Rutas = "/api/v1/panel/casos";

    [Fact]
    public void Las_transiciones_permitidas_son_las_de_RN03()
    {
        Assert.True(EstadoCaso.Recibida.PuedePasarA(EstadoCaso.EnRevision));
        Assert.True(EstadoCaso.EnRevision.PuedePasarA(EstadoCaso.Rechazada));
        Assert.True(EstadoCaso.EnRevision.PuedePasarA(EstadoCaso.Duplicada));
        Assert.True(EstadoCaso.Asignada.PuedePasarA(EstadoCaso.Derivada));
        Assert.True(EstadoCaso.PendienteValidacion.PuedePasarA(EstadoCaso.Cerrada));

        Assert.False(EstadoCaso.Recibida.PuedePasarA(EstadoCaso.Cerrada));
        Assert.False(EstadoCaso.EnAtencion.PuedePasarA(EstadoCaso.Cerrada));
        Assert.False(EstadoCaso.Rechazada.PuedePasarA(EstadoCaso.EnRevision));
    }

    [Fact]
    public async Task Pasar_a_revision_queda_en_el_historial_con_su_autor()
    {
        var caso = (await RegistrarReporteAsync()).CasoId;
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/estado", new { estado = "en_revision" });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("en_revision", await EstadoAsync(caso));
        Assert.NotNull(await Entorno.EscalarAsync<long?>(
            "SELECT usuario_id FROM eventos_historial WHERE caso_id = $1 AND estado_anterior = 'recibida' AND estado_nuevo = 'en_revision'", caso));
    }

    [Fact]
    public async Task No_se_puede_saltar_estados()
    {
        var caso = (await RegistrarReporteAsync()).CasoId;
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var rechazo = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/estado", new { estado = "rechazada", motivo = "No corresponde." });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, rechazo.StatusCode);
        Assert.Equal("recibida", await EstadoAsync(caso));
    }

    [Theory]
    [InlineData("cerrada")]
    [InlineData("asignada")]
    [InlineData("estado_inventado")]
    public async Task Los_estados_con_operacion_propia_no_se_cambian_directo(string estado)
    {
        var caso = await CasoEnAsync(EstadoCaso.EnRevision);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/estado", new { estado, motivo = "Prueba." });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
        Assert.Equal("en_revision", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Rechazar_exige_motivo_y_el_caso_rechazado_no_se_reabre()
    {
        var caso = await CasoEnAsync(EstadoCaso.EnRevision);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var sinMotivo = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/estado", new { estado = "rechazada" });
        var conMotivo = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/estado", new { estado = "rechazada", motivo = "Es una obra de drenaje, fuera de alcance." });
        var reabrir = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/estado", new { estado = "en_revision" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinMotivo.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, conMotivo.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reabrir.StatusCode);
        Assert.Equal("rechazada", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Confirmar_prioridad_asigna_el_caso_y_calcula_la_fecha_objetivo()
    {
        var caso = await CasoEnAsync(EstadoCaso.Asignada);

        Assert.Equal("alta", await Entorno.EscalarAsync<string>("SELECT prioridad FROM casos WHERE id = $1", caso));
        Assert.NotNull(await Entorno.EscalarAsync<DateTime?>("SELECT fecha_objetivo FROM casos WHERE id = $1", caso));
    }

    [Fact]
    public async Task La_prioridad_no_se_fija_en_un_caso_recien_recibido()
    {
        var caso = (await RegistrarReporteAsync()).CasoId;
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await supervisor.PatchAsJsonAsync($"{Rutas}/{caso}/prioridad", new { prioridad = "alta", justificacion = "Afecta a una avenida principal." });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, respuesta.StatusCode);
    }

    [Fact]
    public async Task Programar_valida_fecha_y_cuadrilla_de_la_misma_unidad()
    {
        var caso = await CasoEnAsync(EstadoCaso.Asignada);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var fechaPasada = await supervisor.PostAsJsonAsync($"{Rutas}/{caso}/programaciones", new { cuadrillaId = 1, fecha = HoraBolivia.Hoy().AddDays(-1) });
        var cuadrillaAjena = await supervisor.PostAsJsonAsync($"{Rutas}/{caso}/programaciones", new { cuadrillaId = 3, fecha = HoraBolivia.Hoy() });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, fechaPasada.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, cuadrillaAjena.StatusCode);
        Assert.Equal("asignada", await EstadoAsync(caso));
    }

    [Fact]
    public async Task Reprogramar_exige_motivo_y_deja_una_sola_programacion_vigente()
    {
        var caso = await CasoEnAsync(EstadoCaso.Programada);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var manana = HoraBolivia.Hoy().AddDays(1);

        var sinMotivo = await supervisor.PostAsJsonAsync($"{Rutas}/{caso}/programaciones", new { cuadrillaId = 2, fecha = manana });
        var conMotivo = await supervisor.PostAsJsonAsync($"{Rutas}/{caso}/programaciones", new { cuadrillaId = 2, fecha = manana, motivoReprogramacion = "Lluvia." });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, sinMotivo.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, conMotivo.StatusCode);
        Assert.Equal(1L, await Entorno.EscalarAsync<long>("SELECT count(*) FROM programaciones WHERE caso_id = $1 AND vigente", caso));
    }

    [Fact]
    public async Task Derivar_deja_el_caso_derivado_y_no_resuelto()
    {
        var caso = await CasoEnAsync(EstadoCaso.Asignada);
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);

        var respuesta = await supervisor.PostAsJsonAsync($"{Rutas}/{caso}/derivaciones", new { entidadId = 1, motivo = "Corresponde a otra entidad." });

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal("derivada", await EstadoAsync(caso));
        Assert.Null(await Entorno.EscalarAsync<DateTime?>("SELECT fecha_cierre FROM casos WHERE id = $1", caso));
    }

    [Fact]
    public async Task El_historial_no_se_puede_modificar_ni_borrar()
    {
        var caso = (await RegistrarReporteAsync()).CasoId;

        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => Entorno.EjecutarAsync("UPDATE eventos_historial SET detalle = 'x' WHERE caso_id = $1", caso));
        await Assert.ThrowsAsync<Npgsql.PostgresException>(() => Entorno.EjecutarAsync("DELETE FROM eventos_historial WHERE caso_id = $1", caso));
    }
}
