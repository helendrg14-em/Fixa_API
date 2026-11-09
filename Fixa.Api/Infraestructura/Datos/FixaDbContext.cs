using System;
using System.Collections.Generic;
using Fixa.Api.Dominio.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Fixa.Api.Infraestructura.Datos;

public partial class FixaDbContext : DbContext
{
    public FixaDbContext(DbContextOptions<FixaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Caso> Casos { get; set; }

    public virtual DbSet<Categoria> Categorias { get; set; }

    public virtual DbSet<ContadoresCodigo> ContadoresCodigos { get; set; }

    public virtual DbSet<Cuadrilla> Cuadrillas { get; set; }

    public virtual DbSet<Derivacione> Derivaciones { get; set; }

    public virtual DbSet<Distrito> Distritos { get; set; }

    public virtual DbSet<EntidadesExterna> EntidadesExternas { get; set; }

    public virtual DbSet<EventosHistorial> EventosHistorials { get; set; }

    public virtual DbSet<Evidencia> Evidencias { get; set; }

    public virtual DbSet<Feriado> Feriados { get; set; }

    public virtual DbSet<FotosReporte> FotosReportes { get; set; }

    public virtual DbSet<Intervencione> Intervenciones { get; set; }

    public virtual DbSet<MetasAtencion> MetasAtencions { get; set; }

    public virtual DbSet<Notificacione> Notificaciones { get; set; }

    public virtual DbSet<Programacione> Programaciones { get; set; }

    public virtual DbSet<Reporte> Reportes { get; set; }

    public virtual DbSet<SolicitudesRevision> SolicitudesRevisions { get; set; }

    public virtual DbSet<UnidadesResponsable> UnidadesResponsables { get; set; }

    public virtual DbSet<Usuario> Usuarios { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasPostgresExtension("fuzzystrmatch")
            .HasPostgresExtension("postgis")
            .HasPostgresExtension("tiger", "postgis_tiger_geocoder")
            .HasPostgresExtension("topology", "postgis_topology");

        modelBuilder.Entity<Caso>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("casos_pkey");

            entity.ToTable("casos");

            entity.HasIndex(e => e.CategoriaId, "casos_categoria_id_index");

            entity.HasIndex(e => e.Estado, "casos_estado_index");

            entity.HasIndex(e => e.UnidadId, "casos_unidad_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.Estado)
                .HasMaxLength(25)
                .HasColumnName("estado");
            entity.Property(e => e.FechaCierre)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_cierre");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.FechaObjetivo)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_objetivo");
            entity.Property(e => e.JustificacionPrioridad).HasColumnName("justificacion_prioridad");
            entity.Property(e => e.Prioridad)
                .HasMaxLength(10)
                .HasColumnName("prioridad");
            entity.Property(e => e.RequiereVerificacionCampo).HasColumnName("requiere_verificacion_campo");
            entity.Property(e => e.UnidadId).HasColumnName("unidad_id");
            entity.Property(e => e.Vencido).HasColumnName("vencido");

            entity.HasOne(d => d.Categoria).WithMany(p => p.Casos)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("casos_categoria_id_foreign");

            entity.HasOne(d => d.Unidad).WithMany(p => p.Casos)
                .HasForeignKey(d => d.UnidadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("casos_unidad_id_foreign");
        });

        modelBuilder.Entity<Categoria>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("categorias_pkey");

            entity.ToTable("categorias");

            entity.HasIndex(e => e.Codigo, "categorias_codigo_unique").IsUnique();

            entity.HasIndex(e => e.Nombre, "categorias_nombre_unique").IsUnique();

            entity.HasIndex(e => e.UnidadId, "categorias_unidad_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activa)
                .HasDefaultValue(true)
                .HasColumnName("activa");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasColumnName("codigo");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Nombre)
                .HasMaxLength(60)
                .HasColumnName("nombre");
            entity.Property(e => e.RadioCercaniaM).HasColumnName("radio_cercania_m");
            entity.Property(e => e.UnidadId).HasColumnName("unidad_id");

            entity.HasOne(d => d.Unidad).WithMany(p => p.Categoria)
                .HasForeignKey(d => d.UnidadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("categorias_unidad_id_foreign");
        });

        modelBuilder.Entity<ContadoresCodigo>(entity =>
        {
            entity.HasKey(e => e.Anio).HasName("contadores_codigo_pkey");

            entity.ToTable("contadores_codigo");

            entity.Property(e => e.Anio)
                .ValueGeneratedNever()
                .HasColumnName("anio");
            entity.Property(e => e.Ultimo).HasColumnName("ultimo");
        });

