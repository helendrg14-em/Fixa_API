namespace Fixa.Api.Modulos.Reportes.Categorias;

public class CreadorBache : CreadorCategoria
{
    protected override IDefinicionCategoria CrearDefinicion() => new DefinicionBache();
}