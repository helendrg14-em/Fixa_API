namespace Fixa.Api.Tests.Infraestructura;

/// <summary>Todas las clases de prueba comparten una sola base y una sola API por ejecución.</summary>
[CollectionDefinition(Nombre)]
public sealed class ColeccionApi : ICollectionFixture<EntornoPruebas>
{
    public const string Nombre = "api";
}
