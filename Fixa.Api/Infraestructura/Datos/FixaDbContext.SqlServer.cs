using Fixa.Api.Dominio;
using Fixa.Api.Dominio.Entidades;
using Fixa.Api.Infraestructura.Datos.Consultas;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace Fixa.Api.Infraestructura.Datos;

/// <summary>
/// Soporte de SQL Server. En PostgreSQL (producción) el esquema lo crean las migraciones de
/// Laravel; en SQL Server lo crean las migraciones de EF Core de Datos/MigracionesSqlServer,
/// con los ajustes de tipos y los datos de prueba de esta clase.
/// </summary>
public partial class FixaDbContext
{
    private IConsultasMotor? consultas;

    /// <summary>Consultas propias del motor en uso (Strategy).</summary>
    public IConsultasMotor Consultas =>
        consultas ??= Database.IsSqlServer() ? new ConsultasSqlServer(this) : new ConsultasPostgreSql(this);

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        if (!Database.IsSqlServer())
            return;

        AjustarTiposParaSqlServer(modelBuilder);
        CargarDatosDePrueba(modelBuilder);
    }

    /// <summary>
    /// timestamp(0) pasa a datetime2(0) y las ubicaciones a geography, el tipo de SQL Server que
    /// mide distancias en metros sobre la Tierra (como geography de PostGIS).
    /// </summary>
    private static void AjustarTiposParaSqlServer(ModelBuilder modelBuilder)
    {
        foreach (var propiedad in modelBuilder.Model.GetEntityTypes().SelectMany(entidad => entidad.GetProperties()))
        {
            var tipo = propiedad.GetColumnType();
            if (tipo is null)
                continue;
            if (tipo.StartsWith("timestamp", StringComparison.Ordinal))
                propiedad.SetColumnType("datetime2(0)");
            else if (tipo.StartsWith("geometry", StringComparison.Ordinal))
                propiedad.SetColumnType("geography");
        }
    }

    /// <summary>
    /// Datos ficticios para probar la API: catálogos, dos distritos y un funcionario de cada
    /// perfil. La contraseña de prueba de todos es Fixa-Demo-2026 (ver README).
    /// </summary>
    private static void CargarDatosDePrueba(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UnidadResponsable>().HasData(
            new UnidadResponsable { Id = 1, Nombre = "Secretaría de Obras Públicas (vialidad)", Sigla = "SMOP" },
            new UnidadResponsable { Id = 2, Nombre = "Dirección de Alumbrado Público", Sigla = "DAP" },
            new UnidadResponsable { Id = 3, Nombre = "Empresa Municipal de Aseo", Sigla = "EMACRUZ" });

        modelBuilder.Entity<Categoria>().HasData(
            new Categoria { Id = 1, UnidadId = 1, Codigo = "bache", Nombre = "Baches y deterioro vial", Descripcion = "Baches, hundimientos y calzada dañada.", Activa = true, RadioCercaniaM = 30 },
            new Categoria { Id = 2, UnidadId = 2, Codigo = "alumbrado", Nombre = "Fallas de alumbrado público", Descripcion = "Luminarias apagadas, intermitentes o dañadas.", Activa = true, RadioCercaniaM = 30 },
            new Categoria { Id = 3, UnidadId = 3, Codigo = "residuos", Nombre = "Microbasurales / residuos", Descripcion = "Acumulación de basura en la vía pública.", Activa = true, RadioCercaniaM = 30 });

        // Anillos en sentido antihorario: SQL Server exige esa orientación en geography.
        modelBuilder.Entity<Distrito>().HasData(
            new Distrito { Id = 1, Numero = 1, Nombre = "Distrito 1 (prueba)", Limite = Rectangulo(-63.20, -17.80, -63.16, -17.76) },
            new Distrito { Id = 2, Numero = 2, Nombre = "Distrito 2 (prueba)", Limite = Rectangulo(-63.16, -17.80, -63.12, -17.76) });

        modelBuilder.Entity<Cuadrilla>().HasData(
            new Cuadrilla { Id = 1, UnidadId = 1, Nombre = "Cuadrilla Vial A", Turno = "manana" },
            new Cuadrilla { Id = 2, UnidadId = 2, Nombre = "Cuadrilla Alumbrado A", Turno = "tarde" });

        modelBuilder.Entity<EntidadExterna>().HasData(
            new EntidadExterna { Id = 1, Nombre = "CRE (distribución eléctrica)", Contacto = "Línea de reclamos" });

        var desde = new DateOnly(2026, 1, 1);
        modelBuilder.Entity<MetaAtencion>().HasData(
            new MetaAtencion { Id = 1, CategoriaId = 1, Prioridad = Prioridad.Alta.ABd(), PlazoHoras = 72, VigenteDesde = desde },
            new MetaAtencion { Id = 2, CategoriaId = 1, Prioridad = Prioridad.Media.ABd(), PlazoDiasHabiles = 5, VigenteDesde = desde },
            new MetaAtencion { Id = 3, CategoriaId = 2, Prioridad = Prioridad.Alta.ABd(), PlazoHoras = 48, VigenteDesde = desde },
            new MetaAtencion { Id = 4, CategoriaId = 3, Prioridad = Prioridad.Media.ABd(), PlazoDiasHabiles = 3, VigenteDesde = desde });

        modelBuilder.Entity<Feriado>().HasData(
            new Feriado { Id = 1, Fecha = new DateOnly(2026, 12, 25), Nombre = "Navidad", Fuente = "Datos de prueba" });

        // Hash bcrypt (costo 12) de la contraseña de prueba Fixa-Demo-2026.
        const string hashPrueba = "$2a$12$uAa4dbkixagNbb2.EatC9eABiTh9zyYAoE8U9Uj2BIj1KLPI8l.Tq";
        var creado = new DateTime(2026, 11, 20, 8, 0, 0);
        modelBuilder.Entity<Usuario>().HasData(
            Funcionario(1, TipoUsuario.Administrador, "Ana", "Administradora", "admin@fixa.test", hashPrueba, creado),
            Funcionario(2, TipoUsuario.Supervisor, "Sergio", "Supervisor", "supervisor@fixa.test", hashPrueba, creado, unidadId: 1),
            Funcionario(3, TipoUsuario.Tecnico, "Teresa", "Técnica", "tecnico@fixa.test", hashPrueba, creado, unidadId: 1, cuadrillaId: 1),
            Funcionario(4, TipoUsuario.Operador, "Óscar", "Operador", "operador@fixa.test", hashPrueba, creado, distritoId: 1));
    }

    private static Usuario Funcionario(
        long id, TipoUsuario tipo, string nombres, string apellidos, string correo, string hash, DateTime creado,
        long? unidadId = null, long? cuadrillaId = null, long? distritoId = null) => new()
    {
        Id = id,
        Tipo = tipo.ABd(),
        Nombres = nombres,
        Apellidos = apellidos,
        Correo = correo,
        PasswordHash = hash,
        Activo = true,
        DebeRegistrarPassword = false,
        Requiere2fa = false,
        FechaCreacion = creado,
        UnidadId = unidadId,
        CuadrillaId = cuadrillaId,
        DistritoId = distritoId,
    };

    private static Polygon Rectangulo(double oeste, double sur, double este, double norte) =>
        new(new LinearRing([
            new Coordinate(oeste, sur), new Coordinate(este, sur), new Coordinate(este, norte),
            new Coordinate(oeste, norte), new Coordinate(oeste, sur),
        ])) { SRID = 4326 };
}
