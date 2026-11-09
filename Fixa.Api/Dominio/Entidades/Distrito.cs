using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Fixa.Api.Dominio.Entidades;

public partial class Distrito
{
    public long Id { get; set; }

    public int Numero { get; set; }

    public string Nombre { get; set; } = null!;

    public Polygon Limite { get; set; } = null!;

    public virtual ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
