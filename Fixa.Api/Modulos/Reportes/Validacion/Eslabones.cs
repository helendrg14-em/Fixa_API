using System.Net.Mail;
using Fixa.Api.Modulos.Reportes.Categorias;

namespace Fixa.Api.Modulos.Reportes.Validacion;

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

public static class CadenasDeValidacion
{
    public static EslabonValidacion ParaReporteNuevo()
    {
        var primero = new ValidarDatosDeCategoria();
        primero.Luego(new ValidarClaveIdempotencia())
               .Luego(new ValidarDireccion())
               .Luego(new ValidarContacto());
        return primero;
    }

    public static EslabonValidacion ParaApoyo()
    {
        var primero = new ValidarClaveIdempotencia();
        primero.Luego(new ValidarContacto());
        return primero;
    }

    public static void Exigir(this EslabonValidacion cadena, DatosAValidar datos)
    {
        var errores = new List<string>();
        cadena.Validar(datos, errores);
        if (errores.Count > 0)
            throw new Dominio.ReglaNegocioException(string.Join(" ", errores));
    }
}
