using Fixa.Api.Infraestructura;
using Fixa.Api.Infraestructura.Datos;
using Fixa.Api.Infraestructura.Seguridad;
using Fixa.Api.Modulos.Auth;
using Fixa.Api.Modulos.Casos;
using Fixa.Api.Modulos.Indicadores;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Ninguna petición puede superar 10 MB (el registro admite hasta 3 fotos de 3 MB).
builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = 10 * 1024 * 1024);

// Las fechas se guardan en hora local de Bolivia (convención de Laravel). Con la zona de la
// sesión en America/La_Paz, los valores por defecto de la base (CURRENT_TIMESTAMP) también lo
// están, aunque el servidor de PostgreSQL esté en UTC (Docker, Neon, Supabase).
var conexion = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Fixa")) { Timezone = "America/La_Paz" };
builder.Services.AddDbContext<FixaDbContext>(opciones => opciones.UseNpgsql(
    conexion.ConnectionString,
    npgsql => npgsql.UseNetTopologySuite()));

// Singleton: se configura una sola vez al arrancar, con la clave de User Secrets o de Render.
ServicioAutenticacion.Instancia.Configurar(builder.Configuration["Jwt:Clave"]);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.MapInboundClaims = true;
        opciones.TokenValidationParameters = ServicioAutenticacion.Instancia.ParametrosValidacion();
        // Una cuenta desactivada pierde el acceso aunque su token siga vigente (igual que en Filament).
        opciones.Events = new JwtBearerEvents
        {
            OnTokenValidated = async contexto =>
            {
                var db = contexto.HttpContext.RequestServices.GetRequiredService<FixaDbContext>();
                var id = ServicioAutenticacion.Instancia.IdUsuario(contexto.Principal!);
                var usuario = await db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
                if (usuario is null || !ServicioAutenticacion.Instancia.PuedeEntrar(usuario))
                    contexto.Fail("Cuenta sin acceso.");
            },
        };
    });

// Seguro por defecto: todo endpoint exige token salvo los marcados con [AllowAnonymous].
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddCors(cors => cors.AddDefaultPolicy(politica => politica
    .WithOrigins(builder.Configuration.GetSection("Cors:Origenes").Get<string[]>() ?? [])
    .WithMethods("GET", "POST", "PATCH")
    .WithHeaders("Authorization", "Content-Type")));

builder.Services.AddServiciosExternos();
builder.Services.AddScoped<PlazoAtencion>();
builder.Services.AddScoped<GestorCasos>();
builder.Services.AddScoped<ControlVencimientos>();
builder.Services.AddScoped<ConsultaIndicadores>();
builder.Services.AddHostedService<VigilanteVencimientos>();
builder.Services.AddLimitesDePeticiones();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorErrores>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCabecerasSeguridad();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    app.MapScalarApiReference().AllowAnonymous();
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription().AllowAnonymous();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite que las pruebas de integración levanten la API (WebApplicationFactory<Program>).
public partial class Program;