        modelBuilder.Entity<Cuadrilla>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("cuadrillas_pkey");

            entity.ToTable("cuadrillas");

            entity.HasIndex(e => e.UnidadId, "cuadrillas_unidad_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(60)
                .HasColumnName("nombre");
            entity.Property(e => e.Turno)
                .HasMaxLength(10)
                .HasColumnName("turno");
            entity.Property(e => e.UnidadId).HasColumnName("unidad_id");

            entity.HasOne(d => d.Unidad).WithMany(p => p.Cuadrillas)
                .HasForeignKey(d => d.UnidadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("cuadrillas_unidad_id_foreign");
        });

        modelBuilder.Entity<Derivacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("derivaciones_pkey");

            entity.ToTable("derivaciones");

            entity.HasIndex(e => e.CasoId, "derivaciones_caso_id_index");

            entity.HasIndex(e => e.EntidadId, "derivaciones_entidad_id_index");

            entity.HasIndex(e => e.ResponsableId, "derivaciones_responsable_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CasoId).HasColumnName("caso_id");
            entity.Property(e => e.EntidadId).HasColumnName("entidad_id");
            entity.Property(e => e.EstadoSeguimiento)
                .HasMaxLength(25)
                .HasColumnName("estado_seguimiento");
            entity.Property(e => e.FechaDerivacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_derivacion");
            entity.Property(e => e.FechaRespuesta)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_respuesta");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.ResponsableId).HasColumnName("responsable_id");
            entity.Property(e => e.RespuestaEntidad).HasColumnName("respuesta_entidad");

            entity.HasOne(d => d.Caso).WithMany(p => p.Derivaciones)
                .HasForeignKey(d => d.CasoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("derivaciones_caso_id_foreign");

            entity.HasOne(d => d.Entidad).WithMany(p => p.Derivaciones)
                .HasForeignKey(d => d.EntidadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("derivaciones_entidad_id_foreign");

            entity.HasOne(d => d.Responsable).WithMany(p => p.Derivaciones)
                .HasForeignKey(d => d.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("derivaciones_responsable_id_foreign");
        });

        modelBuilder.Entity<Distrito>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("distritos_pkey");

            entity.ToTable("distritos");

            entity.HasIndex(e => e.Limite, "distritos_limite_spatialindex").HasMethod("gist");

            entity.HasIndex(e => e.Numero, "distritos_numero_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Limite)
                .HasColumnType("geometry(Polygon,4326)")
                .HasColumnName("limite");
            entity.Property(e => e.Nombre)
                .HasMaxLength(80)
                .HasColumnName("nombre");
            entity.Property(e => e.Numero).HasColumnName("numero");
        });

        modelBuilder.Entity<EntidadesExterna>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("entidades_externas_pkey");

            entity.ToTable("entidades_externas");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Contacto)
                .HasMaxLength(120)
                .HasColumnName("contacto");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<EventosHistorial>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("eventos_historial_pkey");

            entity.ToTable("eventos_historial");

            entity.HasIndex(e => e.CasoId, "eventos_historial_caso_id_index");

            entity.HasIndex(e => e.UsuarioId, "eventos_historial_usuario_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CasoId).HasColumnName("caso_id");
            entity.Property(e => e.Detalle).HasColumnName("detalle");
            entity.Property(e => e.EstadoAnterior)
                .HasMaxLength(25)
                .HasColumnName("estado_anterior");
            entity.Property(e => e.EstadoNuevo)
                .HasMaxLength(25)
                .HasColumnName("estado_nuevo");
            entity.Property(e => e.FechaHora)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_hora");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.UsuarioId).HasColumnName("usuario_id");

            entity.HasOne(d => d.Caso).WithMany(p => p.EventosHistorials)
                .HasForeignKey(d => d.CasoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("eventos_historial_caso_id_foreign");

            entity.HasOne(d => d.Usuario).WithMany(p => p.EventosHistorials)
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("eventos_historial_usuario_id_foreign");
        });

        modelBuilder.Entity<Evidencia>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("evidencias_pkey");

            entity.ToTable("evidencias");

            entity.HasIndex(e => e.IntervencionId, "evidencias_intervencion_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Descripcion)
                .HasMaxLength(200)
                .HasColumnName("descripcion");
            entity.Property(e => e.IntervencionId).HasColumnName("intervencion_id");
            entity.Property(e => e.Tipo)
                .HasMaxLength(15)
                .HasColumnName("tipo");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");

            entity.HasOne(d => d.Intervencion).WithMany(p => p.Evidencia)
                .HasForeignKey(d => d.IntervencionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("evidencias_intervencion_id_foreign");
        });

        modelBuilder.Entity<Feriado>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("feriados_pkey");

            entity.ToTable("feriados");

            entity.HasIndex(e => e.Fecha, "feriados_fecha_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Fecha).HasColumnName("fecha");
            entity.Property(e => e.Fuente)
                .HasMaxLength(200)
                .HasColumnName("fuente");
            entity.Property(e => e.Nombre)
                .HasMaxLength(100)
                .HasColumnName("nombre");
        });

