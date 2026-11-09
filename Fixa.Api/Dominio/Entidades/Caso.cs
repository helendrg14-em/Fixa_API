using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Caso
{
    public long Id { get; set; }

    public long CategoriaId { get; set; }

    public long? UnidadId { get; set; }

    public string Estado { get; set; } = null!;

    public string? Prioridad { get; set; }

    public string? JustificacionPrioridad { get; set; }

    public bool RequiereVerificacionCampo { get; set; }

    public bool Vencido { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaObjetivo { get; set; }

    public DateTime? FechaCierre { get; set; }

    public virtual Categoria Categoria { get; set; } = null!;

    public virtual ICollection<Derivacione> Derivaciones { get; set; } = new List<Derivacione>();

    public virtual ICollection<EventosHistorial> EventosHistorials { get; set; } = new List<EventosHistorial>();

    public virtual ICollection<Intervencione> Intervenciones { get; set; } = new List<Intervencione>();

    public virtual ICollection<Programacione> Programaciones { get; set; } = new List<Programacione>();

    public virtual ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();

    public virtual UnidadesResponsable? Unidad { get; set; }
}
