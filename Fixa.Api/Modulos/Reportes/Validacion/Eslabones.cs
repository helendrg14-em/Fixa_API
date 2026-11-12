using System.Net.Mail;
using Fixa.Api.Modulos.Reportes.Categorias;

namespace Fixa.Api.Modulos.Reportes.Validacion;

// Patrón Chain of Responsibility — Manejadores concretos. Cada uno revisa una sola regla.

/// <summary>Ubicación, tipo, descripción y datos propios de la categoría (usa el Factory Method).</summary>
public sealed class ValidarDatosDeCategoria : EslabonValidacion
{
    protected override void Revisar(DatosAValidar datos, List<string> errores)
    {
        if (datos.Categoria is not { } categoria || datos.Reporte is null)
        {
            errores.Add("Elige una categoría válida.");
            return;
        }
        errores.AddRange(CreadorCategoria.Para(categoria).Validar(datos.Reporte));
    }
}

/// <summary>
/// La clave de idempotencia la genera la app una vez por envío: si el celular reenvía la misma
/// solicitud (por ejemplo, porque se cortó la conexión), no se crea un segundo caso.
/// </summary>
public sealed class ValidarClaveIdempotencia : EslabonValidacion
{
    public const int LargoMinimo = 8, LargoMaximo = 64;

    protected override void Revisar(DatosAValidar datos, List<string> errores)
    {
        var clave = datos.ClaveIdempotencia;
        var formatoValido = clave is not null
            && clave.Length is >= LargoMinimo and <= LargoMaximo
            && clave.All(caracter => char.IsAsciiLetterOrDigit(caracter) || caracter == '-');
        if (!formatoValido)
            errores.Add($"Falta la clave de idempotencia ({LargoMinimo} a {LargoMaximo} letras, números o guiones).");
    }
}

public sealed class ValidarDireccion : EslabonValidacion
{
    public const int LargoMaximo = 200;

    protected override void Revisar(DatosAValidar datos, List<string> errores)
    {
        if (datos.Direccion?.Length > LargoMaximo || datos.Referencia?.Length > LargoMaximo)
            errores.Add($"La dirección y la referencia tienen como máximo {LargoMaximo} caracteres.");
    }
}

/// <summary>Correo y token del celular son opcionales; si vienen, deben tener un formato válido.</summary>
public sealed class ValidarContacto : EslabonValidacion
{
    public const int LargoMaximoCorreo = 120, LargoMaximoToken = 255;

    protected override void Revisar(DatosAValidar datos, List<string> errores)
    {
        var correo = datos.CorreoContacto;
        if (correo is not null && (correo.Length > LargoMaximoCorreo || !MailAddress.TryCreate(correo, out _)))
            errores.Add("El correo de contacto no es válido.");
        if (datos.TokenDispositivo?.Length > LargoMaximoToken)
            errores.Add("El identificador del dispositivo no es válido.");
    }
}

/// <summary>Arma las cadenas de cada flujo en un solo lugar.</summary>
public static class CadenasDeValidacion
{
    /// <summary>Reporte nuevo: categoría → idempotencia → dirección → contacto.</summary>
    public static EslabonValidacion ParaReporteNuevo()
    {
        var primero = new ValidarDatosDeCategoria();
        primero.Luego(new ValidarClaveIdempotencia())
               .Luego(new ValidarDireccion())
               .Luego(new ValidarContacto());
        return primero;
    }

    /// <summary>Apoyo a un caso existente: solo idempotencia → contacto (el caso ya tiene sus datos).</summary>
    public static EslabonValidacion ParaApoyo()
    {
        var primero = new ValidarClaveIdempotencia();
        primero.Luego(new ValidarContacto());
        return primero;
    }

    /// <summary>Corre la cadena y, si hubo errores, los devuelve todos juntos como regla de negocio (422).</summary>
    public static void Exigir(this EslabonValidacion cadena, DatosAValidar datos)
    {
        var errores = new List<string>();
        cadena.Validar(datos, errores);
        if (errores.Count > 0)
            throw new Dominio.ReglaNegocioException(string.Join(" ", errores));
    }
}
