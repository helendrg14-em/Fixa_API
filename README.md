[![CI](https://github.com/helendrg14-em/Fixa_API/actions/workflows/ci.yml/badge.svg)](https://github.com/helendrg14-em/Fixa_API/actions/workflows/ci.yml)

# FIXA API

API REST de FIXA en ASP.NET Core (.NET 10). La consumen la app del ciudadano y el panel web.
Usa la misma base PostgreSQL + PostGIS que el proyecto Laravel (`FIXA_back`). Laravel crea las
tablas del sistema y esta API crea las suyas con migraciones de Entity Framework Core (ver
"Base de datos").

## Estructura

```
Fixa.Api/
├── Dominio/           entidades (una por tabla), enumeraciones y reglas del negocio
├── Dtos/              objetos de entrada (Solicitud…, Filtro…) y de salida de cada módulo
├── Infraestructura/
│   ├── Datos/         FixaDbContext y las migraciones de EF Core (Migraciones/)
│   ├── Fotos/         adaptador de Cloudinary
│   ├── Notificaciones/ adaptadores de Firebase y Mailgun y el servicio que avisa al vecino
│   └── Seguridad/     cabeceras, límites por IP, errores y validación en español
├── Modulos/           un controlador por recurso: Auth, Casos, Cuadrillas, Indicadores,
│                      Intervenciones, Notificaciones y Reportes
└── Program.cs         inyección de dependencias y configuración
```

Ningún endpoint devuelve entidades ni objetos anónimos: todo entra y sale como DTO (`Dtos/`).

## Patrones de diseño

| Patrón | Tipo | Archivo | Para qué |
|---|---|---|---|
| Factory Method | Creacional | `Modulos/Reportes/Categorias/CreadorCategoria.cs` y subclases | Reglas y tipos de cada categoría (bache, alumbrado, residuos) |
| Singleton | Creacional | `Modulos/Auth/ServicioAutenticacion.cs` | Único punto de las reglas de acceso y del token JWT |
| Builder | Creacional | `Modulos/Reportes/ReporteBuilder.cs` | Armar y guardar un reporte paso a paso, en una sola transacción |
| Adapter | Estructural | `Infraestructura/Fotos/CloudinaryAdapter.cs`, `Infraestructura/Notificaciones/FirebaseAdapter.cs`, `MailgunAdapter.cs` | Servicios externos detrás de interfaces propias (`IAlmacenFotos`, `ICanalNotificacion`) |
| Facade | Estructural | `Modulos/Casos/GestorCasos.cs` | Una sola puerta para gestionar casos: estados (RN-03), historial, plazos y avisos |
| Strategy | Comportamiento | `Modulos/Casos/PlazoAtencion.cs` (contexto) y `Modulos/Casos/Plazos/` | Calcular la fecha objetivo en horas corridas o en días hábiles según la meta (RN-06) |
| Chain of Responsibility | Comportamiento | `Modulos/Reportes/Validacion/` | Validar el reporte eslabón por eslabón; el apoyo arma una cadena más corta con los mismos eslabones |
| Template Method | Comportamiento | `Modulos/Reportes/AccionesVecino/AccionDelVecino.cs` y `Acciones.cs` | Toda acción con código y clave verifica primero el acceso y luego hace lo suyo (seguimiento, revisión, conformidad, token) |

## Endpoints

| Método | Ruta | Acceso |
|---|---|---|
| GET | `/salud` (comprueba la base; Render la usa como health check y el panel y la app, para despertar la API) | Público |
| GET | `/api/v1/publico/categorias` | Público |
| GET | `/api/v1/publico/categorias/{codigo}/tipos` | Público |
| POST | `/api/v1/publico/reportes` (multipart, con fotos) | Público, máx. 10 por IP cada 10 min |
| POST | `/api/v1/publico/seguimiento` | Público, máx. 10 por IP por minuto |
| POST | `/api/v1/publico/revisiones` (pedir revisión de un cierre, hasta 15 días) | Público con código y clave, máx. 10 por IP por minuto |
| GET | `/api/v1/publico/mapa` (GeoJSON anónimo, ubicación aproximada) | Público, máx. 10 por IP por minuto |
| POST | `/api/v1/publico/conformidad` (el vecino confirma o no la solución, hasta 15 días) | Público con código y clave, máx. 10 por IP por minuto |
| PATCH | `/api/v1/publico/dispositivo` (guardar o quitar el token del celular para los avisos push) | Público con código y clave, máx. 10 por IP por minuto |
| GET | `/api/v1/publico/casos/cercanos?categoria=&latitud=&longitud=` (casos abiertos en el radio de la categoría, sin datos personales) | Público, máx. 10 por IP por minuto |
| POST | `/api/v1/publico/casos/{casoId}/apoyos` (apoyar un caso cercano, sin foto; recibe su propio código y clave) | Público, solo dentro del radio del caso, máx. 10 por IP cada 10 min |
| POST | `/api/v1/panel/auth/login` | Público, máx. 5 por IP por minuto |
| GET | `/api/v1/panel/auth/yo` | Funcionario con token |
| GET | `/api/v1/panel/casos` y `/api/v1/panel/casos/{id}` | Supervisor (su unidad) y administrador (todas) |
| PATCH | `/api/v1/panel/casos/{id}/estado` y `/prioridad` | Supervisor (su unidad) y administrador (todas) |
| POST | `/api/v1/panel/casos/{id}/programaciones` y `/derivaciones` | Supervisor (su unidad) y administrador (todas) |
| POST | `/api/v1/panel/casos/intervenciones/{id}/validacion` | Supervisor (su unidad) y administrador (todas) |
| PATCH | `/api/v1/panel/casos/{id}/categoria` (corregir categoría; el caso pasa a su unidad) | Supervisor (su unidad) y administrador (todas) |
| GET | `/api/v1/panel/casos/{id}/cercanos` (posibles duplicados) | Supervisor (su unidad) y administrador (todas) |
| POST | `/api/v1/panel/casos/{id}/vinculacion` (marcar duplicado de un caso principal) | Supervisor (su unidad) y administrador (todas) |
| GET | `/api/v1/panel/casos/revisiones` y POST `/api/v1/panel/casos/revisiones/{id}` | Supervisor (su unidad) y administrador (todas) |
| GET | `/api/v1/panel/indicadores` y `/api/v1/panel/mapa` (`?desde=&hasta=&distritoId=&categoria=`) | Supervisor (su unidad) y administrador (todas) |
| POST | `/api/v1/panel/reportes` (multipart; registro por teléfono o en ventanilla, `canal` telefono o presencial, foto o `motivoSinFoto`; responde 201 con `Location` del caso) | Administrador |
| GET | `/api/v1/panel/cuadrillas`, `/api/v1/panel/cuadrillas/{id}` y `/api/v1/panel/cuadrillas/{id}/tecnicos` | Supervisor (su unidad) y administrador (todas) |
| POST | `/api/v1/panel/cuadrillas` (201 con `Location`; el administrador indica `unidadId`) | Supervisor (su unidad) y administrador (todas) |
| PUT | `/api/v1/panel/cuadrillas/{id}` (nombre y turno: `Mañana` o `Tarde`) | Supervisor (su unidad) y administrador (todas) |
| DELETE | `/api/v1/panel/cuadrillas/{id}` (solo sin técnicos ni programaciones) | Supervisor (su unidad) y administrador (todas) |
| GET | `/api/v1/panel/ordenes` | Técnico (casos programados para su cuadrilla) |
| POST | `/api/v1/panel/ordenes/{casoId}/inicio` | Técnico |
| POST | `/api/v1/panel/ordenes/{casoId}/intervenciones` (multipart, con evidencia) | Técnico, máx. 10 por IP cada 10 min |
| GET | `/api/v1/panel/plantillas-notificacion` y `/{id}` (texto del aviso de cada estado del caso) | Administrador |
| POST | `/api/v1/panel/plantillas-notificacion` (201 con `Location`; un estado tiene una sola plantilla) | Administrador |
| PUT | `/api/v1/panel/plantillas-notificacion/{id}` (`activa: false` deja de avisar ese estado) | Administrador |
| DELETE | `/api/v1/panel/plantillas-notificacion/{id}` (ese estado vuelve al texto por defecto) | Administrador |
| GET | `/api/v1/panel/notificaciones?estadoEntrega=&casoId=` y `/{id}` (avisos enviados, sin el contacto del vecino) | Supervisor (su unidad) y administrador (todas) |
| POST | `/api/v1/panel/notificaciones/{id}/reintento` (solo avisos no entregados; usa el contacto actual del vecino) | Supervisor (su unidad) y administrador (todas) |

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
| `Proxy:DetrasDeRender` | Solo en Render (`true`) | Lee la IP real del vecino desde el proxy de Render, para que los límites por IP funcionen. No activarlo fuera de Render |

En Render, los dos puntos se escriben como doble guion bajo: `ConnectionStrings__Fixa`, `Jwt__Clave`.

**Fechas:** todo se guarda en hora local de Bolivia, igual que en Laravel. La API abre cada
conexión con la zona `America/La_Paz`, así que los valores por defecto de la base también salen
en hora de Bolivia aunque el servidor de PostgreSQL esté en UTC.

**Vencidos:** un proceso en segundo plano marca los casos abiertos con la fecha objetivo pasada
y deja el evento en el historial. Como Render gratis se apaga sin tráfico, la bandeja y los
indicadores también lo revisan antes de mostrar datos.

## Base de datos

| Tablas | Las crea | Dónde |
|---|---|---|
| Las del sistema (`usuarios`, `casos`, `reportes`, `notificaciones`…) | Laravel | Migraciones de [FIXA_back](https://github.com/helendrg14-em/FIXA_back) |
| `plantillas_notificacion` (textos de los avisos, con un registro inicial por estado) | Esta API | `Infraestructura/Datos/Migraciones` (EF Core) |

- La primera migración de EF Core, `EsquemaDeLaravel`, solo registra el modelo de las tablas de
  Laravel: no crea nada. Las siguientes crean las tablas propias de la API.
- **La API aplica sus migraciones sola al iniciar** (`Database.Migrate()` en `Program.cs`). A mano:
  `dotnet ef database update`.
- Toda tabla nueva activa RLS en su migración, igual que en Laravel (Supabase publica el esquema).
- En `usuarios`, el perfil de cada funcionario está en la columna **`rol`** (`administrador`,
  `supervisor`, `tecnico`, `operador`).

Para cambiar una tabla de Laravel: primero la migración en Laravel, después el modelo de EF Core
y se regenera `Fixa.Api.Tests/Datos/schema.sql` (ver "Pruebas automáticas y CI"). Para una tabla
de la API: el modelo y luego `dotnet ef migrations add NombreDelCambio --output-dir Infraestructura/Datos/Migraciones`.

**Al desplegar**, primero las migraciones de Laravel contra Supabase (`php artisan migrate --force`)
y después esta API.

## Qué instalar (una sola vez)

| Programa | Para qué | Cómo comprobarlo |
|---|---|---|
| [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) | Compilar y ejecutar la API | `dotnet --version` (10.x) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | La base de datos y las pruebas | `docker --version` |
| El proyecto [FIXA_back](https://github.com/helendrg14-em/FIXA_back) ya en marcha | Crea la base y sus tablas | Su README, pasos 1 y 2 |

La API **no tiene base propia**: usa la que levanta Laravel con `docker compose` (puerto 5433).
Por eso, primero se pone en marcha FIXA_back y después esta API.

Para editar sirve VS Code con la extensión **C# Dev Kit**, o Visual Studio 2022 o superior.

## Ejecutar

```bash
git clone https://github.com/helendrg14-em/Fixa_API.git
cd Fixa_API/Fixa.Api
```

Las credenciales van en **User Secrets**, que se guardan fuera del repositorio. Usa los mismos
datos `DB_*` de tu `.env` de Laravel:

```bash
dotnet user-secrets set "ConnectionStrings:Fixa" "Host=127.0.0.1;Port=5433;Database=fixa;Username=fixa;Password=la_de_tu_env"
dotnet user-secrets set "Jwt:Clave" "una_clave_aleatoria_de_32_caracteres_o_más"
dotnet run
```

- Comprobar que funciona: http://localhost:5112/salud debe responder `{"estado":"ok"}`.
- Probar los endpoints: http://localhost:5112/swagger o http://localhost:5112/scalar (solo en desarrollo).

**Para conectar el panel**, copia la misma `Jwt:Clave` en el `.env` de Laravel como
`FIXA_API_JWT_CLAVE` y pon `FIXA_API_URL=http://localhost:5112`.

Cloudinary, Firebase y Mailgun son opcionales en tu PC: sin ellos la API arranca, pero no puede
guardar fotos de reportes reales. Las pruebas automáticas no los necesitan.

### Problemas comunes

| Síntoma | Causa | Solución |
|---|---|---|
| `Jwt:Clave debe tener al menos 32 caracteres` | Falta el User Secret | Repite `dotnet user-secrets set "Jwt:Clave" ...` |
| `/salud` responde 503 o hay error de conexión | La base de Docker no está encendida | En FIXA_back: `docker compose up -d --wait` |
| `relation "categorias" does not exist` | La base está vacía | En FIXA_back: `php artisan migrate` y `php artisan db:seed` |
| El panel dice "No se pudo completar la acción" | La clave del panel no es la misma | `FIXA_API_JWT_CLAVE` debe ser idéntica a `Jwt:Clave` |

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
| `Notificaciones/` | Rol en el login, CRUD de plantillas de aviso, texto del aviso según la plantilla, reintento con el contacto nuevo, sin datos de contacto |

Cloudinary, Firebase y Mailgun se reemplazan por adaptadores falsos: las pruebas no gastan
créditos ni necesitan credenciales.

**En tu PC** (con Docker encendido; usa la misma conexión de User Secrets de la API):

```bash
dotnet test
```

**En GitHub** corren solas en cada push y pull request a `main` (`.github/workflows/ci.yml`).
El resultado está en la pestaña **Actions** del repositorio.

**Si Laravel cambia una migración**, actualiza el esquema de las pruebas:

Desde la carpeta de FIXA_back (funciona aunque tu carpeta tenga otro nombre), con los datos de tu `.env`:

```bash
docker compose exec -T db pg_dump -U fixa -d fixa --schema-only --no-owner --no-privileges --no-comments --exclude-table=plantillas_notificacion --exclude-table='"__EFMigrationsHistory"' > ../Fixa_API/Fixa.Api.Tests/Datos/schema.sql
```

Las tablas de la API se excluyen porque las pruebas las crean con las migraciones de EF Core. Ajusta `-U` y `-d` si tu `DB_USERNAME` o `DB_DATABASE` son distintos, y la ruta final si no clonaste los dos repositorios uno al lado del otro.

## Seguridad

- Todo endpoint exige token salvo los marcados como públicos; una cuenta desactivada pierde el acceso al instante.
- El supervisor solo ve y modifica los casos de su unidad; el técnico, solo los programados para su cuadrilla, y no puede cerrarlos (RN-04).
- Contraseñas (costo 12) y claves de consulta del vecino (costo 10, para que el seguimiento responda rápido) con bcrypt, verificadas en tiempo constante y con el mismo mensaje de error.
- Errores de validación en español y sin detalles internos: un JSON mal formado no muestra tipos de C# ni posiciones.
- Límites de peticiones por IP, cabeceras de seguridad, HSTS en producción y errores sin detalles internos.
- Fotos revisadas por su contenido real (JPG, PNG o WebP), máximo 3 de 3 MB.
- Detrás del proxy de Render, la IP real del vecino se lee solo del último salto (`ForwardLimit = 1`), así no se puede falsificar.
- Apoyar un caso exige estar dentro de su radio de cercanía, para que nadie infle los apoyos desde lejos (OWASP API6).
- Consultas geográficas con parámetros (nunca texto pegado en el SQL); los casos cercanos no muestran códigos, descripciones ni contactos (RN-07).
