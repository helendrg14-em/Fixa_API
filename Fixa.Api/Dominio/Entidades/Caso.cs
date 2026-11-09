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

    public virtual ICollection<Derivacion> Derivaciones { get; set; } = new List<Derivacion>();

    public virtual ICollection<EventoHistorial> EventosHistorial { get; set; } = new List<EventoHistorial>();

    public virtual ICollection<Intervencion> Intervenciones { get; set; } = new List<Intervencion>();

    public virtual ICollection<Programacion> Programaciones { get; set; } = new List<Programacion>();

    public virtual ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();

    public virtual UnidadResponsable? Unidad { get; set; }
}
