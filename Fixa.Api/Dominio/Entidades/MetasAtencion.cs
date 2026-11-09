using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class MetasAtencion
{
    public long Id { get; set; }

    public long CategoriaId { get; set; }

    public string Prioridad { get; set; } = null!;

    public int? PlazoHoras { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public int? PlazoDiasHabiles { get; set; }

    public virtual Categoria Categoria { get; set; } = null!;
}
