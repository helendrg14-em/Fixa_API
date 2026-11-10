namespace Fixa.Api.Modulos.Casos;

/// <summary>
/// Proceso en segundo plano que revisa los vencimientos cada cierto tiempo (por defecto, 15
/// minutos; se cambia con Vencimientos:Minutos). En el plan gratuito de Render la API se apaga
/// sin tráfico; por eso la bandeja y los indicadores también revisan antes de mostrar datos.
/// </summary>
public sealed class VigilanteVencimientos(IServiceScopeFactory alcances, IConfiguration configuracion, ILogger<VigilanteVencimientos> log)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        var minutos = Math.Max(1, configuracion.GetValue("Vencimientos:Minutos", 15));
        using var reloj = new PeriodicTimer(TimeSpan.FromMinutes(minutos));
        do
        {
            try
            {
                await using var alcance = alcances.CreateAsyncScope();
                var marcados = await alcance.ServiceProvider.GetRequiredService<ControlVencimientos>().MarcarAsync(detener);
                if (marcados > 0)
                    log.LogInformation("Se marcaron {Cantidad} casos como vencidos.", marcados);
            }
            catch (Exception error) when (error is not OperationCanceledException)
            {
                // Si la base no responde, se intenta en la siguiente vuelta; la API sigue funcionando.
                log.LogError(error, "No se pudieron revisar los vencimientos.");
            }
        }
        while (await reloj.WaitForNextTickAsync(detener));
    }
}
