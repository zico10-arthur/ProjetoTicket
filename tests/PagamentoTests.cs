using Application.DTOs;
using Application.Service;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Interface;
using Moq;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// Spec 170: Testes do fluxo de pagamento simulado (checkout interno).
/// </summary>
public class PagamentoTests
{
    private static readonly Guid ReservaId = Guid.NewGuid();
    private static readonly Guid EventoId = Guid.NewGuid();
    private const string CpfComprador = "52998224725";

    // ==================== Pagamento Entity ====================

    [Fact]
    public void Pagamento_Criar_DeveInicializarComoConfirmado()
    {
        // Arrange
        var valor = 180.00m;
        var metodo = "Simulado";

        // Act
        var pagamento = new Pagamento(ReservaId, valor, metodo);

        // Assert
        Assert.NotEqual(Guid.Empty, pagamento.Id);
        Assert.Equal(ReservaId, pagamento.ReservaId);
        Assert.Equal(180.00m, pagamento.ValorPago);
        Assert.Equal(StatusPagamento.Confirmado, pagamento.Status);
        Assert.Equal("Simulado", pagamento.Metodo);
        Assert.True(pagamento.DataPagamento <= DateTime.UtcNow);
        Assert.True(pagamento.DataCriacao <= DateTime.UtcNow);
    }

    [Fact]
    public void Pagamento_Criar_EventoGratuito_DeveTerValorZero()
    {
        // Arrange
        var valor = 0m;

        // Act
        var pagamento = new Pagamento(ReservaId, valor, "Simulado");

        // Assert
        Assert.Equal(0m, pagamento.ValorPago);
        Assert.Equal(StatusPagamento.Confirmado, pagamento.Status);
    }

    [Fact]
    public void Pagamento_MarcarReembolsado_DeveAlterarStatus()
    {
        // Arrange
        var pagamento = new Pagamento(ReservaId, 100m, "Simulado");

        // Act
        pagamento.MarcarReembolsado();

        // Assert
        Assert.Equal(StatusPagamento.Reembolsado, pagamento.Status);
    }

    // ==================== Reserva Entity ====================

    [Fact]
    public void Reserva_Criar_DeveInicializarPagoComoFalse()
    {
        // Arrange
        var ingressoId = Guid.NewGuid();
        var itens = new List<ItemReserva>
        {
            new ItemReserva(CpfComprador, ingressoId, 100m)
        };

        // Act
        var reserva = Reserva.Criar(CpfComprador, EventoId, itens);

        // Assert
        Assert.False(reserva.Pago);
    }

    [Fact]
    public void Reserva_MarcarPago_DeveAlterarFlag()
    {
        // Arrange
        var ingressoId = Guid.NewGuid();
        var itens = new List<ItemReserva>
        {
            new ItemReserva(CpfComprador, ingressoId, 100m)
        };
        var reserva = Reserva.Criar(CpfComprador, EventoId, itens);

        // Act
        reserva.MarcarPago();

        // Assert
        Assert.True(reserva.Pago);
    }

    // ==================== StatusPagamento Enum ====================

    [Fact]
    public void StatusPagamento_Valores_DeveTerMapeamentoCorreto()
    {
        // Arrange
        // (enum já definido)

        // Act
        var pendente = (int)StatusPagamento.Pendente;
        var confirmado = (int)StatusPagamento.Confirmado;
        var reembolsado = (int)StatusPagamento.Reembolsado;
        var falhou = (int)StatusPagamento.Falhou;

        // Assert
        Assert.Equal(0, pendente);
        Assert.Equal(1, confirmado);
        Assert.Equal(2, reembolsado);
        Assert.Equal(3, falhou);
    }

    // ==================== PagamentoService ====================

