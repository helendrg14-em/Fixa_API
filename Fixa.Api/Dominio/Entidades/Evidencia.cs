using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Evidencia
{
    public long Id { get; set; }

    public long IntervencionId { get; set; }

    public string Tipo { get; set; } = null!;

    public string Url { get; set; } = null!;

    public string? Descripcion { get; set; }

    public virtual Intervencione Intervencion { get; set; } = null!;
}
