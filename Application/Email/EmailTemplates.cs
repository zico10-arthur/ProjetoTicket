using Domain.ValueObjects;

namespace Application.Email;

public static class EmailTemplates
{
    public static EmailMessage BoasVindasComprador(string destinatario, string nome)
    {
        var assunto = "Bem-vindo ao SoldOut Tickets!";
        var html = $@"
<h1>Bem-vindo, {System.Net.WebUtility.HtmlEncode(nome)}!</h1>
<p>Sua conta foi criada com sucesso no <strong>SoldOut Tickets</strong>.</p>
<p>Você já pode descobrir eventos e comprar ingressos.</p>
<p>Acesse: <a href=""https://soldouttickets.com"">soldouttickets.com</a></p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Bem-vindo, {nome}! Sua conta foi criada com sucesso. Acesse soldouttickets.com";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage BoasVindasVendedor(string destinatario, string nomeFantasia, string plano)
    {
        var assunto = "Bem-vindo ao SoldOut Tickets — Comece a vender!";
        var html = $@"
<h1>Bem-vindo, {System.Net.WebUtility.HtmlEncode(nomeFantasia)}!</h1>
<p>Sua conta de <strong>Vendedor</strong> foi criada com sucesso no <strong>SoldOut Tickets</strong>.</p>
<p>Plano atual: <strong>{System.Net.WebUtility.HtmlEncode(plano)}</strong></p>
<p>Acesse seu Painel do Vendedor e crie seu primeiro evento em minutos!</p>
<p><a href=""https://soldouttickets.com/painel"">Acessar Painel</a></p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Bem-vindo, {nomeFantasia}! Sua conta de Vendedor foi criada. Plano: {plano}. Acesse soldouttickets.com/painel";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage ReservaConfirmada(string destinatario, string nomeEvento,
        DateTime dataEvento, int quantidadeItens, decimal valorTotal)
    {
        var assunto = $"Reserva confirmada — {nomeEvento}";
        var html = $@"
<h1>Reserva confirmada!</h1>
<p><strong>Evento:</strong> {System.Net.WebUtility.HtmlEncode(nomeEvento)}</p>
<p><strong>Data:</strong> {dataEvento:dd/MM/yyyy HH:mm}</p>
<p><strong>Participantes:</strong> {quantidadeItens}</p>
<p><strong>Valor total:</strong> R$ {valorTotal:N2}</p>
<p>Se for um evento pago, acesse sua conta para confirmar o pagamento.</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Reserva confirmada! Evento: {nomeEvento}, Data: {dataEvento:dd/MM/yyyy}, Valor total: R$ {valorTotal:N2}";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage PagamentoConfirmado(string destinatario, string nomeEvento,
        decimal valorPago, string metodo, DateTime dataPagamento)
    {
        var assunto = $"Pagamento confirmado — {nomeEvento}";
        var html = $@"
<h1>Pagamento confirmado!</h1>
<p><strong>Evento:</strong> {System.Net.WebUtility.HtmlEncode(nomeEvento)}</p>
<p><strong>Valor pago:</strong> R$ {valorPago:N2}</p>
<p><strong>Método:</strong> {System.Net.WebUtility.HtmlEncode(metodo)}</p>
<p><strong>Data do pagamento:</strong> {dataPagamento:dd/MM/yyyy HH:mm}</p>
<p>Seus ingressos estão garantidos. Aproveite o evento!</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Pagamento confirmado! Evento: {nomeEvento}, Valor: R$ {valorPago:N2}";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage ReembolsoConfirmado(string destinatario, string nomeEvento,
        decimal valorReembolsado)
    {
        var assunto = $"Reembolso processado — {nomeEvento}";
        var html = $@"
<h1>Reembolso processado</h1>
<p><strong>Evento:</strong> {System.Net.WebUtility.HtmlEncode(nomeEvento)}</p>
<p><strong>Valor reembolsado:</strong> R$ {valorReembolsado:N2}</p>
<p>O valor será devolvido conforme a política de reembolso da plataforma.</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Reembolso processado. Evento: {nomeEvento}, Valor: R$ {valorReembolsado:N2}";
        return new EmailMessage(destinatario, assunto, html, texto);
    }

    public static EmailMessage RedefinicaoSenha(string destinatario, string nome, string link)
    {
        var assunto = "Redefinição de senha — SoldOut Tickets";
        var html = $@"
<h1>Redefinição de senha</h1>
<p>Olá, {System.Net.WebUtility.HtmlEncode(nome)}!</p>
<p>Recebemos uma solicitação para redefinir sua senha no <strong>SoldOut Tickets</strong>.</p>
<p>Clique no link abaixo para criar uma nova senha:</p>
<p><a href=""{link}"">{System.Net.WebUtility.HtmlEncode(link)}</a></p>
<p><strong>Este link é válido por 15 minutos.</strong></p>
<p>Se você não solicitou esta redefinição, ignore este e-mail.</p>
<p>— Equipe SoldOut Tickets</p>";
        var texto = $"Olá, {nome}! Acesse o link para redefinir sua senha: {link}. Válido por 15 minutos.";
        return new EmailMessage(destinatario, assunto, html, texto);
    }
}
