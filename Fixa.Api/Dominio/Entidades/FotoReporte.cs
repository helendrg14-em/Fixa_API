using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class FotoReporte
{
    public long Id { get; set; }

    public long ReporteId { get; set; }

    public string Url { get; set; } = null!;

    public string PublicId { get; set; } = null!;

    public DateTime FechaCarga { get; set; }

    public virtual Reporte Reporte { get; set; } = null!;
}
