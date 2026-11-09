-- Datos mínimos y ficticios para las pruebas automáticas. No son datos reales.
-- Los usuarios se crean desde EntornoPruebas porque su contraseña necesita un hash bcrypt.

INSERT INTO unidades_responsables (id, nombre, sigla) VALUES
    (1, 'Unidad de prueba de vialidad', 'UPV'),
    (2, 'Unidad de prueba de alumbrado', 'UPA'),
    (3, 'Unidad de prueba de aseo', 'UPS');

INSERT INTO categorias (id, unidad_id, nombre, descripcion, activa, radio_cercania_m, codigo) VALUES
    (1, 1, 'Baches y deterioro vial', 'Prueba', true, 30, 'bache'),
    (2, 2, 'Fallas de alumbrado público', 'Prueba', true, 30, 'alumbrado'),
    (3, 3, 'Microbasurales / residuos', 'Prueba', true, 30, 'residuos');

-- Un distrito cuadrado que contiene el punto de prueba (-17.7833, -63.1821).
INSERT INTO distritos (id, numero, nombre, limite) VALUES
    (1, 1, 'Distrito de prueba',
     ST_GeomFromText('POLYGON((-63.20 -17.80, -63.16 -17.80, -63.16 -17.76, -63.20 -17.76, -63.20 -17.80))', 4326));

INSERT INTO cuadrillas (id, unidad_id, nombre, turno) VALUES
    (1, 1, 'Cuadrilla A', 'manana'),
    (2, 1, 'Cuadrilla B', 'tarde'),
    (3, 2, 'Cuadrilla C', 'manana');

INSERT INTO entidades_externas (id, nombre, contacto) VALUES
    (1, 'Entidad externa de prueba', NULL);

INSERT INTO metas_atencion (categoria_id, prioridad, plazo_horas, plazo_dias_habiles, vigente_desde) VALUES
    (1, 'alta', 72, NULL, DATE '2020-01-01'),
    (1, 'media', NULL, 5, DATE '2020-01-01');

SELECT setval('unidades_responsables_id_seq', 3);
SELECT setval('categorias_id_seq', 3);
SELECT setval('distritos_id_seq', 1);
SELECT setval('cuadrillas_id_seq', 3);
SELECT setval('entidades_externas_id_seq', 1);
