using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Programacion
{
    public long Id { get; set; }

    public long CasoId { get; set; }

    public long CuadrillaId { get; set; }

    public DateOnly FechaPrevista { get; set; }

    public string? MotivoReprogramacion { get; set; }

    public bool Vigente { get; set; }

    public virtual Caso Caso { get; set; } = null!;

    public virtual Cuadrilla Cuadrilla { get; set; } = null!;
}
