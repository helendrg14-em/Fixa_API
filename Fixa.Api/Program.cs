using Fixa.Api.Modulos.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Fixa.Api.Infraestructura.Datos;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<FixaDbContext>(opciones =>
    opciones.UseNpgsql(
        builder.Configuration.GetConnectionString("Fixa"),
        npgsql => npgsql.UseNetTopologySuite()));
    
// Singleton: se configura una sola vez al arrancar.
ServicioAutenticacion.Instancia.Configurar(builder.Configuration["Jwt:Clave"]
    ?? throw new InvalidOperationException("Falta Jwt:Clave en User Secrets."));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones => opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = ServicioAutenticacion.Instancia.Clave,
        ClockSkew = TimeSpan.Zero, // el token vence exactamente a los 15 minutos
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
     app.MapScalarApiReference();
    app.MapGet("/", () => Results.Redirect("/scalar")).ExcludeFromDescription();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
