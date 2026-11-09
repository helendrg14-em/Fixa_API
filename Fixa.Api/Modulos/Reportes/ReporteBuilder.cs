using System.Net.Mail;
using System.Security.Cryptography;
using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Fotos;
using Fixa.Api.Infraestructura.Seguridad;
using Fixa.Api.Modulos.Reportes.Categorias;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;

namespace Fixa.Api.Modulos.Reportes;

public record ReporteRegistrado(string Codigo, string? Clave, bool YaExistia);

/// <summary>
/// Patrón Builder: un reporte tiene datos comunes, datos de su categoría y datos que dependen
/// del canal. Se arma paso a paso y RegistrarAsync() recién valida el conjunto y lo guarda de
/// una sola vez: caso, reporte, fotos y evento del historial (guardado atómico e idempotente).
///
/// Usa el Factory Method para las reglas de cada categoría y el Adapter para guardar las fotos.
/// </summary>
public sealed class ReporteBuilder(FixaDbContext db, IAlmacenFotos almacen, Categoria categoria, CanalOrigen canal)
{
    private const string AlfabetoClave = "ABCDEFGHJKMNPQRSTUVWXYZ23456789"; // sin 0/O ni 1/I/L
    private const int LargoClave = 6;

    private double? latitud, longitud;
    private int? numeroLuminarias;
    private string? tipo, descripcion, direccion, referencia, avisoCobranza, correo, tokenDispositivo, claveIdempotencia;
    private IReadOnlyCollection<IFormFile> fotos = [];

    public ReporteBuilder EnUbicacion(double? lat, double? lon) { latitud = lat; longitud = lon; return this; }
    public ReporteBuilder ConTipo(string? valor) { tipo = valor; return this; }
    public ReporteBuilder ConDescripcion(string? valor) { descripcion = valor?.Trim(); return this; }
    public ReporteBuilder ConDireccion(string? valor, string? referenciaLugar) { direccion = Limpiar(valor); referencia = Limpiar(referenciaLugar); return this; }
    public ReporteBuilder ConLuminarias(int? numero) { numeroLuminarias = numero; return this; }
    public ReporteBuilder ConAvisoCobranza(string? codigo) { avisoCobranza = Limpiar(codigo); return this; }
    public ReporteBuilder ConContacto(string? correoContacto, string? token) { correo = Limpiar(correoContacto)?.ToLowerInvariant(); tokenDispositivo = Limpiar(token); return this; }
    public ReporteBuilder ConFotos(IReadOnlyCollection<IFormFile> archivos) { fotos = archivos; return this; }
    public ReporteBuilder ConClaveIdempotencia(string? clave) { claveIdempotencia = clave; return this; }

    public async Task<ReporteRegistrado> RegistrarAsync(CancellationToken cancelacion)
    {
        Validar();

        if (await BuscarExistenteAsync(cancelacion) is { } existente)
            return existente;

        var punto = new Point(longitud!.Value, latitud!.Value) { SRID = 4326 };
        var distritoId = await db.Distritos.Where(d => d.Limite.Contains(punto)).Select(d => (long?)d.Id).FirstOrDefaultAsync(cancelacion)
            ?? throw new ReglaNegocioException("La ubicación está fuera de los distritos que atiende FIXA.");

        // Las fotos se suben antes de abrir la transacción: un servicio externo lento no debe
        // mantener bloqueada la base.
        var guardadas = new List<FotoGuardada>();
        foreach (var foto in fotos)
        {
            await using var contenido = foto.OpenReadStream();
            guardadas.Add(await almacen.GuardarAsync(contenido, cancelacion));
        }

        try
        {
            return await GuardarAsync(punto, distritoId, guardadas, cancelacion);
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dos envíos simultáneos con la misma clave: el otro ganó y este devuelve su reporte.
            db.ChangeTracker.Clear();
            return await BuscarExistenteAsync(cancelacion) ?? throw new InvalidOperationException("No se pudo registrar el reporte.", error);
        }
    }

