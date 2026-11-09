using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Feriado
{
    public long Id { get; set; }

    public DateOnly Fecha { get; set; }

    public string Nombre { get; set; } = null!;

    public string Fuente { get; set; } = null!;
}
