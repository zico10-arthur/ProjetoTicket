using Domain.ValueObjects;

namespace Domain.Interface;

public interface IEmailSender
{
    /// <summary>
    /// Enfileira um e-mail para envio assíncrono em background.
    /// Retorna imediatamente — o envio real ocorre em background.
    /// </summary>
    ValueTask EnfileirarAsync(EmailMessage email, CancellationToken ct);
}