    private void Validar()
    {
        var errores = CreadorCategoria.Para(ValoresBd.DesdeBd<CodigoCategoria>(categoria.Codigo))
            .Validar(new DatosReporte(latitud, longitud, tipo, descripcion, numeroLuminarias, avisoCobranza));

        if (claveIdempotencia is null || claveIdempotencia.Length is < 8 or > 64 || !claveIdempotencia.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            errores.Add("Falta la clave de idempotencia (8 a 64 letras, números o guiones).");
        if (direccion?.Length > 200 || referencia?.Length > 200)
            errores.Add("La dirección y la referencia tienen como máximo 200 caracteres.");
        if (correo is not null && (correo.Length > 120 || !MailAddress.TryCreate(correo, out _)))
            errores.Add("El correo de contacto no es válido.");
        if (tokenDispositivo?.Length > 255)
            errores.Add("El identificador del dispositivo no es válido.");

        if (errores.Count > 0)
            throw new ReglaNegocioException(string.Join(" ", errores));

        // RN-02: la foto es obligatoria desde la app; solo el registro asistido (versión futura) puede omitirla.
        ValidadorFoto.Validar(fotos, obligatoria: canal is CanalOrigen.AppMovil or CanalOrigen.Web);
    }

    private async Task<ReporteRegistrado?> BuscarExistenteAsync(CancellationToken cancelacion)
    {
        var codigo = await db.Reportes.AsNoTracking()
            .Where(r => r.ClaveIdempotencia == claveIdempotencia)
            .Select(r => r.Codigo)
            .FirstOrDefaultAsync(cancelacion);
        return codigo is null ? null : new ReporteRegistrado(codigo, Clave: null, YaExistia: true);
    }

    private async Task<ReporteRegistrado> GuardarAsync(Point punto, long distritoId, List<FotoGuardada> guardadas, CancellationToken cancelacion)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync(cancelacion);

        var caso = new Caso
        {
            CategoriaId = categoria.Id,
            UnidadId = categoria.UnidadId, // RF-04: asignación automática a la unidad de la categoría
            Estado = EstadoCaso.Recibida.ABd(),
            RequiereVerificacionCampo = guardadas.Count == 0,
        };
        var clave = RandomNumberGenerator.GetString(AlfabetoClave, LargoClave);
        var reporte = new Reporte
        {
            Caso = caso,
            DistritoId = distritoId,
            Codigo = await SiguienteCodigoAsync(cancelacion),
            ClaveHash = VerificadorClave.Hash(clave),
            Descripcion = descripcion!,
            CanalOrigen = canal.ABd(),
            TipoIncidencia = tipo!,
            Ubicacion = punto,
            Direccion = direccion,
            Referencia = referencia,
            NumeroLuminarias = numeroLuminarias,
            CodigoAvisoCobranza = avisoCobranza,
            CorreoContacto = correo,
            TokenDispositivo = tokenDispositivo,
            ClaveIdempotencia = claveIdempotencia!,
            FotosReporte = guardadas.Select(f => new FotoReporte { Url = f.Url, PublicId = f.PublicId }).ToList(),
        };
        db.Reportes.Add(reporte);
        db.EventosHistorial.Add(new EventoHistorial
        {
            Caso = caso,
            Tipo = TipoEvento.Registro.ABd(),
            EstadoNuevo = EstadoCaso.Recibida.ABd(),
            Detalle = $"Reporte {reporte.Codigo} registrado por {canal.ABd()}.",
        });

        await db.SaveChangesAsync(cancelacion);
        await transaccion.CommitAsync(cancelacion);
        return new ReporteRegistrado(reporte.Codigo, clave, YaExistia: false);
    }

    /// <summary>
    /// Código visible FX-AAAA-000123, correlativo por gestión. El contador queda bloqueado hasta
    /// confirmar la transacción, así dos reportes nunca reciben el mismo número.
    /// </summary>
    private async Task<string> SiguienteCodigoAsync(CancellationToken cancelacion)
    {
        var anio = HoraBolivia.Hoy().Year;
        var numero = await db.Database.SqlQuery<int>($"""
            INSERT INTO contadores_codigo (anio, ultimo) VALUES ({anio}, 1)
            ON CONFLICT (anio) DO UPDATE SET ultimo = contadores_codigo.ultimo + 1
            RETURNING ultimo AS "Value"
            """).ToListAsync(cancelacion);
        return $"FX-{anio}-{numero[0]:D6}";
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
