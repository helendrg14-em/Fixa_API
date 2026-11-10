using Fixa.Api.Infraestructura.Seguridad;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Fixa.Api.Tests.Infraestructura;

/// <summary>
/// Crea una base de datos nueva para cada ejecución de las pruebas, con el esquema de Laravel
/// (Datos/schema.sql) y datos ficticios, y la borra al terminar. Como el historial es
/// inmutable, las pruebas no limpian datos: cada una registra sus propios reportes.
///
/// El servidor sale de la variable FIXA_PRUEBAS_DB (en GitHub Actions) o, en tu PC, de la
/// misma conexión que la API tiene en User Secrets. Así no hay contraseñas en el repositorio.
/// </summary>
public sealed class EntornoPruebas : IAsyncLifetime
{
    public const string Password = "Prueba-12345";

    private readonly NpgsqlConnectionStringBuilder servidor;
    private readonly Dictionary<string, string> tokens = [];

    public EntornoPruebas()
    {
        var configuracion = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var conexion = configuracion["FIXA_PRUEBAS_DB"] ?? configuracion.GetConnectionString("Fixa")
            ?? throw new InvalidOperationException("Define FIXA_PRUEBAS_DB o ConnectionStrings:Fixa en User Secrets.");
        servidor = new NpgsqlConnectionStringBuilder(conexion);
        BaseDatos = $"fixa_pruebas_{Guid.NewGuid():N}"[..21];
    }

    public string BaseDatos { get; }

    public string Conexion => new NpgsqlConnectionStringBuilder(servidor.ConnectionString) { Database = BaseDatos }.ConnectionString;

    public FabricaApi Api { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await EjecutarEnServidorAsync($"CREATE DATABASE {BaseDatos}");
        await EjecutarAsync(await LeerAsync("schema.sql"));
        await EjecutarAsync(await LeerAsync("datos_base.sql"));
        await CrearUsuariosAsync();
        Api = new FabricaApi(Conexion);
    }

    public async Task DisposeAsync()
    {
        await Api.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        await EjecutarEnServidorAsync($"DROP DATABASE IF EXISTS {BaseDatos} WITH (FORCE)");
    }

    /// <summary>Token JWT de un funcionario de prueba (se pide una sola vez por usuario).</summary>
    public async Task<string> TokenAsync(string correo)
    {
        if (tokens.TryGetValue(correo, out var token))
            return token;
        using var cliente = Api.CreateClient();
        var respuesta = await cliente.PostAsJsonAsync("/api/v1/panel/auth/login", new { correo, password = Password });
        respuesta.EnsureSuccessStatusCode();
        token = (await respuesta.Content.ReadFromJsonAsync<RespuestaLogin>())!.Token;
        return tokens[correo] = token;
    }

    public async Task<T?> EscalarAsync<T>(string sql, params object[] valores)
    {
        await using var conexion = new NpgsqlConnection(Conexion);
        await conexion.OpenAsync();
        await using var comando = Comando(conexion, sql, valores);
        var resultado = await comando.ExecuteScalarAsync();
        return resultado is null or DBNull ? default : (T)resultado;
    }

    public async Task EjecutarAsync(string sql, params object[] valores)
    {
        await using var conexion = new NpgsqlConnection(Conexion);
        await conexion.OpenAsync();
        await using var comando = Comando(conexion, sql, valores);
        await comando.ExecuteNonQueryAsync();
    }

    /// <summary>Crea un funcionario de prueba y devuelve su id.</summary>
    public async Task<long> CrearUsuarioAsync(string correo, string tipo, long? unidad, long? cuadrilla, bool activo = true) =>
        await EscalarAsync<long>(
            """
            INSERT INTO usuarios (tipo, nombres, apellidos, correo, password_hash, activo, debe_registrar_password, unidad_id, cuadrilla_id)
            VALUES ($1, 'Prueba', $2, $3, $4, $5, false, $6, $7) RETURNING id
            """,
            tipo, tipo, correo, VerificadorClave.Hash(Password), activo, (object?)unidad ?? DBNull.Value, (object?)cuadrilla ?? DBNull.Value);

    private async Task CrearUsuariosAsync()
    {
        await CrearUsuarioAsync(Usuarios.SupervisorVialidad, "supervisor", unidad: 1, cuadrilla: null);
        await CrearUsuarioAsync(Usuarios.SupervisorAlumbrado, "supervisor", unidad: 2, cuadrilla: null);
        await CrearUsuarioAsync(Usuarios.TecnicoCuadrillaA, "tecnico", unidad: 1, cuadrilla: 1);
        await CrearUsuarioAsync(Usuarios.TecnicoCuadrillaB, "tecnico", unidad: 1, cuadrilla: 2);
        await CrearUsuarioAsync(Usuarios.Administrador, "administrador", unidad: null, cuadrilla: null);
    }

    private static NpgsqlCommand Comando(NpgsqlConnection conexion, string sql, object[] valores)
    {
        var comando = new NpgsqlCommand(sql, conexion);
        foreach (var valor in valores)
            comando.Parameters.Add(new NpgsqlParameter { Value = valor });
        return comando;
    }

    private async Task EjecutarEnServidorAsync(string sql)
    {
        var conexion = new NpgsqlConnectionStringBuilder(servidor.ConnectionString) { Database = "postgres", Pooling = false };
        await using var conectado = new NpgsqlConnection(conexion.ConnectionString);
        await conectado.OpenAsync();
        await using var comando = new NpgsqlCommand(sql, conectado);
        await comando.ExecuteNonQueryAsync();
    }

    private static Task<string> LeerAsync(string archivo) =>
        File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Datos", archivo));

    private sealed record RespuestaLogin(string Token);
}
