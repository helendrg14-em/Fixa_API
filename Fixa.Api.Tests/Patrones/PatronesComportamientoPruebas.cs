using Fixa.Api.Dominio;
using Fixa.Api.Modulos.Casos.Plazos;
using Fixa.Api.Modulos.Reportes.Categorias;
using Fixa.Api.Modulos.Reportes.Validacion;

namespace Fixa.Api.Tests.Patrones;

/// <summary>Pruebas unitarias del Strategy y de la Chain of Responsibility (no usan la base).</summary>
public sealed class PatronesComportamientoPruebas
{
    private static readonly DatosReporte BacheValido = new(-17.78, -63.18, "bache", "Bache en la avenida", null, null);

    [Fact]
    public async Task Strategy_horas_corridas_suma_las_horas_sin_pausas()
    {
        var sabadoNoche = new DateTime(2026, 11, 14, 22, 0, 0);

        var fechaObjetivo = await new PlazoEnHorasCorridas(24).FechaObjetivoAsync(sabadoNoche, CancellationToken.None);

        Assert.Equal(new DateTime(2026, 11, 15, 22, 0, 0), fechaObjetivo);
    }

    [Fact]
    public void Cadena_de_reporte_valido_no_devuelve_errores()
    {
        var errores = new List<string>();

        CadenasDeValidacion.ParaReporteNuevo().Validar(new DatosAValidar(
            Reporte: BacheValido, Categoria: CodigoCategoria.Bache, ClaveIdempotencia: Guid.NewGuid().ToString("N")), errores);

        Assert.Empty(errores);
    }

    [Fact]
    public void Cadena_junta_los_errores_de_todos_los_eslabones()
    {
        var errores = new List<string>();

        CadenasDeValidacion.ParaReporteNuevo().Validar(new DatosAValidar(
            Reporte: BacheValido,
            Categoria: CodigoCategoria.Bache,
            ClaveIdempotencia: "corta",
            CorreoContacto: "no-es-correo"), errores);

        Assert.Equal(2, errores.Count);
        Assert.Contains(errores, error => error.Contains("idempotencia"));
        Assert.Contains(errores, error => error.Contains("correo"));
    }

    [Fact]
    public void Cadena_de_apoyo_no_pide_categoria_ni_foto()
    {
        var errores = new List<string>();

        CadenasDeValidacion.ParaApoyo().Validar(new DatosAValidar(ClaveIdempotencia: Guid.NewGuid().ToString("N")), errores);

        Assert.Empty(errores);
    }

    [Fact]
    public void Exigir_lanza_regla_de_negocio_con_todos_los_errores()
    {
        var error = Assert.Throws<ReglaNegocioException>(() =>
            CadenasDeValidacion.ParaApoyo().Exigir(new DatosAValidar(ClaveIdempotencia: null)));

        Assert.Contains("idempotencia", error.Message);
    }
}
