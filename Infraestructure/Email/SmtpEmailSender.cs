using Domain.ValueObjects;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Infraestructure.Email;

public class SmtpEmailSender
{
    private readonly SmtpSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public bool Configurado =>
        !string.IsNullOrWhiteSpace(_settings.Host) &&
        !string.IsNullOrWhiteSpace(_settings.FromAddress);

    public async Task EnviarAsync(EmailMessage email, CancellationToken ct)
    {
        if (!Configurado)
        {
            _logger.LogWarning("SMTP não configurado. E-mail descartado para {Destinatario}", email.Destinatario);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
        message.To.Add(MailboxAddress.Parse(email.Destinatario));
        message.Subject = email.Assunto;

        var builder = new BodyBuilder
        {
            HtmlBody = email.CorpoHtml,
            TextBody = email.CorpoTexto
        };
        message.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port,
            _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, ct);

        if (!string.IsNullOrWhiteSpace(_settings.Username))
            await client.AuthenticateAsync(_settings.Username, _settings.Password, ct);

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
