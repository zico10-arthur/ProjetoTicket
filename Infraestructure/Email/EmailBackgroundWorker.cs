using System.Threading.Channels;
using Domain.Interface;
using Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infraestructure.Email;

public class EmailBackgroundWorker : BackgroundService, IEmailSender
{
    private readonly Channel<EmailMessage> _fila;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailBackgroundWorker> _logger;

    public EmailBackgroundWorker(IServiceScopeFactory scopeFactory, ILogger<EmailBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _fila = Channel.CreateBounded<EmailMessage>(
            new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            });
    }

    public async ValueTask EnfileirarAsync(EmailMessage email, CancellationToken ct)
    {
        await _fila.Writer.WriteAsync(email, ct);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _fila.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessarComRetentativa(email, stoppingToken);
        }
    }

    private async Task ProcessarComRetentativa(EmailMessage email, CancellationToken ct)
    {
        var tentativas = 3;
        for (int i = 0; i < tentativas; i++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<SmtpEmailSender>();
                var enviado = await sender.EnviarAsync(email, ct);
                if (enviado)
                {
                    _logger.LogInformation("E-mail enviado para {Destinatario}: {Assunto}",
                        email.Destinatario, email.Assunto);
                }
                return;
            }
            catch (Exception ex) when (i < tentativas - 1)
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, i + 1)); // 2s, 4s, 8s
                _logger.LogWarning(ex,
                    "Tentativa {Tentativa}/{Total} falhou para {Destinatario}. Retentando em {Delay}s.",
                    i + 1, tentativas, email.Destinatario, delay.TotalSeconds);
                await Task.Delay(delay, ct);
            }
        }
        _logger.LogError("Falha ao enviar e-mail para {Destinatario} após {Tentativas} tentativas.",
            email.Destinatario, tentativas);
    }
}
