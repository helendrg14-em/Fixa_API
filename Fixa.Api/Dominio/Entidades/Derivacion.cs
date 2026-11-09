using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Derivacion
{
    public long Id { get; set; }

    public long CasoId { get; set; }

    public long EntidadId { get; set; }

    public long ResponsableId { get; set; }

    public string Motivo { get; set; } = null!;

    public DateTime FechaDerivacion { get; set; }

    public string EstadoSeguimiento { get; set; } = null!;

    public string? RespuestaEntidad { get; set; }

    public DateTime? FechaRespuesta { get; set; }

    public virtual Caso Caso { get; set; } = null!;

    public virtual EntidadExterna Entidad { get; set; } = null!;

    public virtual Usuario Responsable { get; set; } = null!;
}
