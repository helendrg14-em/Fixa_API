using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class EventoHistorial
{
    public long Id { get; set; }

    public long CasoId { get; set; }

    public long? UsuarioId { get; set; }

    public DateTime FechaHora { get; set; }

    public string Tipo { get; set; } = null!;

    public string? EstadoAnterior { get; set; }

    public string? EstadoNuevo { get; set; }

    public string Detalle { get; set; } = null!;

    public string? Motivo { get; set; }

    public virtual Caso Caso { get; set; } = null!;

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual Usuario? Usuario { get; set; }
}
