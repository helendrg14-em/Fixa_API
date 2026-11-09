using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace Fixa.Api.Dominio.Entidades;

public partial class Reporte
{
    public long Id { get; set; }

    public long CasoId { get; set; }

    public long DistritoId { get; set; }

    public long? OperadorId { get; set; }

    public string Codigo { get; set; } = null!;

    public string ClaveHash { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public string CanalOrigen { get; set; } = null!;

    public Point Ubicacion { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Referencia { get; set; }

    public string? CodigoAvisoCobranza { get; set; }

    public string? MotivoSinFoto { get; set; }

    public string? CorreoContacto { get; set; }

    public string? TokenDispositivo { get; set; }

    public string ClaveIdempotencia { get; set; } = null!;

    public bool EsApoyo { get; set; }

    public bool? Conformidad { get; set; }

    public DateTime? FechaConformidad { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string TipoIncidencia { get; set; } = null!;

    public int? NumeroLuminarias { get; set; }

    public virtual Caso Caso { get; set; } = null!;

    public virtual Distrito Distrito { get; set; } = null!;

    public virtual ICollection<FotoReporte> FotosReporte { get; set; } = new List<FotoReporte>();

    public virtual ICollection<Notificacion> Notificaciones { get; set; } = new List<Notificacion>();

    public virtual Usuario? Operador { get; set; }

    public virtual ICollection<SolicitudRevision> SolicitudesRevision { get; set; } = new List<SolicitudRevision>();
}