        modelBuilder.Entity<FotosReporte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("fotos_reporte_pkey");

            entity.ToTable("fotos_reporte");

            entity.HasIndex(e => e.ReporteId, "fotos_reporte_reporte_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FechaCarga)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_carga");
            entity.Property(e => e.PublicId)
                .HasMaxLength(200)
                .HasColumnName("public_id");
            entity.Property(e => e.ReporteId).HasColumnName("reporte_id");
            entity.Property(e => e.Url)
                .HasMaxLength(500)
                .HasColumnName("url");

            entity.HasOne(d => d.Reporte).WithMany(p => p.FotosReportes)
                .HasForeignKey(d => d.ReporteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fotos_reporte_reporte_id_foreign");
        });

        modelBuilder.Entity<Intervencione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("intervenciones_pkey");

            entity.ToTable("intervenciones");

            entity.HasIndex(e => e.CasoId, "intervenciones_caso_id_index");

            entity.HasIndex(e => e.SupervisorId, "intervenciones_supervisor_id_index");

            entity.HasIndex(e => e.TecnicoId, "intervenciones_tecnico_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CasoId).HasColumnName("caso_id");
            entity.Property(e => e.EstadoValidacion)
                .HasMaxLength(15)
                .HasColumnName("estado_validacion");
            entity.Property(e => e.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha");
            entity.Property(e => e.ObservacionValidacion).HasColumnName("observacion_validacion");
            entity.Property(e => e.SupervisorId).HasColumnName("supervisor_id");
            entity.Property(e => e.TecnicoId).HasColumnName("tecnico_id");
            entity.Property(e => e.TrabajoRealizado).HasColumnName("trabajo_realizado");

            entity.HasOne(d => d.Caso).WithMany(p => p.Intervenciones)
                .HasForeignKey(d => d.CasoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("intervenciones_caso_id_foreign");

            entity.HasOne(d => d.Supervisor).WithMany(p => p.IntervencioneSupervisors)
                .HasForeignKey(d => d.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("intervenciones_supervisor_id_foreign");

            entity.HasOne(d => d.Tecnico).WithMany(p => p.IntervencioneTecnicos)
                .HasForeignKey(d => d.TecnicoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("intervenciones_tecnico_id_foreign");
        });

        modelBuilder.Entity<MetasAtencion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("metas_atencion_pkey");

            entity.ToTable("metas_atencion");

            entity.HasIndex(e => e.CategoriaId, "metas_atencion_categoria_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoriaId).HasColumnName("categoria_id");
            entity.Property(e => e.PlazoDiasHabiles).HasColumnName("plazo_dias_habiles");
            entity.Property(e => e.PlazoHoras).HasColumnName("plazo_horas");
            entity.Property(e => e.Prioridad)
                .HasMaxLength(10)
                .HasColumnName("prioridad");
            entity.Property(e => e.VigenteDesde).HasColumnName("vigente_desde");
            entity.Property(e => e.VigenteHasta).HasColumnName("vigente_hasta");

            entity.HasOne(d => d.Categoria).WithMany(p => p.MetasAtencions)
                .HasForeignKey(d => d.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("metas_atencion_categoria_id_foreign");
        });

        modelBuilder.Entity<Notificacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("notificaciones_pkey");

            entity.ToTable("notificaciones");

            entity.HasIndex(e => e.EventoId, "notificaciones_evento_id_index");

            entity.HasIndex(e => e.ReporteId, "notificaciones_reporte_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Canal)
                .HasMaxLength(10)
                .HasColumnName("canal");
            entity.Property(e => e.Destino)
                .HasMaxLength(255)
                .HasColumnName("destino");
            entity.Property(e => e.EstadoEntrega)
                .HasMaxLength(15)
                .HasColumnName("estado_entrega");
            entity.Property(e => e.EventoId).HasColumnName("evento_id");
            entity.Property(e => e.FechaEnvio)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_envio");
            entity.Property(e => e.ReporteId).HasColumnName("reporte_id");

            entity.HasOne(d => d.Evento).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.EventoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notificaciones_evento_id_foreign");

            entity.HasOne(d => d.Reporte).WithMany(p => p.Notificaciones)
                .HasForeignKey(d => d.ReporteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("notificaciones_reporte_id_foreign");
        });

        modelBuilder.Entity<Programacione>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("programaciones_pkey");

            entity.ToTable("programaciones");

            entity.HasIndex(e => e.CasoId, "programaciones_caso_id_index");

            entity.HasIndex(e => e.CuadrillaId, "programaciones_cuadrilla_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CasoId).HasColumnName("caso_id");
            entity.Property(e => e.CuadrillaId).HasColumnName("cuadrilla_id");
            entity.Property(e => e.FechaPrevista).HasColumnName("fecha_prevista");
            entity.Property(e => e.MotivoReprogramacion)
                .HasMaxLength(200)
                .HasColumnName("motivo_reprogramacion");
            entity.Property(e => e.Vigente)
                .HasDefaultValue(true)
                .HasColumnName("vigente");

            entity.HasOne(d => d.Caso).WithMany(p => p.Programaciones)
                .HasForeignKey(d => d.CasoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("programaciones_caso_id_foreign");

            entity.HasOne(d => d.Cuadrilla).WithMany(p => p.Programaciones)
                .HasForeignKey(d => d.CuadrillaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("programaciones_cuadrilla_id_foreign");
        });

        modelBuilder.Entity<Reporte>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("reportes_pkey");

            entity.ToTable("reportes");

            entity.HasIndex(e => e.CasoId, "reportes_caso_id_index");

            entity.HasIndex(e => e.ClaveIdempotencia, "reportes_clave_idempotencia_unique").IsUnique();

            entity.HasIndex(e => e.Codigo, "reportes_codigo_unique").IsUnique();

            entity.HasIndex(e => e.DistritoId, "reportes_distrito_id_index");

            entity.HasIndex(e => e.OperadorId, "reportes_operador_id_index");

            entity.HasIndex(e => e.Ubicacion, "reportes_ubicacion_spatialindex").HasMethod("gist");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CanalOrigen)
                .HasMaxLength(25)
                .HasColumnName("canal_origen");
            entity.Property(e => e.CasoId).HasColumnName("caso_id");
            entity.Property(e => e.ClaveHash)
                .HasMaxLength(255)
                .HasColumnName("clave_hash");
            entity.Property(e => e.ClaveIdempotencia)
                .HasMaxLength(64)
                .HasColumnName("clave_idempotencia");
            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .HasColumnName("codigo");
            entity.Property(e => e.CodigoAvisoCobranza)
                .HasMaxLength(30)
                .HasColumnName("codigo_aviso_cobranza");
            entity.Property(e => e.Conformidad).HasColumnName("conformidad");
            entity.Property(e => e.CorreoContacto)
                .HasMaxLength(120)
                .HasColumnName("correo_contacto");
            entity.Property(e => e.Descripcion).HasColumnName("descripcion");
            entity.Property(e => e.Direccion)
                .HasMaxLength(200)
                .HasColumnName("direccion");
            entity.Property(e => e.DistritoId).HasColumnName("distrito_id");
            entity.Property(e => e.EsApoyo).HasColumnName("es_apoyo");
            entity.Property(e => e.FechaConformidad)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_conformidad");
            entity.Property(e => e.FechaRegistro)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_registro");
            entity.Property(e => e.MotivoSinFoto)
                .HasMaxLength(200)
                .HasColumnName("motivo_sin_foto");
            entity.Property(e => e.NumeroLuminarias).HasColumnName("numero_luminarias");
            entity.Property(e => e.OperadorId).HasColumnName("operador_id");
            entity.Property(e => e.Referencia)
                .HasMaxLength(200)
                .HasColumnName("referencia");
            entity.Property(e => e.TipoIncidencia)
                .HasMaxLength(30)
                .HasColumnName("tipo_incidencia");
            entity.Property(e => e.TokenDispositivo)
                .HasMaxLength(255)
                .HasColumnName("token_dispositivo");
            entity.Property(e => e.Ubicacion)
                .HasColumnType("geometry(Point,4326)")
                .HasColumnName("ubicacion");

            entity.HasOne(d => d.Caso).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.CasoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("reportes_caso_id_foreign");

            entity.HasOne(d => d.Distrito).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.DistritoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("reportes_distrito_id_foreign");

            entity.HasOne(d => d.Operador).WithMany(p => p.Reportes)
                .HasForeignKey(d => d.OperadorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("reportes_operador_id_foreign");
        });

        modelBuilder.Entity<SolicitudesRevision>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("solicitudes_revision_pkey");

            entity.ToTable("solicitudes_revision");

            entity.HasIndex(e => e.ReporteId, "solicitudes_revision_reporte_id_index");

            entity.HasIndex(e => e.SupervisorId, "solicitudes_revision_supervisor_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(15)
                .HasColumnName("estado");
            entity.Property(e => e.FechaResolucion)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_resolucion");
            entity.Property(e => e.FechaSolicitud)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_solicitud");
            entity.Property(e => e.Motivo).HasColumnName("motivo");
            entity.Property(e => e.ReporteId).HasColumnName("reporte_id");
            entity.Property(e => e.Respuesta).HasColumnName("respuesta");
            entity.Property(e => e.SupervisorId).HasColumnName("supervisor_id");

            entity.HasOne(d => d.Reporte).WithMany(p => p.SolicitudesRevisions)
                .HasForeignKey(d => d.ReporteId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("solicitudes_revision_reporte_id_foreign");

            entity.HasOne(d => d.Supervisor).WithMany(p => p.SolicitudesRevisions)
                .HasForeignKey(d => d.SupervisorId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("solicitudes_revision_supervisor_id_foreign");
        });

        modelBuilder.Entity<UnidadesResponsable>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("unidades_responsables_pkey");

            entity.ToTable("unidades_responsables");

            entity.HasIndex(e => e.Sigla, "unidades_responsables_sigla_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(120)
                .HasColumnName("nombre");
            entity.Property(e => e.Sigla)
                .HasMaxLength(20)
                .HasColumnName("sigla");
        });

        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("usuarios_pkey");

            entity.ToTable("usuarios");

            entity.HasIndex(e => e.Correo, "usuarios_correo_unique").IsUnique();

            entity.HasIndex(e => e.CuadrillaId, "usuarios_cuadrilla_id_index");

            entity.HasIndex(e => e.DistritoId, "usuarios_distrito_id_index");

            entity.HasIndex(e => e.UnidadId, "usuarios_unidad_id_index");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Activo)
                .HasDefaultValue(true)
                .HasColumnName("activo");
            entity.Property(e => e.Apellidos)
                .HasMaxLength(80)
                .HasColumnName("apellidos");
            entity.Property(e => e.Cargo)
                .HasMaxLength(80)
                .HasColumnName("cargo");
            entity.Property(e => e.Codigo2faExpira)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("codigo_2fa_expira");
            entity.Property(e => e.Codigo2faHash)
                .HasMaxLength(255)
                .HasColumnName("codigo_2fa_hash");
            entity.Property(e => e.Correo)
                .HasMaxLength(120)
                .HasColumnName("correo");
            entity.Property(e => e.CuadrillaId).HasColumnName("cuadrilla_id");
            entity.Property(e => e.DebeRegistrarPassword)
                .HasDefaultValue(true)
                .HasColumnName("debe_registrar_password");
            entity.Property(e => e.DistritoId).HasColumnName("distrito_id");
            entity.Property(e => e.Especialidad)
                .HasMaxLength(80)
                .HasColumnName("especialidad");
            entity.Property(e => e.FechaCreacion)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("fecha_creacion");
            entity.Property(e => e.Nombres)
                .HasMaxLength(80)
                .HasColumnName("nombres");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.PuntoAtencion)
                .HasMaxLength(80)
                .HasColumnName("punto_atencion");
            entity.Property(e => e.Requiere2fa).HasColumnName("requiere_2fa");
            entity.Property(e => e.Tipo)
                .HasMaxLength(20)
                .HasColumnName("tipo");
            entity.Property(e => e.TokenExpira)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("token_expira");
            entity.Property(e => e.TokenRecuperacionHash)
                .HasMaxLength(255)
                .HasColumnName("token_recuperacion_hash");
            entity.Property(e => e.UltimoAcceso)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("ultimo_acceso");
            entity.Property(e => e.UnidadId).HasColumnName("unidad_id");

            entity.HasOne(d => d.Cuadrilla).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.CuadrillaId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("usuarios_cuadrilla_id_foreign");

            entity.HasOne(d => d.Distrito).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.DistritoId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("usuarios_distrito_id_foreign");

            entity.HasOne(d => d.Unidad).WithMany(p => p.Usuarios)
                .HasForeignKey(d => d.UnidadId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("usuarios_unidad_id_foreign");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
