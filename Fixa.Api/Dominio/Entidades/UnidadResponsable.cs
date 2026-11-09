using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class UnidadResponsable
{
    public long Id { get; set; }

    public string Nombre { get; set; } = null!;

    public string Sigla { get; set; } = null!;

    public virtual ICollection<Caso> Casos { get; set; } = new List<Caso>();

    public virtual ICollection<Categoria> Categorias { get; set; } = new List<Categoria>();

    public virtual ICollection<Cuadrilla> Cuadrillas { get; set; } = new List<Cuadrilla>();

    public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
}
