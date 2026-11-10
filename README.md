[![CI](https://github.com/helendrg14-em/Fixa_API/actions/workflows/ci.yml/badge.svg)](https://github.com/helendrg14-em/Fixa_API/actions/workflows/ci.yml)

# FIXA API

API REST de FIXA en ASP.NET Core (.NET 10). La consumen la app del ciudadano y el panel web.
Usa la misma base PostgreSQL + PostGIS que el proyecto Laravel (`FIXA_back`), que sigue siendo
el dueño de las tablas: esta API no crea migraciones.

## Patrones de diseño

| Patrón | Tipo | Archivo | Para qué |
|---|---|---|---|
| Factory Method | Creacional | `Modulos/Reportes/Categorias/CreadorCategoria.cs` y subclases | Reglas y tipos de cada categoría (bache, alumbrado, residuos) |
| Singleton | Creacional | `Modulos/Auth/ServicioAutenticacion.cs` | Único punto de las reglas de acceso y del token JWT |
| Builder | Creacional | `Modulos/Reportes/ReporteBuilder.cs` | Armar y guardar un reporte paso a paso, en una sola transacción |
| Adapter | Estructural | `Infraestructura/Fotos/CloudinaryAdapter.cs`, `Infraestructura/Notificaciones/FirebaseAdapter.cs`, `MailgunAdapter.cs` | Servicios externos detrás de interfaces propias (`IAlmacenFotos`, `ICanalNotificacion`) |
| Facade | Estructural | `Modulos/Casos/GestorCasos.cs` | Una sola puerta para gestionar casos: estados (RN-03), historial, plazos y avisos |

## Endpoints

| Método | Ruta | Acceso |
|---|---|---|
| GET | `/api/v1/publico/categorias` | Público |
| GET | `/api/v1/publico/categorias/{codigo}/tipos` | Público |
| POST | `/api/v1/publico/reportes` (multipart, con fotos) | Público, máx. 10 por IP cada 10 min |
| POST | `/api/v1/publico/seguimiento` | Público, máx. 10 por IP por minuto |
| POST | `/api/v1/publico/revisiones` (pedir revisión de un cierre, hasta 15 días) | Público con código y clave, máx. 10 por IP por minuto |
| GET | `/api/v1/publico/mapa` (GeoJSON anónimo, ubicación aproximada) | Público, máx. 10 por IP por minuto |
| POST | `/api/v1/panel/auth/login` | Público, máx. 5 por IP por minuto |
| GET | `/api/v1/panel/auth/yo` | Funcionario con token |
| GET | `/api/v1/panel/casos` y `/api/v1/panel/casos/{id}` | Supervisor |
| PATCH | `/api/v1/panel/casos/{id}/estado` y `/prioridad` | Supervisor |
| POST | `/api/v1/panel/casos/{id}/programaciones` y `/derivaciones` | Supervisor |
| POST | `/api/v1/panel/casos/intervenciones/{id}/validacion` | Supervisor |
| PATCH | `/api/v1/panel/casos/{id}/categoria` (corregir categoría; el caso pasa a su unidad) | Supervisor |
| GET | `/api/v1/panel/casos/{id}/cercanos` (posibles duplicados) | Supervisor |
| POST | `/api/v1/panel/casos/{id}/vinculacion` (marcar duplicado de un caso principal) | Supervisor |
| GET | `/api/v1/panel/casos/revisiones` y POST `/api/v1/panel/casos/revisiones/{id}` | Supervisor |
| GET | `/api/v1/panel/indicadores` y `/api/v1/panel/mapa` (`?desde=&hasta=&distritoId=&categoria=`) | Supervisor (su unidad) y administrador (todas) |
| GET | `/api/v1/panel/ordenes` | Técnico (casos programados para su cuadrilla) |
| POST | `/api/v1/panel/ordenes/{casoId}/inicio` | Técnico |
| POST | `/api/v1/panel/ordenes/{casoId}/intervenciones` (multipart, con evidencia) | Técnico, máx. 10 por IP cada 10 min |

## Configuración (User Secrets en desarrollo, variables de entorno en Render)

| Clave | Obligatoria | Uso |
|---|---|---|
| `ConnectionStrings:Fixa` | Sí | Conexión a PostgreSQL |
| `Jwt:Clave` | Sí | Firma de los tokens (32 caracteres o más) |
| `Cloudinary:CloudName`, `Cloudinary:ApiKey`, `Cloudinary:ApiSecret` | Para registrar reportes | Fotos |
| `Firebase:CredencialesJson` | No | Avisos push; sin ella no se envían |
| `Mailgun:Dominio`, `Mailgun:ApiKey`, `Mailgun:Remitente`, `Mailgun:UrlBase` | No | Avisos por correo; sin ellos no se envían |
| `Cors:Origenes` | No | Orígenes web permitidos (panel) |
| `Vencimientos:Minutos` | No | Cada cuánto se marcan los casos vencidos (por defecto, 15) |

En Render, los dos puntos se escriben como doble guion bajo: `ConnectionStrings__Fixa`, `Jwt__Clave`.

**Fechas:** todo se guarda en hora local de Bolivia, igual que en Laravel. La API abre cada
conexión con la zona `America/La_Paz`, así que los valores por defecto de la base también salen
en hora de Bolivia aunque el servidor de PostgreSQL esté en UTC.

**Vencidos:** un proceso en segundo plano marca los casos abiertos con la fecha objetivo pasada
y deja el evento en el historial. Como Render gratis se apaga sin tráfico, la bandeja y los
indicadores también lo revisan antes de mostrar datos.

## Ejecutar

```bash
cd Fixa.Api
dotnet user-secrets set "ConnectionStrings:Fixa" "Host=127.0.0.1;Port=5433;Database=fixa;Username=fixa;Password=..."
dotnet user-secrets set "Jwt:Clave" "..."
dotnet run
```

En desarrollo, la página de pruebas está en `http://localhost:5112/scalar`.

## Pruebas automáticas y CI

`Fixa.Api.Tests` tiene pruebas de integración (xUnit) que levantan la API completa en memoria
contra una base PostGIS **temporal**: se crea al empezar y se borra al terminar.

| Carpeta | Qué verifica |
|---|---|
| `Registro/` | Reporte válido, idempotencia, foto obligatoria y real, distrito, solo 3 categorías, seguimiento con código y clave, límite por IP, fechas en hora de Bolivia |
| `Permisos/` | Token, roles, unidad del supervisor, cuadrilla del técnico, cuenta desactivada, sin datos de contacto |
| `Estados/` | Máquina de estados RN-03, prioridad, programación, derivación, corrección de categoría, duplicados, vencidos, historial inmutable |
| `Cierre/` | Intervención con evidencia, solo el supervisor cierra (RN-04), rechazo con observación, revisión del cierre en 15 días |
| `Indicadores/` | Indicadores por unidad, filtros, mapa del panel y mapa público anónimo |

Cloudinary, Firebase y Mailgun se reemplazan por adaptadores falsos: las pruebas no gastan
créditos ni necesitan credenciales.

**En tu PC** (con Docker encendido; usa la misma conexión de User Secrets de la API):

```bash
dotnet test
```

**En GitHub** corren solas en cada push y pull request a `main` (`.github/workflows/ci.yml`).
El resultado está en la pestaña **Actions** del repositorio.

**Si Laravel cambia una migración**, actualiza el esquema de las pruebas:

```bash
docker exec fixa_laravel-db-1 pg_dump -U fixa -d fixa --schema-only --no-owner --no-privileges --no-comments > Fixa.Api.Tests/Datos/schema.sql
```

## Seguridad

- Todo endpoint exige token salvo los marcados como públicos; una cuenta desactivada pierde el acceso al instante.
- El supervisor solo ve y modifica los casos de su unidad; el técnico, solo los programados para su cuadrilla, y no puede cerrarlos (RN-04).
- Contraseñas y claves de consulta con bcrypt, verificadas en tiempo constante y con el mismo mensaje de error.
- Límites de peticiones por IP, cabeceras de seguridad, HSTS en producción y errores sin detalles internos.
- Fotos revisadas por su contenido real (JPG, PNG o WebP), máximo 3 de 3 MB.
