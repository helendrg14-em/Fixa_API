using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Categoria
{
    public long Id { get; set; }

    public long UnidadId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public bool Activa { get; set; }

    public int RadioCercaniaM { get; set; }

    public string Codigo { get; set; } = null!;

    public virtual ICollection<Caso> Casos { get; set; } = new List<Caso>();

    public virtual ICollection<MetasAtencion> MetasAtencions { get; set; } = new List<MetasAtencion>();

    public virtual UnidadesResponsable Unidad { get; set; } = null!;
}
