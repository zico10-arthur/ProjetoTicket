using Domain.Interface;

namespace Api.BackgroundTasks;

public class LiberacaoAssentosWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LiberacaoAssentosWorker> _logger;

    public LiberacaoAssentosWorker(IServiceProvider serviceProvider, ILogger<LiberacaoAssentosWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Robô de liberação de assentos iniciado.");

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var ingressoRepository = scope.ServiceProvider.GetRequiredService<IIngressoRepository>();
                
                var reservaRepository = scope.ServiceProvider.GetRequiredService<IReservaRepository>();

                int minutosParaExpirar = 15;

                await reservaRepository.DeletarReservasNaoPagasExpiradas(minutosParaExpirar, stoppingToken);

                await ingressoRepository.LiberarAssentosExpirados(minutosParaExpirar, stoppingToken);
                
                _logger.LogInformation($"[{DateTime.Now}] Varredura concluída: Reservas e Assentos expirados foram limpos.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao liberar assentos expirados.");
            }
        }
    }
}