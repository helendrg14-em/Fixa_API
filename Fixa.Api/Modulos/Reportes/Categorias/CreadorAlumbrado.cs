namespace Fixa.Api.Modulos.Reportes.Categorias;

public class CreadorAlumbrado : CreadorCategoria
{
    protected override IDefinicionCategoria CrearDefinicion() => new DefinicionAlumbrado();
}