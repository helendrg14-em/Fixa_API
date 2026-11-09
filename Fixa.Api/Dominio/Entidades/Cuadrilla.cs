using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Cuadrilla
{
    public long Id { get; set; }

    public long UnidadId { get; set; }

    public string Nombre { get; set; } = null!;

    public string Turno { get; set; } = null!;

    public virtual ICollection<Programacione> Programaciones { get; set; } = new List<Programacione>();

    public virtual UnidadesResponsable Unidad { get; set; } = null!;

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
