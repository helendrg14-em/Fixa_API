using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class SolicitudesRevision
{
    public long Id { get; set; }

    public long ReporteId { get; set; }

    public long? SupervisorId { get; set; }

    public DateTime FechaSolicitud { get; set; }

    public string Motivo { get; set; } = null!;

    public string Estado { get; set; } = null!;

    public string? Respuesta { get; set; }

    public DateTime? FechaResolucion { get; set; }

    public virtual Reporte Reporte { get; set; } = null!;

    public virtual Usuario? Supervisor { get; set; }
}
