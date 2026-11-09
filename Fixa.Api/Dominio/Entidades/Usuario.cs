using System;
using System.Collections.Generic;

namespace Fixa.Api.Dominio.Entidades;

public partial class Usuario
{
    public long Id { get; set; }

    public string Tipo { get; set; } = null!;

    public string Nombres { get; set; } = null!;

    public string Apellidos { get; set; } = null!;

    public string Correo { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public bool Activo { get; set; }

    public bool DebeRegistrarPassword { get; set; }

    public bool Requiere2fa { get; set; }

    public string? Codigo2faHash { get; set; }

    public DateTime? Codigo2faExpira { get; set; }

    public string? TokenRecuperacionHash { get; set; }

    public DateTime? TokenExpira { get; set; }

    public DateTime? UltimoAcceso { get; set; }

    public DateTime FechaCreacion { get; set; }

    public string? PuntoAtencion { get; set; }

    public long? DistritoId { get; set; }

    public string? Cargo { get; set; }

    public long? UnidadId { get; set; }

    public string? Especialidad { get; set; }

    public long? CuadrillaId { get; set; }

    public virtual Cuadrilla? Cuadrilla { get; set; }

    public virtual ICollection<Derivacion> Derivaciones { get; set; } = new List<Derivacion>();

    public virtual Distrito? Distrito { get; set; }

    public virtual ICollection<EventoHistorial> EventosHistorial { get; set; } = new List<EventoHistorial>();

    public virtual ICollection<Intervencion> IntervencionesComoSupervisor { get; set; } = new List<Intervencion>();

    public virtual ICollection<Intervencion> IntervencionesComoTecnico { get; set; } = new List<Intervencion>();

    public virtual ICollection<Reporte> Reportes { get; set; } = new List<Reporte>();

    public virtual ICollection<SolicitudRevision> SolicitudesRevision { get; set; } = new List<SolicitudRevision>();

    public virtual UnidadResponsable? Unidad { get; set; }
}
