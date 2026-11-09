using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Intervencion
{
    public long Id { get; set; }

    public long CasoId { get; set; }

    public long TecnicoId { get; set; }

    public long? SupervisorId { get; set; }

    public DateTime Fecha { get; set; }

    public string TrabajoRealizado { get; set; } = null!;

    public string EstadoValidacion { get; set; } = null!;

    public string? ObservacionValidacion { get; set; }

    public virtual Caso Caso { get; set; } = null!;

    public virtual ICollection<Evidencia> Evidencias { get; set; } = new List<Evidencia>();

    public virtual Usuario? Supervisor { get; set; }

    public virtual Usuario Tecnico { get; set; } = null!;
}
