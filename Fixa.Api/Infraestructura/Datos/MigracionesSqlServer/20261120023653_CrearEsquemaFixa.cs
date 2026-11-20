using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fixa.Api.Infraestructura.Datos.MigracionesSqlServer
{
    /// <inheritdoc />
    public partial class CrearEsquemaFixa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contadores_codigo",
                columns: table => new
                {
                    anio = table.Column<int>(type: "int", nullable: false),
                    ultimo = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("contadores_codigo_pkey", x => x.anio);
                });

            migrationBuilder.CreateTable(
                name: "distritos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    numero = table.Column<int>(type: "int", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    limite = table.Column<Polygon>(type: "geography", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("distritos_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "entidades_externas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    contacto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("entidades_externas_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "feriados",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    fuente = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("feriados_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "unidades_responsables",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    sigla = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("unidades_responsables_pkey", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    unidad_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    activa = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    radio_cercania_m = table.Column<int>(type: "int", nullable: false),
                    codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("categorias_pkey", x => x.id);
                    table.ForeignKey(
                        name: "categorias_unidad_id_foreign",
                        column: x => x.unidad_id,
                        principalTable: "unidades_responsables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cuadrillas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    unidad_id = table.Column<long>(type: "bigint", nullable: false),
                    nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    turno = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("cuadrillas_pkey", x => x.id);
                    table.ForeignKey(
                        name: "cuadrillas_unidad_id_foreign",
                        column: x => x.unidad_id,
                        principalTable: "unidades_responsables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "casos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    categoria_id = table.Column<long>(type: "bigint", nullable: false),
                    unidad_id = table.Column<long>(type: "bigint", nullable: true),
                    estado = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    prioridad = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    justificacion_prioridad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    requiere_verificacion_campo = table.Column<bool>(type: "bit", nullable: false),
                    vencido = table.Column<bool>(type: "bit", nullable: false),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    fecha_objetivo = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    fecha_cierre = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("casos_pkey", x => x.id);
                    table.ForeignKey(
                        name: "casos_categoria_id_foreign",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "casos_unidad_id_foreign",
                        column: x => x.unidad_id,
                        principalTable: "unidades_responsables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "metas_atencion",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    categoria_id = table.Column<long>(type: "bigint", nullable: false),
                    prioridad = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    plazo_horas = table.Column<int>(type: "int", nullable: true),
                    vigente_desde = table.Column<DateOnly>(type: "date", nullable: false),
                    vigente_hasta = table.Column<DateOnly>(type: "date", nullable: true),
                    plazo_dias_habiles = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("metas_atencion_pkey", x => x.id);
                    table.ForeignKey(
                        name: "metas_atencion_categoria_id_foreign",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    nombres = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    apellidos = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    correo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    debe_registrar_password = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    requiere_2fa = table.Column<bool>(type: "bit", nullable: false),
                    codigo_2fa_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    codigo_2fa_expira = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    token_recuperacion_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    token_expira = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    ultimo_acceso = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    fecha_creacion = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    punto_atencion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    distrito_id = table.Column<long>(type: "bigint", nullable: true),
                    cargo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    unidad_id = table.Column<long>(type: "bigint", nullable: true),
                    especialidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    cuadrilla_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("usuarios_pkey", x => x.id);
                    table.ForeignKey(
                        name: "usuarios_cuadrilla_id_foreign",
                        column: x => x.cuadrilla_id,
                        principalTable: "cuadrillas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "usuarios_distrito_id_foreign",
                        column: x => x.distrito_id,
                        principalTable: "distritos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "usuarios_unidad_id_foreign",
                        column: x => x.unidad_id,
                        principalTable: "unidades_responsables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "programaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    caso_id = table.Column<long>(type: "bigint", nullable: false),
                    cuadrilla_id = table.Column<long>(type: "bigint", nullable: false),
                    fecha_prevista = table.Column<DateOnly>(type: "date", nullable: false),
                    motivo_reprogramacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    vigente = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("programaciones_pkey", x => x.id);
                    table.ForeignKey(
                        name: "programaciones_caso_id_foreign",
                        column: x => x.caso_id,
                        principalTable: "casos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "programaciones_cuadrilla_id_foreign",
                        column: x => x.cuadrilla_id,
                        principalTable: "cuadrillas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "derivaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    caso_id = table.Column<long>(type: "bigint", nullable: false),
                    entidad_id = table.Column<long>(type: "bigint", nullable: false),
                    responsable_id = table.Column<long>(type: "bigint", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    fecha_derivacion = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    estado_seguimiento = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    respuesta_entidad = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_respuesta = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("derivaciones_pkey", x => x.id);
                    table.ForeignKey(
                        name: "derivaciones_caso_id_foreign",
                        column: x => x.caso_id,
                        principalTable: "casos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "derivaciones_entidad_id_foreign",
                        column: x => x.entidad_id,
                        principalTable: "entidades_externas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "derivaciones_responsable_id_foreign",
                        column: x => x.responsable_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "eventos_historial",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    caso_id = table.Column<long>(type: "bigint", nullable: false),
                    usuario_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_hora = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    estado_anterior = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    estado_nuevo = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    detalle = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    motivo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("eventos_historial_pkey", x => x.id);
                    table.ForeignKey(
                        name: "eventos_historial_caso_id_foreign",
                        column: x => x.caso_id,
                        principalTable: "casos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "eventos_historial_usuario_id_foreign",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "intervenciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    caso_id = table.Column<long>(type: "bigint", nullable: false),
                    tecnico_id = table.Column<long>(type: "bigint", nullable: false),
                    supervisor_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    trabajo_realizado = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado_validacion = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    observacion_validacion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("intervenciones_pkey", x => x.id);
                    table.ForeignKey(
                        name: "intervenciones_caso_id_foreign",
                        column: x => x.caso_id,
                        principalTable: "casos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "intervenciones_supervisor_id_foreign",
                        column: x => x.supervisor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "intervenciones_tecnico_id_foreign",
                        column: x => x.tecnico_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reportes",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    caso_id = table.Column<long>(type: "bigint", nullable: false),
                    distrito_id = table.Column<long>(type: "bigint", nullable: false),
                    operador_id = table.Column<long>(type: "bigint", nullable: true),
                    codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    clave_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    canal_origen = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: false),
                    ubicacion = table.Column<Point>(type: "geography", nullable: false),
                    direccion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    referencia = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    codigo_aviso_cobranza = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    motivo_sin_foto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    correo_contacto = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    token_dispositivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    clave_idempotencia = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    es_apoyo = table.Column<bool>(type: "bit", nullable: false),
                    conformidad = table.Column<bool>(type: "bit", nullable: true),
                    fecha_conformidad = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    fecha_registro = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    tipo_incidencia = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    numero_luminarias = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("reportes_pkey", x => x.id);
                    table.ForeignKey(
                        name: "reportes_caso_id_foreign",
                        column: x => x.caso_id,
                        principalTable: "casos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "reportes_distrito_id_foreign",
                        column: x => x.distrito_id,
                        principalTable: "distritos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "reportes_operador_id_foreign",
                        column: x => x.operador_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evidencias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    intervencion_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    descripcion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("evidencias_pkey", x => x.id);
                    table.ForeignKey(
                        name: "evidencias_intervencion_id_foreign",
                        column: x => x.intervencion_id,
                        principalTable: "intervenciones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fotos_reporte",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reporte_id = table.Column<long>(type: "bigint", nullable: false),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    public_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    fecha_carga = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("fotos_reporte_pkey", x => x.id);
                    table.ForeignKey(
                        name: "fotos_reporte_reporte_id_foreign",
                        column: x => x.reporte_id,
                        principalTable: "reportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    evento_id = table.Column<long>(type: "bigint", nullable: false),
                    reporte_id = table.Column<long>(type: "bigint", nullable: false),
                    canal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    destino = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    fecha_envio = table.Column<DateTime>(type: "datetime2(0)", nullable: true),
                    estado_entrega = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("notificaciones_pkey", x => x.id);
                    table.ForeignKey(
                        name: "notificaciones_evento_id_foreign",
                        column: x => x.evento_id,
                        principalTable: "eventos_historial",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "notificaciones_reporte_id_foreign",
                        column: x => x.reporte_id,
                        principalTable: "reportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes_revision",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    reporte_id = table.Column<long>(type: "bigint", nullable: false),
                    supervisor_id = table.Column<long>(type: "bigint", nullable: true),
                    fecha_solicitud = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    motivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    estado = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    respuesta = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    fecha_resolucion = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("solicitudes_revision_pkey", x => x.id);
                    table.ForeignKey(
                        name: "solicitudes_revision_reporte_id_foreign",
                        column: x => x.reporte_id,
                        principalTable: "reportes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "solicitudes_revision_supervisor_id_foreign",
                        column: x => x.supervisor_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "distritos",
                columns: new[] { "id", "limite", "nombre", "numero" },
                values: new object[,]
                {
                    { 1L, (NetTopologySuite.Geometries.Polygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POLYGON ((-63.2 -17.8, -63.16 -17.8, -63.16 -17.76, -63.2 -17.76, -63.2 -17.8))"), "Distrito 1 (prueba)", 1 },
                    { 2L, (NetTopologySuite.Geometries.Polygon)new NetTopologySuite.IO.WKTReader().Read("SRID=4326;POLYGON ((-63.16 -17.8, -63.12 -17.8, -63.12 -17.76, -63.16 -17.76, -63.16 -17.8))"), "Distrito 2 (prueba)", 2 }
                });

            migrationBuilder.InsertData(
                table: "entidades_externas",
                columns: new[] { "id", "contacto", "nombre" },
                values: new object[] { 1L, "Línea de reclamos", "CRE (distribución eléctrica)" });

            migrationBuilder.InsertData(
                table: "feriados",
                columns: new[] { "id", "fecha", "fuente", "nombre" },
                values: new object[] { 1L, new DateOnly(2026, 12, 25), "Datos de prueba", "Navidad" });

            migrationBuilder.InsertData(
                table: "unidades_responsables",
                columns: new[] { "id", "nombre", "sigla" },
                values: new object[,]
                {
                    { 1L, "Secretaría de Obras Públicas (vialidad)", "SMOP" },
                    { 2L, "Dirección de Alumbrado Público", "DAP" },
                    { 3L, "Empresa Municipal de Aseo", "EMACRUZ" }
                });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "activo", "apellidos", "cargo", "codigo_2fa_expira", "codigo_2fa_hash", "correo", "cuadrilla_id", "distrito_id", "especialidad", "fecha_creacion", "nombres", "password_hash", "punto_atencion", "requiere_2fa", "tipo", "token_expira", "token_recuperacion_hash", "ultimo_acceso", "unidad_id" },
                values: new object[] { 1L, true, "Administradora", null, null, null, "admin@fixa.test", null, null, null, new DateTime(2026, 11, 20, 8, 0, 0, 0, DateTimeKind.Unspecified), "Ana", "$2a$12$uAa4dbkixagNbb2.EatC9eABiTh9zyYAoE8U9Uj2BIj1KLPI8l.Tq", null, false, "administrador", null, null, null, null });

            migrationBuilder.InsertData(
                table: "categorias",
                columns: new[] { "id", "activa", "codigo", "descripcion", "nombre", "radio_cercania_m", "unidad_id" },
                values: new object[,]
                {
                    { 1L, true, "bache", "Baches, hundimientos y calzada dañada.", "Baches y deterioro vial", 30, 1L },
                    { 2L, true, "alumbrado", "Luminarias apagadas, intermitentes o dañadas.", "Fallas de alumbrado público", 30, 2L },
                    { 3L, true, "residuos", "Acumulación de basura en la vía pública.", "Microbasurales / residuos", 30, 3L }
                });

            migrationBuilder.InsertData(
                table: "cuadrillas",
                columns: new[] { "id", "nombre", "turno", "unidad_id" },
                values: new object[,]
                {
                    { 1L, "Cuadrilla Vial A", "manana", 1L },
                    { 2L, "Cuadrilla Alumbrado A", "tarde", 2L }
                });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "activo", "apellidos", "cargo", "codigo_2fa_expira", "codigo_2fa_hash", "correo", "cuadrilla_id", "distrito_id", "especialidad", "fecha_creacion", "nombres", "password_hash", "punto_atencion", "requiere_2fa", "tipo", "token_expira", "token_recuperacion_hash", "ultimo_acceso", "unidad_id" },
                values: new object[,]
                {
                    { 2L, true, "Supervisor", null, null, null, "supervisor@fixa.test", null, null, null, new DateTime(2026, 11, 20, 8, 0, 0, 0, DateTimeKind.Unspecified), "Sergio", "$2a$12$uAa4dbkixagNbb2.EatC9eABiTh9zyYAoE8U9Uj2BIj1KLPI8l.Tq", null, false, "supervisor", null, null, null, 1L },
                    { 4L, true, "Operador", null, null, null, "operador@fixa.test", null, 1L, null, new DateTime(2026, 11, 20, 8, 0, 0, 0, DateTimeKind.Unspecified), "Óscar", "$2a$12$uAa4dbkixagNbb2.EatC9eABiTh9zyYAoE8U9Uj2BIj1KLPI8l.Tq", null, false, "operador", null, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "metas_atencion",
                columns: new[] { "id", "categoria_id", "plazo_dias_habiles", "plazo_horas", "prioridad", "vigente_desde", "vigente_hasta" },
                values: new object[,]
                {
                    { 1L, 1L, null, 72, "alta", new DateOnly(2026, 1, 1), null },
                    { 2L, 1L, 5, null, "media", new DateOnly(2026, 1, 1), null },
                    { 3L, 2L, null, 48, "alta", new DateOnly(2026, 1, 1), null },
                    { 4L, 3L, 3, null, "media", new DateOnly(2026, 1, 1), null }
                });

            migrationBuilder.InsertData(
                table: "usuarios",
                columns: new[] { "id", "activo", "apellidos", "cargo", "codigo_2fa_expira", "codigo_2fa_hash", "correo", "cuadrilla_id", "distrito_id", "especialidad", "fecha_creacion", "nombres", "password_hash", "punto_atencion", "requiere_2fa", "tipo", "token_expira", "token_recuperacion_hash", "ultimo_acceso", "unidad_id" },
                values: new object[] { 3L, true, "Técnica", null, null, null, "tecnico@fixa.test", 1L, null, null, new DateTime(2026, 11, 20, 8, 0, 0, 0, DateTimeKind.Unspecified), "Teresa", "$2a$12$uAa4dbkixagNbb2.EatC9eABiTh9zyYAoE8U9Uj2BIj1KLPI8l.Tq", null, false, "tecnico", null, null, null, 1L });

            migrationBuilder.CreateIndex(
                name: "casos_categoria_id_index",
                table: "casos",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "casos_estado_index",
                table: "casos",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "casos_unidad_id_index",
                table: "casos",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "categorias_codigo_unique",
                table: "categorias",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "categorias_nombre_unique",
                table: "categorias",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "categorias_unidad_id_index",
                table: "categorias",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "cuadrillas_unidad_id_index",
                table: "cuadrillas",
                column: "unidad_id");

            migrationBuilder.CreateIndex(
                name: "derivaciones_caso_id_index",
                table: "derivaciones",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "derivaciones_entidad_id_index",
                table: "derivaciones",
                column: "entidad_id");

            migrationBuilder.CreateIndex(
                name: "derivaciones_responsable_id_index",
                table: "derivaciones",
                column: "responsable_id");

            migrationBuilder.CreateIndex(
                name: "distritos_numero_unique",
                table: "distritos",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "eventos_historial_caso_id_index",
                table: "eventos_historial",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "eventos_historial_usuario_id_index",
                table: "eventos_historial",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "evidencias_intervencion_id_index",
                table: "evidencias",
                column: "intervencion_id");

            migrationBuilder.CreateIndex(
                name: "feriados_fecha_unique",
                table: "feriados",
                column: "fecha",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "fotos_reporte_reporte_id_index",
                table: "fotos_reporte",
                column: "reporte_id");

            migrationBuilder.CreateIndex(
                name: "intervenciones_caso_id_index",
                table: "intervenciones",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "intervenciones_supervisor_id_index",
                table: "intervenciones",
                column: "supervisor_id");

            migrationBuilder.CreateIndex(
                name: "intervenciones_tecnico_id_index",
                table: "intervenciones",
                column: "tecnico_id");

            migrationBuilder.CreateIndex(
                name: "metas_atencion_categoria_id_index",
                table: "metas_atencion",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "notificaciones_evento_id_index",
                table: "notificaciones",
                column: "evento_id");

            migrationBuilder.CreateIndex(
                name: "notificaciones_reporte_id_index",
                table: "notificaciones",
                column: "reporte_id");

            migrationBuilder.CreateIndex(
                name: "programaciones_caso_id_index",
                table: "programaciones",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "programaciones_cuadrilla_id_index",
                table: "programaciones",
                column: "cuadrilla_id");

            migrationBuilder.CreateIndex(
                name: "reportes_caso_id_index",
                table: "reportes",
                column: "caso_id");

            migrationBuilder.CreateIndex(
                name: "reportes_clave_idempotencia_unique",
                table: "reportes",
                column: "clave_idempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "reportes_codigo_unique",
                table: "reportes",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "reportes_distrito_id_index",
                table: "reportes",
                column: "distrito_id");

            migrationBuilder.CreateIndex(
                name: "reportes_operador_id_index",
                table: "reportes",
                column: "operador_id");

            migrationBuilder.CreateIndex(
                name: "solicitudes_revision_reporte_id_index",
                table: "solicitudes_revision",
                column: "reporte_id");

            migrationBuilder.CreateIndex(
                name: "solicitudes_revision_supervisor_id_index",
                table: "solicitudes_revision",
                column: "supervisor_id");

            migrationBuilder.CreateIndex(
                name: "unidades_responsables_sigla_unique",
                table: "unidades_responsables",
                column: "sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "usuarios_correo_unique",
                table: "usuarios",
                column: "correo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "usuarios_cuadrilla_id_index",
                table: "usuarios",
                column: "cuadrilla_id");

            migrationBuilder.CreateIndex(
                name: "usuarios_distrito_id_index",
                table: "usuarios",
                column: "distrito_id");

            migrationBuilder.CreateIndex(
                name: "usuarios_unidad_id_index",
                table: "usuarios",
                column: "unidad_id");

            // Índices espaciales (equivalen a los GiST de PostgreSQL); EF Core no los genera solo.
            migrationBuilder.Sql("CREATE SPATIAL INDEX distritos_limite_spatialindex ON distritos (limite);");
            migrationBuilder.Sql("CREATE SPATIAL INDEX reportes_ubicacion_spatialindex ON reportes (ubicacion);");

            // Los funcionarios de prueba ya tienen contraseña. EF omite el false en los datos de
            // prueba porque la columna vale true por defecto, así que se corrige aquí.
            migrationBuilder.Sql("UPDATE usuarios SET debe_registrar_password = 0 WHERE id IN (1, 2, 3, 4);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS reportes_ubicacion_spatialindex ON reportes;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS distritos_limite_spatialindex ON distritos;");

            migrationBuilder.DropTable(
                name: "contadores_codigo");

            migrationBuilder.DropTable(
                name: "derivaciones");

            migrationBuilder.DropTable(
                name: "evidencias");

            migrationBuilder.DropTable(
                name: "feriados");

            migrationBuilder.DropTable(
                name: "fotos_reporte");

            migrationBuilder.DropTable(
                name: "metas_atencion");

            migrationBuilder.DropTable(
                name: "notificaciones");

            migrationBuilder.DropTable(
                name: "programaciones");

            migrationBuilder.DropTable(
                name: "solicitudes_revision");

            migrationBuilder.DropTable(
                name: "entidades_externas");

            migrationBuilder.DropTable(
                name: "intervenciones");

            migrationBuilder.DropTable(
                name: "eventos_historial");

            migrationBuilder.DropTable(
                name: "reportes");

            migrationBuilder.DropTable(
                name: "casos");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "categorias");

            migrationBuilder.DropTable(
                name: "cuadrillas");

            migrationBuilder.DropTable(
                name: "distritos");

            migrationBuilder.DropTable(
                name: "unidades_responsables");
        }
    }
}
