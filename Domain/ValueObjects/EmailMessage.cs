namespace Domain.ValueObjects;

public class EmailMessage
{
    public string Destinatario { get; }
    public string Assunto { get; }
    public string CorpoHtml { get; }
    public string CorpoTexto { get; }

    public EmailMessage(string destinatario, string assunto, string corpoHtml, string corpoTexto)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
            throw new ArgumentException("Destinatário é obrigatório.", nameof(destinatario));
        if (string.IsNullOrWhiteSpace(assunto))
            throw new ArgumentException("Assunto é obrigatório.", nameof(assunto));

        Destinatario = destinatario;
        Assunto = assunto;
        CorpoHtml = corpoHtml ?? string.Empty;
        CorpoTexto = corpoTexto ?? string.Empty;
    }
}
