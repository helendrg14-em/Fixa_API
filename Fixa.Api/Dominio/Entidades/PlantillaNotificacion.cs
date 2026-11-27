namespace Fixa.Api.Dominio.Entidades;

/// <summary>
/// Texto del aviso que recibe el vecino cuando su caso llega a un estado. El administrador lo
/// edita desde la API; {codigo} y {estado} se reemplazan al enviar.
/// </summary>
public partial class PlantillaNotificacion
{
    public long Id { get; set; }

    public string Estado { get; set; } = null!;

    public string Titulo { get; set; } = null!;

    public string Cuerpo { get; set; } = null!;

    public bool Activa { get; set; }

    public DateTime FechaActualizacion { get; set; }
}
