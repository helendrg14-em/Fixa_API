using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Notificacione
{
    public long Id { get; set; }

    public long EventoId { get; set; }

    public long ReporteId { get; set; }

    public string Canal { get; set; } = null!;

    public string Destino { get; set; } = null!;

    public DateTime? FechaEnvio { get; set; }

    public string EstadoEntrega { get; set; } = null!;

    public virtual EventosHistorial Evento { get; set; } = null!;

    public virtual Reporte Reporte { get; set; } = null!;
}
