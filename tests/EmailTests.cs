using Application.Email;
using Domain.ValueObjects;
using Infraestructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// Spec 181: Testes para correção dos bugs de envio de e-mail.
/// </summary>
public class EmailTests
{
    // ──────────────────────────────────────────────
    // Grupo A: BUG-001 — Links HTML quebrados
    // ──────────────────────────────────────────────

    [Fact]
    public void BoasVindasComprador_HtmlContemHrefCorreto()
    {
        var msg = EmailTemplates.BoasVindasComprador("a@b.com", "João");

        Assert.Contains("href=\"https://soldouttickets.com\"", msg.CorpoHtml);
        Assert.DoesNotContain("\\\"", msg.CorpoHtml);
    }

    [Fact]
    public void BoasVindasVendedor_HtmlContemHrefCorreto()
    {
        var msg = EmailTemplates.BoasVindasVendedor("a@b.com", "Loja X", "Gratuito");

        Assert.Contains("href=\"https://soldouttickets.com/painel\"", msg.CorpoHtml);
        Assert.DoesNotContain("\\\"", msg.CorpoHtml);
    }

    [Fact]
    public void RedefinicaoSenha_HtmlContemHrefCorreto()
    {
        var msg = EmailTemplates.RedefinicaoSenha("a@b.com", "João", "https://localhost/redefinir?token=abc");

        Assert.Contains("href=\"https://localhost/redefinir?token=abc\"", msg.CorpoHtml);
        Assert.DoesNotContain("\\\"", msg.CorpoHtml);
    }

    // ──────────────────────────────────────────────
    // Grupo B: BUG-002 — Log enganoso
    // ──────────────────────────────────────────────

    [Fact]
    public async Task EnviarAsync_RetornaFalse_QuandoNaoConfigurado()
    {
        var settings = Options.Create(new SmtpSettings()); // defaults: Host=""
        var logger = Mock.Of<ILogger<SmtpEmailSender>>();
        var sender = new SmtpEmailSender(settings, logger);
        var email = new EmailMessage("a@b.com", "Test", "<p>Oi</p>", "Oi");

        var result = await sender.EnviarAsync(email, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task EnviarAsync_TentaConectar_QuandoConfigurado()
    {
        // Com SMTP configurado (Host/FromAddress preenchidos), o método
        // deve tentar conectar ao servidor em vez de retornar false.
        // Como localhost:25 não tem servidor, esperamos uma exceção de rede.
        var settings = Options.Create(new SmtpSettings
        {
            Host = "localhost",
            Port = 25,
            FromAddress = "test@test.com"
        });
        var logger = Mock.Of<ILogger<SmtpEmailSender>>();
        var sender = new SmtpEmailSender(settings, logger);
        var email = new EmailMessage("a@b.com", "Test", "<p>Oi</p>", "Oi");

        // Não deve retornar false (Configurado == true); vai tentar conectar
        // e lançar exceção de rede. Capturamos qualquer exceção para validar
        // que NÃO retornou false.
        var result = false;
        try
        {
            await sender.EnviarAsync(email, CancellationToken.None);
        }
        catch
        {
            // Exceção de conexão = Configurado == true (tentou conectar)
            result = true;
        }

        Assert.True(result, "Deveria ter tentado conectar e lançado exceção de rede.");
    }

    // ──────────────────────────────────────────────
    // Grupo C: BUG-003 — Clean Architecture
    // ──────────────────────────────────────────────

    [Fact]
    public void EmailTemplates_Namespace_Correto()
    {
        Assert.Equal("Application.Email", typeof(EmailTemplates).Namespace);
    }
}
