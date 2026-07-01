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
    public async Task ObterStatusCancelamento_EventoPagoComVendas_DeveRetornarReembolsoNecessario()
    {
        // Arrange
        var evento = CriarEvento(49.90m);
        var status = CriarStatus(false, 23, 1147.70m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);

        // Act
        var result = await service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        Assert.True(result.ReembolsoNecessario);
        Assert.Equal(1147.70m, result.ValorTotalReembolso);
        Assert.Equal(23, result.TotalIngressosVendidos);
        Assert.Contains("23 ingressos vendidos", result.Mensagem);
        Assert.Contains("1147", result.Mensagem);
    }

    [Fact]
    public async Task ObterStatusCancelamento_EventoGratuito_DeveRetornarReembolsoNaoNecessario()
    {
        // Arrange
        var evento = CriarEvento(0m);
        var status = CriarStatus(true, 5, 0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);

        // Act
        var result = await service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        Assert.True(result.Gratuito);
        Assert.False(result.ReembolsoNecessario);
        Assert.Equal(0m, result.ValorTotalReembolso);
        Assert.Contains("não exige reembolso", result.Mensagem);
    }

    [Fact]
    public async Task ObterStatusCancelamento_VendedorNaoDono_DeveLancarUnauthorizedAccessException()
    {
        // Arrange
        var evento = CriarEvento(50m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);

        var service = new EventoService(repo.Object, null!);

        // Act
        Func<Task> act = () => service.ObterStatusCancelamento(EventoId, CpfOutroVendedor, false, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task ObterStatusCancelamento_AdminAcessaEventoAlheio_DeveRetornarStatus()
    {
        // Arrange
        var evento = CriarEvento(50m);
        var status = CriarStatus(false, 0, 0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);

        // Act
        var result = await service.ObterStatusCancelamento(EventoId, CpfOutroVendedor, true, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ReembolsoNecessario);
    }

    [Fact]
    public async Task ObterStatusCancelamento_EventoInexistente_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync((Evento?)null);

        var service = new EventoService(repo.Object, null!);

        // Act
        Func<Task> act = () => service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }

    // ==================== T6-T12: CancelarEvento ====================

    [Fact]
    public async Task CancelarEvento_EventoPagoComVendas_DeveCancelarComSucesso()
    {
        // Arrange
        var evento = CriarEvento(49.90m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);

        // Act
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarEvento_EventoGratuito_DeveCancelarComSucesso()
    {
        // Arrange
        var evento = CriarEvento(0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);

        // Act
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CancelarComTransacao(EventoId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarEvento_EventoSemVendas_DeveCancelarComSucesso()
    {
        // Arrange
        var evento = CriarEvento(30m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);

        // Act
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarEvento_JaCancelado_DeveLancarDomainException()
    {
        // Arrange
        var evento = CriarEvento(50m, cancelado: true);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);

        var service = new EventoService(repo.Object, null!);

        // Act
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None));

        // Assert
        Assert.Equal("Evento já foi cancelado.", ex.Message);
    }

    [Fact]
    public async Task CancelarEvento_VendedorNaoDono_DeveLancarUnauthorizedAccessException()
    {
        // Arrange
        var evento = CriarEvento(50m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);

        var service = new EventoService(repo.Object, null!);

        // Act
        Func<Task> act = () => service.CancelarEvento(EventoId, CpfOutroVendedor, false, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(act);
    }

    [Fact]
    public async Task CancelarEvento_AdminCancelaEventoAlheio_DeveCancelarComSucesso()
    {
        // Arrange
        var evento = CriarEvento(50m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);

        // Act
        await service.CancelarEvento(EventoId, CpfOutroVendedor, true, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarEvento_EventoInexistente_DeveLancarKeyNotFoundException()
    {
        // Arrange
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync((Evento?)null);

        var service = new EventoService(repo.Object, null!);

        // Act
        Func<Task> act = () => service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(act);
    }

    // ==================== T13: CancelarComTransacao com status gratuito ====================

    [Fact]
    public async Task CancelarEvento_EventoPago_DeveChamarTransacaoComGratuitoFalse()
    {
        // Arrange
        var evento = CriarEvento(49.90m); // PrecoPadrao > 0 → Gratuito = false
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);

        // Act
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CancelarComTransacao(EventoId, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarEvento_EventoGratuito_DeveChamarTransacaoComGratuitoTrue()
    {
        // Arrange
        var evento = CriarEvento(0m); // PrecoPadrao = 0 → Gratuito = true
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.CancelarComTransacao(EventoId, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new EventoService(repo.Object, null!);

        // Act
        await service.CancelarEvento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        repo.Verify(r => r.CancelarComTransacao(EventoId, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ==================== Teste adicional: mensagem do status ====================

    [Fact]
    public async Task ObterStatusCancelamento_SemVendas_DeveRetornarMensagemNaoExigeReembolso()
    {
        // Arrange
        var evento = CriarEvento(50m);
        var status = CriarStatus(false, 0, 0m);
        var repo = new Mock<IEventoRepository>();
        repo.Setup(r => r.GetByIdAsync(EventoId)).ReturnsAsync(evento);
        repo.Setup(r => r.ObterStatusCancelamento(EventoId, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var service = new EventoService(repo.Object, null!);

        // Act
        var result = await service.ObterStatusCancelamento(EventoId, CpfVendedor, false, CancellationToken.None);

        // Assert
        Assert.Equal("Nenhum ingresso vendido. O cancelamento não exige reembolso.", result.Mensagem);
    }
}
