using Domain.Interface;
using Hangfire;

namespace Api.BackgroundTasks;

public class LiberacaoAssentosJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<LiberacaoAssentosJob> _logger;

    public LiberacaoAssentosJob(
        IServiceProvider serviceProvider,
        ILogger<LiberacaoAssentosJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 120)]
    public async Task ExecutarAsync(CancellationToken ct)
    {
        _logger.LogInformation(
            "Hangfire: Iniciando liberação de assentos expirados.");

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ingressoRepository = scope.ServiceProvider
                .GetRequiredService<IIngressoRepository>();
            var reservaRepository = scope.ServiceProvider
                .GetRequiredService<IReservaRepository>();

            int minutosParaExpirar = 15;

            await reservaRepository.DeletarReservasNaoPagasExpiradas(
                minutosParaExpirar, ct);

            await ingressoRepository.LiberarAssentosExpirados(
                minutosParaExpirar, ct);

            _logger.LogInformation(
                "[{Time}] Hangfire: Varredura concluída. " +
                "Reservas e Assentos expirados foram limpos.",
                DateTime.Now);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Hangfire: Erro ao liberar assentos expirados.");
            throw;
        }
    }
}