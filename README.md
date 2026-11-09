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
| POST | `/api/v1/panel/auth/login` | Público, máx. 5 por IP por minuto |
| GET | `/api/v1/panel/auth/yo` | Funcionario con token |
| GET | `/api/v1/panel/casos` y `/api/v1/panel/casos/{id}` | Supervisor |
| PATCH | `/api/v1/panel/casos/{id}/estado` y `/prioridad` | Supervisor |
| POST | `/api/v1/panel/casos/{id}/programaciones` y `/derivaciones` | Supervisor |
| POST | `/api/v1/panel/casos/intervenciones/{id}/validacion` | Supervisor |
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

En Render, los dos puntos se escriben como doble guion bajo: `ConnectionStrings__Fixa`, `Jwt__Clave`.

## Ejecutar

```bash
cd Fixa.Api
dotnet user-secrets set "ConnectionStrings:Fixa" "Host=127.0.0.1;Port=5433;Database=fixa;Username=fixa;Password=..."
dotnet user-secrets set "Jwt:Clave" "..."
dotnet run
```

En desarrollo, la página de pruebas está en `http://localhost:5112/scalar`.

## Seguridad

- Todo endpoint exige token salvo los marcados como públicos; una cuenta desactivada pierde el acceso al instante.
- El supervisor solo ve y modifica los casos de su unidad; el técnico, solo los programados para su cuadrilla, y no puede cerrarlos (RN-04).
- Contraseñas y claves de consulta con bcrypt, verificadas en tiempo constante y con el mismo mensaje de error.
- Límites de peticiones por IP, cabeceras de seguridad, HSTS en producción y errores sin detalles internos.
- Fotos revisadas por su contenido real (JPG, PNG o WebP), máximo 3 de 3 MB.