    [Fact]
    public async Task ConfirmarCheckout_ReservaValida_DeveRetornarSucesso()
    {
        // Arrange
        var ingressoId = Guid.NewGuid();
        var itens = new List<ItemReserva>
        {
            new ItemReserva(CpfComprador, ingressoId, 100m)
        };
        var reserva = Reserva.Criar(CpfComprador, EventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, ReservaId);

        var evento = new Evento("Workshop .NET", 50, DateTime.UtcNow.AddDays(30), 100m, "12345678901234");

        var pagamentoRepoMock = new Mock<IPagamentoRepository>();
        var reservaRepoMock = new Mock<IReservaRepository>();
        var eventoRepoMock = new Mock<IEventoRepository>();

        reservaRepoMock.Setup(r => r.BuscarPorId(ReservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        eventoRepoMock.Setup(r => r.GetByIdAsync(EventoId))
            .ReturnsAsync(evento);

        var service = new PagamentoService(
            pagamentoRepoMock.Object, reservaRepoMock.Object, eventoRepoMock.Object);

        // Act
        var result = await service.ConfirmarCheckout(ReservaId, CpfComprador, "Simulado", CancellationToken.None);

        // Assert
        Assert.Equal(ReservaId, result.ReservaId);
        Assert.Equal("Confirmado", result.Status);
        Assert.Equal(100m, result.ValorPago);
        Assert.Contains("sucesso", result.Message);

        pagamentoRepoMock.Verify(r => r.CriarComTransacao(
            It.IsAny<Pagamento>(), It.IsAny<Reserva>(), It.IsAny<Evento>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmarCheckout_JaPago_LancaExcecao()
    {
        // Arrange
        var ingressoId = Guid.NewGuid();
        var itens = new List<ItemReserva>
        {
            new ItemReserva(CpfComprador, ingressoId, 100m)
        };
        var reserva = Reserva.Criar(CpfComprador, EventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, ReservaId);
        reserva.MarcarPago();

        var pagamentoRepoMock = new Mock<IPagamentoRepository>();
        var reservaRepoMock = new Mock<IReservaRepository>();
        var eventoRepoMock = new Mock<IEventoRepository>();

        reservaRepoMock.Setup(r => r.BuscarPorId(ReservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        var service = new PagamentoService(
            pagamentoRepoMock.Object, reservaRepoMock.Object, eventoRepoMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.ConfirmarCheckout(ReservaId, CpfComprador, "Simulado", CancellationToken.None));

        // Assert
        Assert.Equal("Reserva já foi paga.", ex.Message);
    }

    [Fact]
    public async Task ConfirmarCheckout_OutroUsuario_LancaExcecao()
    {
        // Arrange
        var ingressoId = Guid.NewGuid();
        var itens = new List<ItemReserva>
        {
            new ItemReserva(CpfComprador, ingressoId, 100m)
        };
        var reserva = Reserva.Criar(CpfComprador, EventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, ReservaId);

        var pagamentoRepoMock = new Mock<IPagamentoRepository>();
        var reservaRepoMock = new Mock<IReservaRepository>();
        var eventoRepoMock = new Mock<IEventoRepository>();

        reservaRepoMock.Setup(r => r.BuscarPorId(ReservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        var service = new PagamentoService(
            pagamentoRepoMock.Object, reservaRepoMock.Object, eventoRepoMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => service.ConfirmarCheckout(ReservaId, "11144477735", "Simulado", CancellationToken.None));

        // Assert
        Assert.Contains("não pertence", ex.Message);
    }

    [Fact]
    public async Task ConfirmarCheckout_EventoJaComecou_LancaExcecao()
    {
        // Arrange
        var ingressoId = Guid.NewGuid();
        var itens = new List<ItemReserva>
        {
            new ItemReserva(CpfComprador, ingressoId, 100m)
        };
        var reserva = Reserva.Criar(CpfComprador, EventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, ReservaId);

        var evento = new Evento("Workshop .NET", 50, DateTime.UtcNow.AddDays(-1), 100m, "12345678901234");

        var pagamentoRepoMock = new Mock<IPagamentoRepository>();
        var reservaRepoMock = new Mock<IReservaRepository>();
        var eventoRepoMock = new Mock<IEventoRepository>();

        reservaRepoMock.Setup(r => r.BuscarPorId(ReservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        eventoRepoMock.Setup(r => r.GetByIdAsync(EventoId))
            .ReturnsAsync(evento);

        var service = new PagamentoService(
            pagamentoRepoMock.Object, reservaRepoMock.Object, eventoRepoMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.ConfirmarCheckout(ReservaId, CpfComprador, "Simulado", CancellationToken.None));

        // Assert
        Assert.Contains("evento já começou", ex.Message.ToLower());
    }

    [Fact]
    public async Task ConfirmarCheckout_ReservaNaoEncontrada_LancaExcecao()
    {
        // Arrange
        var pagamentoRepoMock = new Mock<IPagamentoRepository>();
        var reservaRepoMock = new Mock<IReservaRepository>();
        var eventoRepoMock = new Mock<IEventoRepository>();

        reservaRepoMock.Setup(r => r.BuscarPorId(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reserva?)null);

        var service = new PagamentoService(
            pagamentoRepoMock.Object, reservaRepoMock.Object, eventoRepoMock.Object);

        // Act
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.ConfirmarCheckout(Guid.NewGuid(), CpfComprador, "Simulado", CancellationToken.None));

        // Assert
        Assert.Equal("Reserva não encontrada.", ex.Message);
    }
}
