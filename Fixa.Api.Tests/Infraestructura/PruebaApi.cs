using System.Net.Http.Headers;
using Fixa.Api.Dominio;

namespace Fixa.Api.Tests.Infraestructura;

public sealed record ReporteCreado(string Codigo, string Clave, long CasoId);

/// <summary>
/// Base de las pruebas: clientes HTTP (anónimo o con token) y pasos que se repiten, como
/// registrar un reporte o llevar un caso hasta cierto estado usando la propia API.
/// </summary>
[Collection(ColeccionApi.Nombre)]
public abstract class PruebaApi(EntornoPruebas entorno)
{
    // Punto dentro del distrito de prueba y otro fuera de cualquier distrito.
    protected const double Latitud = -17.7833, Longitud = -63.1821;
    protected const double LatitudFuera = -16.5000, LongitudFuera = -68.1500;

    protected static readonly byte[] FotoJpg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    protected EntornoPruebas Entorno => entorno;

    protected HttpClient Anonimo(string? ip = null)
    {
        var cliente = entorno.Api.CreateClient();
        if (ip is not null)
            cliente.DefaultRequestHeaders.Add(IpDePrueba.Cabecera, ip);
        return cliente;
    }

    protected async Task<HttpClient> ComoAsync(string correo)
    {
        var cliente = entorno.Api.CreateClient();
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await entorno.TokenAsync(correo));
        return cliente;
    }

    /// <summary>Formulario de reporte válido (bache con foto); cada prueba cambia lo que necesita.</summary>
    protected static Dictionary<string, string> FormularioValido() => new()
    {
        ["categoria"] = "bache",
        ["latitud"] = Latitud.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["longitud"] = Longitud.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["tipoIncidencia"] = "bache",
        ["descripcion"] = "Bache de prueba automática",
        ["claveIdempotencia"] = Guid.NewGuid().ToString("N"),
    };

    protected static MultipartFormDataContent Multipart(Dictionary<string, string> campos, params byte[][] fotos)
    {
        var contenido = new MultipartFormDataContent();
        foreach (var (nombre, valor) in campos)
            contenido.Add(new StringContent(valor), nombre);
        foreach (var foto in fotos)
        {
            var archivo = new ByteArrayContent(foto);
            archivo.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            contenido.Add(archivo, "fotos", "foto.jpg");
        }
        return contenido;
    }

    protected Task<HttpResponseMessage> EnviarReporteAsync(Dictionary<string, string> campos, params byte[][] fotos) =>
        Anonimo().PostAsync("/api/v1/publico/reportes", Multipart(campos, fotos));

    protected async Task<ReporteCreado> RegistrarReporteAsync()
    {
        var respuesta = await EnviarReporteAsync(FormularioValido(), FotoJpg);
        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var cuerpo = (await respuesta.Content.ReadFromJsonAsync<RespuestaRegistro>())!;
        var casoId = await entorno.EscalarAsync<long>("SELECT caso_id FROM reportes WHERE codigo = $1", cuerpo.Codigo);
        return new ReporteCreado(cuerpo.Codigo, cuerpo.Clave, casoId);
    }

    protected Task<string?> EstadoAsync(long casoId) =>
        entorno.EscalarAsync<string>("SELECT estado FROM casos WHERE id = $1", casoId);

    /// <summary>
    /// Registra un caso de vialidad y lo lleva por la API hasta el estado pedido, siguiendo el
    /// camino normal de RN-03 (supervisor de vialidad y técnico de la cuadrilla A).
    /// </summary>
    protected async Task<long> CasoEnAsync(EstadoCaso destino) => (await ReporteEnAsync(destino)).CasoId;

    /// <summary>Igual que CasoEnAsync, pero devuelve también el código y la clave del vecino.</summary>
    protected async Task<ReporteCreado> ReporteEnAsync(EstadoCaso destino)
    {
        var reporte = await RegistrarReporteAsync();
        var caso = reporte.CasoId;
        var supervisor = await ComoAsync(Usuarios.SupervisorVialidad);
        var tecnico = await ComoAsync(Usuarios.TecnicoCuadrillaA);

        var pasos = new (EstadoCaso Llega, Func<Task<HttpResponseMessage>> Paso)[]
        {
            (EstadoCaso.EnRevision, () => supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{caso}/estado", new { estado = "en_revision" })),
            (EstadoCaso.Asignada, () => supervisor.PatchAsJsonAsync($"/api/v1/panel/casos/{caso}/prioridad", new { prioridad = "alta", justificacion = "Afecta a una avenida principal." })),
            (EstadoCaso.Programada, () => supervisor.PostAsJsonAsync($"/api/v1/panel/casos/{caso}/programaciones", new { cuadrillaId = 1, fecha = HoraBolivia.Hoy() })),
            (EstadoCaso.EnAtencion, () => tecnico.PostAsync($"/api/v1/panel/ordenes/{caso}/inicio", null)),
            (EstadoCaso.PendienteValidacion, () => tecnico.PostAsync($"/api/v1/panel/ordenes/{caso}/intervenciones", Multipart(new()
            {
                ["trabajoRealizado"] = "Se rellenó el bache con asfalto.",
                ["tipoEvidencia"] = "fotografia",
            }, FotoJpg))),
        };

        foreach (var (llega, paso) in pasos)
        {
            if (destino == EstadoCaso.Recibida)
                break;
            var respuesta = await paso();
            Assert.True(respuesta.IsSuccessStatusCode, $"No se pudo llegar a {llega}: {await respuesta.Content.ReadAsStringAsync()}");
            if (llega == destino)
                break;
        }
        Assert.Equal(destino.ABd(), await EstadoAsync(caso));
        return reporte;
    }

    protected Task<long> IntervencionPendienteAsync(long casoId) =>
        entorno.EscalarAsync<long>("SELECT id FROM intervenciones WHERE caso_id = $1 AND estado_validacion = 'pendiente'", casoId)!;

    private sealed record RespuestaRegistro(string Codigo, string Clave);
}
