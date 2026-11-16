using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Fixa.Api.Infraestructura.Seguridad;

/// <summary>
/// Los errores de validación llegan tal cual a la pantalla del vecino y del funcionario: se
/// escriben en español y sin detalles internos (tipos de C#, posiciones del JSON).
/// </summary>
public sealed class ValidacionEnEspanol : IValidationMetadataProvider, IDisplayMetadataProvider
{
    // Cómo se nombra cada campo en los mensajes. Los que no están aquí usan su propio nombre.
    private static readonly Dictionary<string, string> nombres = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Categoria"] = "categoría", ["Codigo"] = "código", ["Clave"] = "clave", ["Password"] = "contraseña",
        ["Correo"] = "correo", ["CorreoContacto"] = "correo de contacto", ["Descripcion"] = "descripción",
        ["Direccion"] = "dirección", ["Justificacion"] = "justificación", ["Observacion"] = "observación",
        ["TipoIncidencia"] = "tipo de incidencia", ["NumeroLuminarias"] = "número de luminarias",
        ["CodigoAvisoCobranza"] = "código del aviso de cobranza", ["MotivoSinFoto"] = "motivo de no tener foto",
        ["MotivoReprogramacion"] = "motivo de la reprogramación", ["TrabajoRealizado"] = "trabajo realizado",
        ["CuadrillaId"] = "cuadrilla", ["EntidadId"] = "entidad", ["CasoPrincipalId"] = "caso principal",
    };

    public void CreateDisplayMetadata(DisplayMetadataProviderContext contexto)
    {
        if (contexto.Key.Name is { } campo && nombres.TryGetValue(campo, out var nombre))
            contexto.DisplayMetadata.DisplayName = () => nombre;
    }

    public void CreateValidationMetadata(ValidationMetadataProviderContext contexto)
    {
        foreach (var regla in contexto.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
        {
            if (regla.ErrorMessageResourceName is not null)
                continue;

            regla.ErrorMessage = regla switch
            {
                RequiredAttribute => "El campo {0} es obligatorio.",
                EmailAddressAttribute => "El campo {0} no es un correo válido.",
                StringLengthAttribute { MinimumLength: > 0 } => "El campo {0} debe tener entre {2} y {1} caracteres.",
                StringLengthAttribute => "El campo {0} admite hasta {1} caracteres.",
                RangeAttribute => "El campo {0} debe estar entre {1} y {2}.",
                _ => regla.ErrorMessage,
            };
        }
    }

    public static IMvcBuilder AddControllersEnEspanol(IServiceCollection servicios) => servicios
        .AddControllers(mvc =>
        {
            mvc.ModelMetadataDetailsProviders.Add(new ValidacionEnEspanol());

            var mensajes = mvc.ModelBindingMessageProvider;
            mensajes.SetValueMustNotBeNullAccessor(_ => "El campo es obligatorio.");
            mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el campo {campo}.");
            mensajes.SetMissingKeyOrValueAccessor(() => "Falta un valor.");
            mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "Faltan los datos de la petición.");
            mensajes.SetValueMustBeANumberAccessor(campo => $"El campo {campo} debe ser un número.");
            mensajes.SetAttemptedValueIsInvalidAccessor((valor, campo) => $"El valor «{valor}» no sirve para {campo}.");
            mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor de {campo} no es válido.");
            mensajes.SetValueIsInvalidAccessor(valor => $"El valor «{valor}» no es válido.");
            mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor «{valor}» no es válido.");
            mensajes.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Los datos enviados no son válidos.");
            mensajes.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
        })
        // Un JSON mal formado responde un mensaje genérico, no el error interno del lector.
        .AddJsonOptions(json => json.AllowInputFormatterExceptionMessages = false)
        .ConfigureApiBehaviorOptions(api => api.InvalidModelStateResponseFactory = contexto =>
        {
            // Las claves que empiezan con $ vienen del lector de JSON: el cuerpo no se pudo leer.
            var errores = contexto.ModelState;
            if (errores.Keys.Any(clave => clave.StartsWith('$')))
            {
                errores = new();
                errores.AddModelError("datos", "Los datos enviados no tienen el formato esperado.");
            }

            return new BadRequestObjectResult(contexto.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()
                .CreateValidationProblemDetails(contexto.HttpContext, errores, title: "Revisa los datos enviados."));
        });
}
