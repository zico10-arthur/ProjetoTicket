using Domain.Entities;
using Domain.Exceptions;
using Domain.Interface;
using Domain.DTOs;
using Application.Service;
using Moq;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// Spec 50: Testes de Cancelamento de Evento com Reembolso Obrigatório.
/// T1-T5: ObterStatusCancelamento | T6-T12: CancelarEvento
/// </summary>
public class EventoCancelamentoTests
{
    private const string CpfVendedor = "52987654321";
    private const string CpfOutroVendedor = "11122233344";
    private static readonly Guid EventoId = Guid.NewGuid();

    private static Evento CriarEvento(decimal precoPadrao, bool cancelado = false)
    {
        var evento = new Evento("Evento Teste", 100, DateTime.UtcNow.AddDays(10),
            precoPadrao, CpfVendedor, TipoEvento.Palestra);
        if (cancelado) evento.Cancelar();
        return evento;
    }

    private static StatusCancelamentoDTO CriarStatus(bool gratuito, int vendidos, decimal valor)
    {
        return new StatusCancelamentoDTO(EventoId, "Evento Teste",
            gratuito, vendidos, vendidos,
            !gratuito && vendidos > 0, valor, "");
    }

    // ==================== T1-T5: ObterStatusCancelamento ====================

    [Fact]
    public async Task T1_ObterStatusCancelamento_EventoPagoComVendas_RetornaReembolsoNecessario()
    {
        var evento = CriarEvento(49.90m);
        var status = CriarStatus(false, 23, 1147.70m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);
        var result = await service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        Assert.True(result.ReembolsoNecessario);
        Assert.Equal(1147.70m, result.ValorTotalReembolso);
        Assert.Equal(23, result.TotalIngressosVendidos);
        Assert.Contains("23 ingressos vendidos", result.Mensagem);
        Assert.Contains("1147", result.Mensagem);
    }

    [Fact]
    public async Task T2_ObterStatusCancelamento_EventoGratuito_RetornaReembolsoNaoNecessario()
    {
        var evento = CriarEvento(0m);
        var status = CriarStatus(true, 5, 0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);
        var result = await service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        Assert.True(result.Gratuito);
        Assert.False(result.ReembolsoNecessario);
        Assert.Equal(0m, result.ValorTotalReembolso);
        Assert.Contains("não exige reembolso", result.Mensagem);
    }

    [Fact]
    public async Task T3_ObterStatusCancelamento_VendedorNaoDono_LancaUnauthorized()
    {
        var evento = CriarEvento(50m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);

        var service = new EventoService(repo.Object, null!);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ObterStatusCancelamento(EventoId, CpfOutroVendedor, false, CancellationToken.None));
    }

    [Fact]
    public async Task T4_ObterStatusCancelamento_AdminAcessaEventoAlheio_Sucesso()
    {
        var evento = CriarEvento(50m);
        var status = CriarStatus(false, 0, 0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);
        var result = await service.ObterStatusCancelamento(EventoId, CpfOutroVendedor, true, CancellationToken.None);

        Assert.NotNull(result);
        Assert.False(result.ReembolsoNecessario);
    }

    [Fact]
    public async Task T5_ObterStatusCancelamento_EventoInexistente_LancaKeyNotFound()
    {
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync((Evento?)null);

        var service = new EventoService(repo.Object, null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None));
    }

    // ==================== T6-T12: CancelarEvento ====================

    [Fact]
    public async Task T6_CancelarEvento_EventoPagoComVendas_Sucesso()
    {
        var evento = CriarEvento(49.90m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task T7_CancelarEvento_EventoGratuito_Sucesso()
    {
        var evento = CriarEvento(0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        repo.Verify(r => r.CancelarComTransacao(EventoId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task T8_CancelarEvento_EventoSemVendas_Sucesso()
    {
        var evento = CriarEvento(30m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task T9_CancelarEvento_JaCancelado_LancaDomainException()
    {
        var evento = CriarEvento(50m, cancelado: true);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);

        var service = new EventoService(repo.Object, null!);
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None));

        Assert.Equal("Evento já foi cancelado.", ex.Message);
    }

    [Fact]
    public async Task T10_CancelarEvento_VendedorNaoDono_LancaUnauthorized()
    {
        var evento = CriarEvento(50m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);

        var service = new EventoService(repo.Object, null!);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.CancelarEvento(EventoId, CpfOutroVendedor, false, CancellationToken.None));
    }

    [Fact]
    public async Task T11_CancelarEvento_AdminCancelaEventoAlheio_Sucesso()
    {
        var evento = CriarEvento(50m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);
        await service.CancelarEvento(EventoId, CpfOutroVendedor, true, CancellationToken.None);

        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task T12_CancelarEvento_EventoInexistente_LancaKeyNotFound()
    {
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync((Evento?)null);

        var service = new EventoService(repo.Object, null!);
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None));
    }

    // ==================== T13: CancelarComTransacao com status gratuito ====================

    [Fact]
    public async Task T13_CancelarEvento_EventoPago_ChamaTransacaoComGratuitoFalse()
    {
        var evento = CriarEvento(49.90m); // PrecoPadrao > 0 → Gratuito = false
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Verifica que gratuito = false foi passado (evento.Gratuito == false)
        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task T14_CancelarEvento_EventoGratuito_ChamaTransacaoComGratuitoTrue()
    {
        var evento = CriarEvento(0m); // PrecoPadrao = 0 → Gratuito = true
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Verifica que gratuito = true foi passado
        repo.Verify(r => r.CancelarComTransacao(EventoId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==================== Teste adicional: mensagem do status ====================

    [Fact]
    public async Task StatusCancelamento_SemVendas_MensagemNaoExigeReembolso()
    {
        var evento = CriarEvento(50m);
        var status = CriarStatus(false, 0, 0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);
        var result = await service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        Assert.Equal("Nenhum ingresso vendido. O cancelamento não exige reembolso.", result.Mensagem);
    }
}
