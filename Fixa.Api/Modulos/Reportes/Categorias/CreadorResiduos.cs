namespace Fixa.Api.Modulos.Reportes.Categorias;

public class CreadorResiduos : CreadorCategoria
{
    protected override IDefinicionCategoria CrearDefinicion() => new DefinicionResiduos();
}