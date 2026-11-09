--
-- PostgreSQL database dump
--

-- Dumped from database version 17.5 (Debian 17.5-1.pgdg110+1)
-- Dumped by pg_dump version 17.5 (Debian 17.5-1.pgdg110+1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: extensions; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA extensions;


--
-- Name: tiger; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA tiger;


--
-- Name: tiger_data; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA tiger_data;


--
-- Name: topology; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA topology;


--
-- Name: fuzzystrmatch; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS fuzzystrmatch WITH SCHEMA public;


--
-- Name: postgis; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis WITH SCHEMA public;


--
-- Name: postgis_tiger_geocoder; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis_tiger_geocoder WITH SCHEMA tiger;


--
-- Name: postgis_topology; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS postgis_topology WITH SCHEMA topology;


--
-- Name: impedir_cambios_historial(); Type: FUNCTION; Schema: public; Owner: -
--

CREATE FUNCTION public.impedir_cambios_historial() RETURNS trigger
    LANGUAGE plpgsql
    SET search_path TO ''
    AS $$
    BEGIN
        RAISE EXCEPTION 'eventos_historial es inmutable: registre un evento nuevo';
    END;
    $$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: cache; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cache (
    key character varying(255) NOT NULL,
    value text NOT NULL,
    expiration integer NOT NULL
);


--
-- Name: cache_locks; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cache_locks (
    key character varying(255) NOT NULL,
    owner character varying(255) NOT NULL,
    expiration integer NOT NULL
);


--
-- Name: casos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.casos (
    id bigint NOT NULL,
    categoria_id bigint NOT NULL,
    unidad_id bigint,
    estado character varying(25) NOT NULL,
    prioridad character varying(10),
    justificacion_prioridad text,
    requiere_verificacion_campo boolean DEFAULT false NOT NULL,
    vencido boolean DEFAULT false NOT NULL,
    fecha_creacion timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    fecha_objetivo timestamp(0) without time zone,
    fecha_cierre timestamp(0) without time zone
);


--
-- Name: casos_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.casos_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: casos_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.casos_id_seq OWNED BY public.casos.id;


--
-- Name: categorias; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.categorias (
    id bigint NOT NULL,
    unidad_id bigint NOT NULL,
    nombre character varying(60) NOT NULL,
    descripcion text NOT NULL,
    activa boolean DEFAULT true NOT NULL,
    radio_cercania_m integer NOT NULL,
    codigo character varying(20) NOT NULL,
    CONSTRAINT categorias_codigo_check CHECK (((codigo)::text = ANY ((ARRAY['bache'::character varying, 'alumbrado'::character varying, 'residuos'::character varying])::text[])))
);


--
-- Name: categorias_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.categorias_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: categorias_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.categorias_id_seq OWNED BY public.categorias.id;


--
-- Name: contadores_codigo; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.contadores_codigo (
    anio integer NOT NULL,
    ultimo integer NOT NULL
);


--
-- Name: cuadrillas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.cuadrillas (
    id bigint NOT NULL,
    unidad_id bigint NOT NULL,
    nombre character varying(60) NOT NULL,
    turno character varying(10) NOT NULL
);


--
-- Name: cuadrillas_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.cuadrillas_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: cuadrillas_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.cuadrillas_id_seq OWNED BY public.cuadrillas.id;


--
-- Name: derivaciones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.derivaciones (
    id bigint NOT NULL,
    caso_id bigint NOT NULL,
    entidad_id bigint NOT NULL,
    responsable_id bigint NOT NULL,
    motivo text NOT NULL,
    fecha_derivacion timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    estado_seguimiento character varying(25) NOT NULL,
    respuesta_entidad text,
    fecha_respuesta timestamp(0) without time zone
);


--
-- Name: derivaciones_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.derivaciones_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: derivaciones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.derivaciones_id_seq OWNED BY public.derivaciones.id;


--
-- Name: distritos; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.distritos (
    id bigint NOT NULL,
    numero integer NOT NULL,
    nombre character varying(80) NOT NULL,
    limite public.geometry(Polygon,4326) NOT NULL
);


--
-- Name: distritos_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.distritos_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: distritos_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.distritos_id_seq OWNED BY public.distritos.id;


--
-- Name: entidades_externas; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.entidades_externas (
    id bigint NOT NULL,
    nombre character varying(120) NOT NULL,
    contacto character varying(120)
);


--
-- Name: entidades_externas_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.entidades_externas_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: entidades_externas_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.entidades_externas_id_seq OWNED BY public.entidades_externas.id;


--
-- Name: eventos_historial; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.eventos_historial (
    id bigint NOT NULL,
    caso_id bigint NOT NULL,
    usuario_id bigint,
    fecha_hora timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    tipo character varying(20) NOT NULL,
    estado_anterior character varying(25),
    estado_nuevo character varying(25),
    detalle text NOT NULL,
    motivo text
);


--
-- Name: eventos_historial_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.eventos_historial_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: eventos_historial_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.eventos_historial_id_seq OWNED BY public.eventos_historial.id;


--
-- Name: evidencias; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.evidencias (
    id bigint NOT NULL,
    intervencion_id bigint NOT NULL,
    tipo character varying(15) NOT NULL,
    url character varying(500) NOT NULL,
    descripcion character varying(200)
);


--
-- Name: evidencias_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.evidencias_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: evidencias_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.evidencias_id_seq OWNED BY public.evidencias.id;


--
-- Name: failed_jobs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.failed_jobs (
    id bigint NOT NULL,
    uuid character varying(255) NOT NULL,
    connection text NOT NULL,
    queue text NOT NULL,
    payload text NOT NULL,
    exception text NOT NULL,
    failed_at timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: failed_jobs_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.failed_jobs_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: failed_jobs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.failed_jobs_id_seq OWNED BY public.failed_jobs.id;


--
-- Name: feriados; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.feriados (
    id bigint NOT NULL,
    fecha date NOT NULL,
    nombre character varying(100) NOT NULL,
    fuente character varying(200) NOT NULL
);


--
-- Name: feriados_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.feriados_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: feriados_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.feriados_id_seq OWNED BY public.feriados.id;


--
-- Name: fotos_reporte; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.fotos_reporte (
    id bigint NOT NULL,
    reporte_id bigint NOT NULL,
    url character varying(500) NOT NULL,
    public_id character varying(200) NOT NULL,
    fecha_carga timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: fotos_reporte_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.fotos_reporte_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: fotos_reporte_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.fotos_reporte_id_seq OWNED BY public.fotos_reporte.id;


--
-- Name: intervenciones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.intervenciones (
    id bigint NOT NULL,
    caso_id bigint NOT NULL,
    tecnico_id bigint NOT NULL,
    supervisor_id bigint,
    fecha timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    trabajo_realizado text NOT NULL,
    estado_validacion character varying(15) NOT NULL,
    observacion_validacion text
);


--
-- Name: intervenciones_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.intervenciones_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: intervenciones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.intervenciones_id_seq OWNED BY public.intervenciones.id;


--
-- Name: job_batches; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.job_batches (
    id character varying(255) NOT NULL,
    name character varying(255) NOT NULL,
    total_jobs integer NOT NULL,
    pending_jobs integer NOT NULL,
    failed_jobs integer NOT NULL,
    failed_job_ids text NOT NULL,
    options text,
    cancelled_at integer,
    created_at integer NOT NULL,
    finished_at integer
);


--
-- Name: jobs; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.jobs (
    id bigint NOT NULL,
    queue character varying(255) NOT NULL,
    payload text NOT NULL,
    attempts smallint NOT NULL,
    reserved_at integer,
    available_at integer NOT NULL,
    created_at integer NOT NULL
);


--
-- Name: jobs_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.jobs_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: jobs_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.jobs_id_seq OWNED BY public.jobs.id;


--
-- Name: metas_atencion; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.metas_atencion (
    id bigint NOT NULL,
    categoria_id bigint NOT NULL,
    prioridad character varying(10) NOT NULL,
    plazo_horas integer,
    vigente_desde date NOT NULL,
    vigente_hasta date,
    plazo_dias_habiles integer,
    CONSTRAINT metas_atencion_plazo_check CHECK (((plazo_horas IS NULL) <> (plazo_dias_habiles IS NULL)))
);


--
-- Name: metas_atencion_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.metas_atencion_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: metas_atencion_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.metas_atencion_id_seq OWNED BY public.metas_atencion.id;


--
-- Name: migrations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.migrations (
    id integer NOT NULL,
    migration character varying(255) NOT NULL,
    batch integer NOT NULL
);


--
-- Name: migrations_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.migrations_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: migrations_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.migrations_id_seq OWNED BY public.migrations.id;


--
-- Name: notificaciones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.notificaciones (
    id bigint NOT NULL,
    evento_id bigint NOT NULL,
    reporte_id bigint NOT NULL,
    canal character varying(10) NOT NULL,
    destino character varying(255) NOT NULL,
    fecha_envio timestamp(0) without time zone,
    estado_entrega character varying(15) NOT NULL
);


--
-- Name: notificaciones_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.notificaciones_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: notificaciones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.notificaciones_id_seq OWNED BY public.notificaciones.id;


--
-- Name: personal_access_tokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.personal_access_tokens (
    id bigint NOT NULL,
    tokenable_type character varying(255) NOT NULL,
    tokenable_id bigint NOT NULL,
    name text NOT NULL,
    token character varying(64) NOT NULL,
    abilities text,
    last_used_at timestamp(0) without time zone,
    expires_at timestamp(0) without time zone,
    created_at timestamp(0) without time zone,
    updated_at timestamp(0) without time zone
);


--
-- Name: personal_access_tokens_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.personal_access_tokens_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: personal_access_tokens_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.personal_access_tokens_id_seq OWNED BY public.personal_access_tokens.id;


--
-- Name: programaciones; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.programaciones (
    id bigint NOT NULL,
    caso_id bigint NOT NULL,
    cuadrilla_id bigint NOT NULL,
    fecha_prevista date NOT NULL,
    motivo_reprogramacion character varying(200),
    vigente boolean DEFAULT true NOT NULL
);


--
-- Name: programaciones_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.programaciones_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: programaciones_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.programaciones_id_seq OWNED BY public.programaciones.id;


--
-- Name: reportes; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.reportes (
    id bigint NOT NULL,
    caso_id bigint NOT NULL,
    distrito_id bigint NOT NULL,
    operador_id bigint,
    codigo character varying(20) NOT NULL,
    clave_hash character varying(255) NOT NULL,
    descripcion text NOT NULL,
    canal_origen character varying(25) NOT NULL,
    ubicacion public.geometry(Point,4326) NOT NULL,
    direccion character varying(200),
    referencia character varying(200),
    codigo_aviso_cobranza character varying(30),
    motivo_sin_foto character varying(200),
    correo_contacto character varying(120),
    token_dispositivo character varying(255),
    clave_idempotencia character varying(64) NOT NULL,
    es_apoyo boolean DEFAULT false NOT NULL,
    conformidad boolean,
    fecha_conformidad timestamp(0) without time zone,
    fecha_registro timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    tipo_incidencia character varying(30) NOT NULL,
    numero_luminarias integer,
    CONSTRAINT reportes_numero_luminarias_check CHECK ((numero_luminarias > 0))
);


--
-- Name: reportes_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.reportes_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: reportes_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.reportes_id_seq OWNED BY public.reportes.id;


--
-- Name: sessions; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.sessions (
    id character varying(255) NOT NULL,
    user_id bigint,
    ip_address character varying(45),
    user_agent text,
    payload text NOT NULL,
    last_activity integer NOT NULL
);


--
-- Name: solicitudes_revision; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.solicitudes_revision (
    id bigint NOT NULL,
    reporte_id bigint NOT NULL,
    supervisor_id bigint,
    fecha_solicitud timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    motivo text NOT NULL,
    estado character varying(15) NOT NULL,
    respuesta text,
    fecha_resolucion timestamp(0) without time zone
);


--
-- Name: solicitudes_revision_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.solicitudes_revision_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: solicitudes_revision_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.solicitudes_revision_id_seq OWNED BY public.solicitudes_revision.id;


--
-- Name: unidades_responsables; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.unidades_responsables (
    id bigint NOT NULL,
    nombre character varying(120) NOT NULL,
    sigla character varying(20) NOT NULL
);


--
-- Name: unidades_responsables_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.unidades_responsables_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: unidades_responsables_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.unidades_responsables_id_seq OWNED BY public.unidades_responsables.id;


--
-- Name: usuarios; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public.usuarios (
    id bigint NOT NULL,
    tipo character varying(20) NOT NULL,
    nombres character varying(80) NOT NULL,
    apellidos character varying(80) NOT NULL,
    correo character varying(120) NOT NULL,
    password_hash character varying(255) NOT NULL,
    activo boolean DEFAULT true NOT NULL,
    debe_registrar_password boolean DEFAULT true NOT NULL,
    requiere_2fa boolean DEFAULT false NOT NULL,
    codigo_2fa_hash character varying(255),
    codigo_2fa_expira timestamp(0) without time zone,
    token_recuperacion_hash character varying(255),
    token_expira timestamp(0) without time zone,
    ultimo_acceso timestamp(0) without time zone,
    fecha_creacion timestamp(0) without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    punto_atencion character varying(80),
    distrito_id bigint,
    cargo character varying(80),
    unidad_id bigint,
    especialidad character varying(80),
    cuadrilla_id bigint,
    CONSTRAINT usuarios_tipo_check CHECK (((tipo)::text = ANY ((ARRAY['administrador'::character varying, 'supervisor'::character varying, 'tecnico'::character varying, 'operador'::character varying])::text[])))
);


--
-- Name: usuarios_id_seq; Type: SEQUENCE; Schema: public; Owner: -
--

CREATE SEQUENCE public.usuarios_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: usuarios_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: -
--

ALTER SEQUENCE public.usuarios_id_seq OWNED BY public.usuarios.id;


--
-- Name: casos id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.casos ALTER COLUMN id SET DEFAULT nextval('public.casos_id_seq'::regclass);


--
-- Name: categorias id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias ALTER COLUMN id SET DEFAULT nextval('public.categorias_id_seq'::regclass);


--
-- Name: cuadrillas id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cuadrillas ALTER COLUMN id SET DEFAULT nextval('public.cuadrillas_id_seq'::regclass);


--
-- Name: derivaciones id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.derivaciones ALTER COLUMN id SET DEFAULT nextval('public.derivaciones_id_seq'::regclass);


--
-- Name: distritos id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.distritos ALTER COLUMN id SET DEFAULT nextval('public.distritos_id_seq'::regclass);


--
-- Name: entidades_externas id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entidades_externas ALTER COLUMN id SET DEFAULT nextval('public.entidades_externas_id_seq'::regclass);


--
-- Name: eventos_historial id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.eventos_historial ALTER COLUMN id SET DEFAULT nextval('public.eventos_historial_id_seq'::regclass);


--
-- Name: evidencias id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.evidencias ALTER COLUMN id SET DEFAULT nextval('public.evidencias_id_seq'::regclass);


--
-- Name: failed_jobs id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.failed_jobs ALTER COLUMN id SET DEFAULT nextval('public.failed_jobs_id_seq'::regclass);


--
-- Name: feriados id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.feriados ALTER COLUMN id SET DEFAULT nextval('public.feriados_id_seq'::regclass);


--
-- Name: fotos_reporte id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.fotos_reporte ALTER COLUMN id SET DEFAULT nextval('public.fotos_reporte_id_seq'::regclass);


--
-- Name: intervenciones id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.intervenciones ALTER COLUMN id SET DEFAULT nextval('public.intervenciones_id_seq'::regclass);


--
-- Name: jobs id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.jobs ALTER COLUMN id SET DEFAULT nextval('public.jobs_id_seq'::regclass);


--
-- Name: metas_atencion id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.metas_atencion ALTER COLUMN id SET DEFAULT nextval('public.metas_atencion_id_seq'::regclass);


--
-- Name: migrations id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.migrations ALTER COLUMN id SET DEFAULT nextval('public.migrations_id_seq'::regclass);


--
-- Name: notificaciones id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notificaciones ALTER COLUMN id SET DEFAULT nextval('public.notificaciones_id_seq'::regclass);


--
-- Name: personal_access_tokens id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.personal_access_tokens ALTER COLUMN id SET DEFAULT nextval('public.personal_access_tokens_id_seq'::regclass);


--
-- Name: programaciones id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.programaciones ALTER COLUMN id SET DEFAULT nextval('public.programaciones_id_seq'::regclass);


--
-- Name: reportes id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes ALTER COLUMN id SET DEFAULT nextval('public.reportes_id_seq'::regclass);


--
-- Name: solicitudes_revision id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.solicitudes_revision ALTER COLUMN id SET DEFAULT nextval('public.solicitudes_revision_id_seq'::regclass);


--
-- Name: unidades_responsables id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.unidades_responsables ALTER COLUMN id SET DEFAULT nextval('public.unidades_responsables_id_seq'::regclass);


--
-- Name: usuarios id; Type: DEFAULT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios ALTER COLUMN id SET DEFAULT nextval('public.usuarios_id_seq'::regclass);


--
-- Name: cache_locks cache_locks_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cache_locks
    ADD CONSTRAINT cache_locks_pkey PRIMARY KEY (key);


--
-- Name: cache cache_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cache
    ADD CONSTRAINT cache_pkey PRIMARY KEY (key);


--
-- Name: casos casos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.casos
    ADD CONSTRAINT casos_pkey PRIMARY KEY (id);


--
-- Name: categorias categorias_codigo_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT categorias_codigo_unique UNIQUE (codigo);


--
-- Name: categorias categorias_nombre_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT categorias_nombre_unique UNIQUE (nombre);


--
-- Name: categorias categorias_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT categorias_pkey PRIMARY KEY (id);


--
-- Name: contadores_codigo contadores_codigo_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.contadores_codigo
    ADD CONSTRAINT contadores_codigo_pkey PRIMARY KEY (anio);


--
-- Name: cuadrillas cuadrillas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cuadrillas
    ADD CONSTRAINT cuadrillas_pkey PRIMARY KEY (id);


--
-- Name: derivaciones derivaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.derivaciones
    ADD CONSTRAINT derivaciones_pkey PRIMARY KEY (id);


--
-- Name: distritos distritos_numero_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.distritos
    ADD CONSTRAINT distritos_numero_unique UNIQUE (numero);


--
-- Name: distritos distritos_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.distritos
    ADD CONSTRAINT distritos_pkey PRIMARY KEY (id);


--
-- Name: entidades_externas entidades_externas_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.entidades_externas
    ADD CONSTRAINT entidades_externas_pkey PRIMARY KEY (id);


--
-- Name: eventos_historial eventos_historial_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.eventos_historial
    ADD CONSTRAINT eventos_historial_pkey PRIMARY KEY (id);


--
-- Name: evidencias evidencias_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.evidencias
    ADD CONSTRAINT evidencias_pkey PRIMARY KEY (id);


--
-- Name: failed_jobs failed_jobs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.failed_jobs
    ADD CONSTRAINT failed_jobs_pkey PRIMARY KEY (id);


--
-- Name: failed_jobs failed_jobs_uuid_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.failed_jobs
    ADD CONSTRAINT failed_jobs_uuid_unique UNIQUE (uuid);


--
-- Name: feriados feriados_fecha_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.feriados
    ADD CONSTRAINT feriados_fecha_unique UNIQUE (fecha);


--
-- Name: feriados feriados_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.feriados
    ADD CONSTRAINT feriados_pkey PRIMARY KEY (id);


--
-- Name: fotos_reporte fotos_reporte_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.fotos_reporte
    ADD CONSTRAINT fotos_reporte_pkey PRIMARY KEY (id);


--
-- Name: intervenciones intervenciones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.intervenciones
    ADD CONSTRAINT intervenciones_pkey PRIMARY KEY (id);


--
-- Name: job_batches job_batches_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.job_batches
    ADD CONSTRAINT job_batches_pkey PRIMARY KEY (id);


--
-- Name: jobs jobs_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.jobs
    ADD CONSTRAINT jobs_pkey PRIMARY KEY (id);


--
-- Name: metas_atencion metas_atencion_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.metas_atencion
    ADD CONSTRAINT metas_atencion_pkey PRIMARY KEY (id);


--
-- Name: migrations migrations_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.migrations
    ADD CONSTRAINT migrations_pkey PRIMARY KEY (id);


--
-- Name: notificaciones notificaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notificaciones
    ADD CONSTRAINT notificaciones_pkey PRIMARY KEY (id);


--
-- Name: personal_access_tokens personal_access_tokens_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.personal_access_tokens
    ADD CONSTRAINT personal_access_tokens_pkey PRIMARY KEY (id);


--
-- Name: personal_access_tokens personal_access_tokens_token_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.personal_access_tokens
    ADD CONSTRAINT personal_access_tokens_token_unique UNIQUE (token);


--
-- Name: programaciones programaciones_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.programaciones
    ADD CONSTRAINT programaciones_pkey PRIMARY KEY (id);


--
-- Name: reportes reportes_clave_idempotencia_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes
    ADD CONSTRAINT reportes_clave_idempotencia_unique UNIQUE (clave_idempotencia);


--
-- Name: reportes reportes_codigo_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes
    ADD CONSTRAINT reportes_codigo_unique UNIQUE (codigo);


--
-- Name: reportes reportes_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes
    ADD CONSTRAINT reportes_pkey PRIMARY KEY (id);


--
-- Name: sessions sessions_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.sessions
    ADD CONSTRAINT sessions_pkey PRIMARY KEY (id);


--
-- Name: solicitudes_revision solicitudes_revision_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.solicitudes_revision
    ADD CONSTRAINT solicitudes_revision_pkey PRIMARY KEY (id);


--
-- Name: unidades_responsables unidades_responsables_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.unidades_responsables
    ADD CONSTRAINT unidades_responsables_pkey PRIMARY KEY (id);


--
-- Name: unidades_responsables unidades_responsables_sigla_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.unidades_responsables
    ADD CONSTRAINT unidades_responsables_sigla_unique UNIQUE (sigla);


--
-- Name: usuarios usuarios_correo_unique; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_correo_unique UNIQUE (correo);


--
-- Name: usuarios usuarios_pkey; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_pkey PRIMARY KEY (id);


--
-- Name: cache_expiration_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX cache_expiration_index ON public.cache USING btree (expiration);


--
-- Name: cache_locks_expiration_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX cache_locks_expiration_index ON public.cache_locks USING btree (expiration);


--
-- Name: casos_categoria_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX casos_categoria_id_index ON public.casos USING btree (categoria_id);


--
-- Name: casos_estado_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX casos_estado_index ON public.casos USING btree (estado);


--
-- Name: casos_unidad_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX casos_unidad_id_index ON public.casos USING btree (unidad_id);


--
-- Name: categorias_unidad_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX categorias_unidad_id_index ON public.categorias USING btree (unidad_id);


--
-- Name: cuadrillas_unidad_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX cuadrillas_unidad_id_index ON public.cuadrillas USING btree (unidad_id);


--
-- Name: derivaciones_caso_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX derivaciones_caso_id_index ON public.derivaciones USING btree (caso_id);


--
-- Name: derivaciones_entidad_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX derivaciones_entidad_id_index ON public.derivaciones USING btree (entidad_id);


--
-- Name: derivaciones_responsable_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX derivaciones_responsable_id_index ON public.derivaciones USING btree (responsable_id);


--
-- Name: distritos_limite_spatialindex; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX distritos_limite_spatialindex ON public.distritos USING gist (limite);


--
-- Name: eventos_historial_caso_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX eventos_historial_caso_id_index ON public.eventos_historial USING btree (caso_id);


--
-- Name: eventos_historial_usuario_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX eventos_historial_usuario_id_index ON public.eventos_historial USING btree (usuario_id);


--
-- Name: evidencias_intervencion_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX evidencias_intervencion_id_index ON public.evidencias USING btree (intervencion_id);


--
-- Name: fotos_reporte_reporte_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX fotos_reporte_reporte_id_index ON public.fotos_reporte USING btree (reporte_id);


--
-- Name: intervenciones_caso_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX intervenciones_caso_id_index ON public.intervenciones USING btree (caso_id);


--
-- Name: intervenciones_supervisor_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX intervenciones_supervisor_id_index ON public.intervenciones USING btree (supervisor_id);


--
-- Name: intervenciones_tecnico_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX intervenciones_tecnico_id_index ON public.intervenciones USING btree (tecnico_id);


--
-- Name: jobs_queue_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX jobs_queue_index ON public.jobs USING btree (queue);


--
-- Name: metas_atencion_categoria_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX metas_atencion_categoria_id_index ON public.metas_atencion USING btree (categoria_id);


--
-- Name: notificaciones_evento_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX notificaciones_evento_id_index ON public.notificaciones USING btree (evento_id);


--
-- Name: notificaciones_reporte_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX notificaciones_reporte_id_index ON public.notificaciones USING btree (reporte_id);


--
-- Name: personal_access_tokens_expires_at_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX personal_access_tokens_expires_at_index ON public.personal_access_tokens USING btree (expires_at);


--
-- Name: personal_access_tokens_tokenable_type_tokenable_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX personal_access_tokens_tokenable_type_tokenable_id_index ON public.personal_access_tokens USING btree (tokenable_type, tokenable_id);


--
-- Name: programaciones_caso_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX programaciones_caso_id_index ON public.programaciones USING btree (caso_id);


--
-- Name: programaciones_cuadrilla_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX programaciones_cuadrilla_id_index ON public.programaciones USING btree (cuadrilla_id);


--
-- Name: reportes_caso_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX reportes_caso_id_index ON public.reportes USING btree (caso_id);


--
-- Name: reportes_distrito_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX reportes_distrito_id_index ON public.reportes USING btree (distrito_id);


--
-- Name: reportes_operador_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX reportes_operador_id_index ON public.reportes USING btree (operador_id);


--
-- Name: reportes_ubicacion_spatialindex; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX reportes_ubicacion_spatialindex ON public.reportes USING gist (ubicacion);


--
-- Name: sessions_last_activity_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX sessions_last_activity_index ON public.sessions USING btree (last_activity);


--
-- Name: sessions_user_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX sessions_user_id_index ON public.sessions USING btree (user_id);


--
-- Name: solicitudes_revision_reporte_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX solicitudes_revision_reporte_id_index ON public.solicitudes_revision USING btree (reporte_id);


--
-- Name: solicitudes_revision_supervisor_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX solicitudes_revision_supervisor_id_index ON public.solicitudes_revision USING btree (supervisor_id);


--
-- Name: usuarios_cuadrilla_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX usuarios_cuadrilla_id_index ON public.usuarios USING btree (cuadrilla_id);


--
-- Name: usuarios_distrito_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX usuarios_distrito_id_index ON public.usuarios USING btree (distrito_id);


--
-- Name: usuarios_unidad_id_index; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX usuarios_unidad_id_index ON public.usuarios USING btree (unidad_id);


--
-- Name: eventos_historial eventos_historial_inmutable; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER eventos_historial_inmutable BEFORE DELETE OR UPDATE ON public.eventos_historial FOR EACH ROW EXECUTE FUNCTION public.impedir_cambios_historial();


--
-- Name: eventos_historial eventos_historial_sin_truncate; Type: TRIGGER; Schema: public; Owner: -
--

CREATE TRIGGER eventos_historial_sin_truncate BEFORE TRUNCATE ON public.eventos_historial FOR EACH STATEMENT EXECUTE FUNCTION public.impedir_cambios_historial();


--
-- Name: casos casos_categoria_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.casos
    ADD CONSTRAINT casos_categoria_id_foreign FOREIGN KEY (categoria_id) REFERENCES public.categorias(id) ON DELETE RESTRICT;


--
-- Name: casos casos_unidad_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.casos
    ADD CONSTRAINT casos_unidad_id_foreign FOREIGN KEY (unidad_id) REFERENCES public.unidades_responsables(id) ON DELETE RESTRICT;


--
-- Name: categorias categorias_unidad_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.categorias
    ADD CONSTRAINT categorias_unidad_id_foreign FOREIGN KEY (unidad_id) REFERENCES public.unidades_responsables(id) ON DELETE RESTRICT;


--
-- Name: cuadrillas cuadrillas_unidad_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.cuadrillas
    ADD CONSTRAINT cuadrillas_unidad_id_foreign FOREIGN KEY (unidad_id) REFERENCES public.unidades_responsables(id) ON DELETE RESTRICT;


--
-- Name: derivaciones derivaciones_caso_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.derivaciones
    ADD CONSTRAINT derivaciones_caso_id_foreign FOREIGN KEY (caso_id) REFERENCES public.casos(id) ON DELETE RESTRICT;


--
-- Name: derivaciones derivaciones_entidad_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.derivaciones
    ADD CONSTRAINT derivaciones_entidad_id_foreign FOREIGN KEY (entidad_id) REFERENCES public.entidades_externas(id) ON DELETE RESTRICT;


--
-- Name: derivaciones derivaciones_responsable_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.derivaciones
    ADD CONSTRAINT derivaciones_responsable_id_foreign FOREIGN KEY (responsable_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: eventos_historial eventos_historial_caso_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.eventos_historial
    ADD CONSTRAINT eventos_historial_caso_id_foreign FOREIGN KEY (caso_id) REFERENCES public.casos(id) ON DELETE RESTRICT;


--
-- Name: eventos_historial eventos_historial_usuario_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.eventos_historial
    ADD CONSTRAINT eventos_historial_usuario_id_foreign FOREIGN KEY (usuario_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: evidencias evidencias_intervencion_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.evidencias
    ADD CONSTRAINT evidencias_intervencion_id_foreign FOREIGN KEY (intervencion_id) REFERENCES public.intervenciones(id) ON DELETE RESTRICT;


--
-- Name: fotos_reporte fotos_reporte_reporte_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.fotos_reporte
    ADD CONSTRAINT fotos_reporte_reporte_id_foreign FOREIGN KEY (reporte_id) REFERENCES public.reportes(id) ON DELETE RESTRICT;


--
-- Name: intervenciones intervenciones_caso_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.intervenciones
    ADD CONSTRAINT intervenciones_caso_id_foreign FOREIGN KEY (caso_id) REFERENCES public.casos(id) ON DELETE RESTRICT;


--
-- Name: intervenciones intervenciones_supervisor_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.intervenciones
    ADD CONSTRAINT intervenciones_supervisor_id_foreign FOREIGN KEY (supervisor_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: intervenciones intervenciones_tecnico_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.intervenciones
    ADD CONSTRAINT intervenciones_tecnico_id_foreign FOREIGN KEY (tecnico_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: metas_atencion metas_atencion_categoria_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.metas_atencion
    ADD CONSTRAINT metas_atencion_categoria_id_foreign FOREIGN KEY (categoria_id) REFERENCES public.categorias(id) ON DELETE RESTRICT;


--
-- Name: notificaciones notificaciones_evento_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notificaciones
    ADD CONSTRAINT notificaciones_evento_id_foreign FOREIGN KEY (evento_id) REFERENCES public.eventos_historial(id) ON DELETE RESTRICT;


--
-- Name: notificaciones notificaciones_reporte_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.notificaciones
    ADD CONSTRAINT notificaciones_reporte_id_foreign FOREIGN KEY (reporte_id) REFERENCES public.reportes(id) ON DELETE RESTRICT;


--
-- Name: programaciones programaciones_caso_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.programaciones
    ADD CONSTRAINT programaciones_caso_id_foreign FOREIGN KEY (caso_id) REFERENCES public.casos(id) ON DELETE RESTRICT;


--
-- Name: programaciones programaciones_cuadrilla_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.programaciones
    ADD CONSTRAINT programaciones_cuadrilla_id_foreign FOREIGN KEY (cuadrilla_id) REFERENCES public.cuadrillas(id) ON DELETE RESTRICT;


--
-- Name: reportes reportes_caso_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes
    ADD CONSTRAINT reportes_caso_id_foreign FOREIGN KEY (caso_id) REFERENCES public.casos(id) ON DELETE RESTRICT;


--
-- Name: reportes reportes_distrito_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes
    ADD CONSTRAINT reportes_distrito_id_foreign FOREIGN KEY (distrito_id) REFERENCES public.distritos(id) ON DELETE RESTRICT;


--
-- Name: reportes reportes_operador_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.reportes
    ADD CONSTRAINT reportes_operador_id_foreign FOREIGN KEY (operador_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: solicitudes_revision solicitudes_revision_reporte_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.solicitudes_revision
    ADD CONSTRAINT solicitudes_revision_reporte_id_foreign FOREIGN KEY (reporte_id) REFERENCES public.reportes(id) ON DELETE RESTRICT;


--
-- Name: solicitudes_revision solicitudes_revision_supervisor_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.solicitudes_revision
    ADD CONSTRAINT solicitudes_revision_supervisor_id_foreign FOREIGN KEY (supervisor_id) REFERENCES public.usuarios(id) ON DELETE RESTRICT;


--
-- Name: usuarios usuarios_cuadrilla_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_cuadrilla_id_foreign FOREIGN KEY (cuadrilla_id) REFERENCES public.cuadrillas(id) ON DELETE RESTRICT;


--
-- Name: usuarios usuarios_distrito_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_distrito_id_foreign FOREIGN KEY (distrito_id) REFERENCES public.distritos(id) ON DELETE RESTRICT;


--
-- Name: usuarios usuarios_unidad_id_foreign; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public.usuarios
    ADD CONSTRAINT usuarios_unidad_id_foreign FOREIGN KEY (unidad_id) REFERENCES public.unidades_responsables(id) ON DELETE RESTRICT;


--
-- Name: cache; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.cache ENABLE ROW LEVEL SECURITY;

--
-- Name: cache_locks; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.cache_locks ENABLE ROW LEVEL SECURITY;

--
-- Name: casos; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.casos ENABLE ROW LEVEL SECURITY;

--
-- Name: categorias; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.categorias ENABLE ROW LEVEL SECURITY;

--
-- Name: contadores_codigo; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.contadores_codigo ENABLE ROW LEVEL SECURITY;

--
-- Name: cuadrillas; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.cuadrillas ENABLE ROW LEVEL SECURITY;

--
-- Name: derivaciones; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.derivaciones ENABLE ROW LEVEL SECURITY;

--
-- Name: distritos; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.distritos ENABLE ROW LEVEL SECURITY;

--
-- Name: entidades_externas; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.entidades_externas ENABLE ROW LEVEL SECURITY;

--
-- Name: eventos_historial; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.eventos_historial ENABLE ROW LEVEL SECURITY;

--
-- Name: evidencias; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.evidencias ENABLE ROW LEVEL SECURITY;

--
-- Name: failed_jobs; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.failed_jobs ENABLE ROW LEVEL SECURITY;

--
-- Name: feriados; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.feriados ENABLE ROW LEVEL SECURITY;

--
-- Name: fotos_reporte; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.fotos_reporte ENABLE ROW LEVEL SECURITY;

--
-- Name: intervenciones; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.intervenciones ENABLE ROW LEVEL SECURITY;

--
-- Name: job_batches; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.job_batches ENABLE ROW LEVEL SECURITY;

--
-- Name: jobs; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.jobs ENABLE ROW LEVEL SECURITY;

--
-- Name: metas_atencion; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.metas_atencion ENABLE ROW LEVEL SECURITY;

--
-- Name: migrations; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.migrations ENABLE ROW LEVEL SECURITY;

--
-- Name: notificaciones; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.notificaciones ENABLE ROW LEVEL SECURITY;

--
-- Name: programaciones; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.programaciones ENABLE ROW LEVEL SECURITY;

--
-- Name: reportes; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.reportes ENABLE ROW LEVEL SECURITY;

--
-- Name: sessions; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.sessions ENABLE ROW LEVEL SECURITY;

--
-- Name: solicitudes_revision; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.solicitudes_revision ENABLE ROW LEVEL SECURITY;

--
-- Name: unidades_responsables; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.unidades_responsables ENABLE ROW LEVEL SECURITY;

--
-- Name: usuarios; Type: ROW SECURITY; Schema: public; Owner: -
--

ALTER TABLE public.usuarios ENABLE ROW LEVEL SECURITY;

--
-- PostgreSQL database dump complete
--

