using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class EntidadesExterna
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string? Contacto { get; set; }

    public virtual ICollection<Derivacione> Derivaciones { get; set; } = new List<Derivacione>();
}
