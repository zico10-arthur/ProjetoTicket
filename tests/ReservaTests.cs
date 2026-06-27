using Domain.Entities;
using Domain.Exceptions;
using Domain.Interface;
using Domain.ValueObjects;
using Application.Service;
using Application.Interfaces;
using Domain.DTOs;
using Moq;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// ST-04: Testes de ItemReserva e Reserva multi-participante.
/// </summary>
public class ReservaTests
{
    private static readonly Guid PerfilComprador = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");
    private const string CpfValido = "52998224725";
    private const string CpfValido2 = "12345678909";
    private const string CpfValido3 = "98765432100";
    private const string CpfValido4 = "11144477735";
    private static readonly Guid EventoId = Guid.NewGuid();

    private ItemReserva CriarItem(string cpf, Guid ingressoId, decimal preco = 100.00m)
    {
        return new ItemReserva(cpf, ingressoId, preco);
    }

    // ==================== ItemReserva ====================

    [Fact]
    public void ItemReserva_Criar_DeveInicializarCorretamente()
    {
        var ingressoId = Guid.NewGuid();
        var item = new ItemReserva("52998224725", ingressoId, 50.00m);

        Assert.Equal("52998224725", item.CpfParticipante);
        Assert.Equal(ingressoId, item.IngressoId);
        Assert.Equal(50.00m, item.PrecoUnitario);
        Assert.False(item.Reembolsado);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void ItemReserva_MarcarReembolsado_DeveAlterarFlag()
    {
        var item = new ItemReserva("52998224725", Guid.NewGuid(), 50.00m);
        Assert.False(item.Reembolsado);

        item.MarcarReembolsado();

        Assert.True(item.Reembolsado);
    }

    [Fact]
    public void ItemReserva_VincularReserva_DeveAssociarReservaId()
    {
        var item = new ItemReserva("52998224725", Guid.NewGuid(), 50.00m);
        var reservaId = Guid.NewGuid();

        item.VincularReserva(reservaId);

        Assert.Equal(reservaId, item.ReservaId);
    }

    // ==================== Reserva: Sem cupom ====================

    [Fact]
    public void Criar_SemCupom_DeveUsarPrecoCheio()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };

        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        Assert.NotNull(reserva);
        Assert.Equal(100.00m, reserva.ValorFinalPago);
        Assert.Null(reserva.CupomUtilizado);
        Assert.Single(reserva.Itens);
    }

    // ==================== Reserva: Com cupom ====================

    [Fact]
    public void Criar_ComCupomValido_DeveAplicarDesconto()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };
        var cupom = Cupom.Criar("DESC10", 10, 50.00m, DateTime.Now.AddDays(30));

        var reserva = Reserva.Criar(CpfValido, EventoId, itens, cupom);

        Assert.Equal(90.00m, reserva.ValorFinalPago);
        Assert.Equal("DESC10", reserva.CupomUtilizado);
    }

    [Fact]
    public void Criar_ComCupomAbaixoDoValorMinimo_DeveLancarExcecao()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };
        var cupom = Cupom.Criar("ALTO50", 10, 200.00m, DateTime.Now.AddDays(30));

        Assert.Throws<ValorMinimoCupomExcedido>(() =>
            Reserva.Criar(CpfValido, EventoId, itens, cupom));
    }

    [Fact]
    public void Criar_ComCupomInativo_DeveLancarExcecao()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };
        var cupom = Cupom.Criar("INATI50", 10, 50.00m, DateTime.Now.AddDays(30));
        cupom.AlternarStatus();

        Assert.Throws<CupomInvalidoParaUso>(() =>
            Reserva.Criar(CpfValido, EventoId, itens, cupom));
    }

    // ==================== Validação de CPF ====================

    [Fact]
    public void Criar_ComCpfInvalido_DeveLancarExcecao()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem("12345678900", Guid.NewGuid(), 100.00m)  // CPF inválido
        };

        Assert.Throws<CpfInvalido>(() =>
            Reserva.Criar(CpfValido, EventoId, itens));
    }

    [Fact]
    public void Criar_ComCpfVazio_DeveLancarExcecao()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem("", Guid.NewGuid(), 100.00m)
        };

        Assert.Throws<CpfVazio>(() =>
            Reserva.Criar(CpfValido, EventoId, itens));
    }

    // ==================== Validação de itens ====================

    [Fact]
    public void Criar_ComZeroItens_DeveLancarExcecao()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Reserva.Criar(CpfValido, EventoId, new List<ItemReserva>()));
        Assert.Contains("pelo menos 1", ex.Message);
    }

    [Fact]
    public void Criar_ComMaisDe4Itens_DeveLancarExcecao()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 50m),
            CriarItem(CpfValido2, Guid.NewGuid(), 50m),
            CriarItem(CpfValido3, Guid.NewGuid(), 50m),
            CriarItem("11122233344", Guid.NewGuid(), 50m),
            CriarItem("55566677788", Guid.NewGuid(), 50m),  // 5º item
        };

        var ex = Assert.Throws<DomainException>(() =>
            Reserva.Criar(CpfValido, EventoId, itens));
        Assert.Contains("4", ex.Message);
    }

    // ==================== Cupom sobre valor total ====================

    [Fact]
    public void Criar_ComMultiplosItensECupom_DeveAplicarSobreTotal()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100m),
            CriarItem(CpfValido2, Guid.NewGuid(), 100m),
            CriarItem(CpfValido3, Guid.NewGuid(), 50m),
        };
        // Total = 250, Cupom 10% = 25 de desconto
        var cupom = Cupom.Criar("DESC10", 10, 100.00m, DateTime.Now.AddDays(30));

        var reserva = Reserva.Criar(CpfValido, EventoId, itens, cupom);

        Assert.Equal(225.00m, reserva.ValorFinalPago);
        Assert.Equal(3, reserva.Itens.Count);
    }

    // ==================== CPF duplicado na mesma reserva ====================

    [Fact]
    public void Criar_ComCpfDuplicado_DeveLancarExcecao()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 50m),
            CriarItem(CpfValido, Guid.NewGuid(), 50m),  // mesmo CPF
        };

        var ex = Assert.Throws<DomainException>(() =>
            Reserva.Criar(CpfValido, EventoId, itens));
        Assert.Contains("mais de uma vez", ex.Message);
    }

    // ==================== PodeAdicionarMaisItens ====================

    [Fact]
    public void PodeAdicionarMaisItens_ComMenosDe4_DeveRetornarTrue()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 50m)
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        Assert.True(reserva.PodeAdicionarMaisItens);
    }

    [Fact]
    public void PodeAdicionarMaisItens_Com4_DeveRetornarFalse()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 10m),
            CriarItem(CpfValido2, Guid.NewGuid(), 10m),
            CriarItem(CpfValido3, Guid.NewGuid(), 10m),
            CriarItem(CpfValido4, Guid.NewGuid(), 10m),
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        Assert.False(reserva.PodeAdicionarMaisItens);
    }

    // ==================== Constante LimiteMaximoItens ====================

    [Fact]
    public void Criar_Com4Itens_DeveCriarNormalmente()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 25m),
            CriarItem(CpfValido2, Guid.NewGuid(), 25m),
            CriarItem(CpfValido3, Guid.NewGuid(), 25m),
            CriarItem(CpfValido4, Guid.NewGuid(), 25m),
        };

        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        Assert.Equal(4, reserva.Itens.Count);
        Assert.Equal(100m, reserva.ValorFinalPago);
    }

    // ==================== Spec 40: Reembolso ====================

    [Fact]
    public void Reserva_Reembolsada_InicializaFalse()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100m)
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        Assert.False(reserva.Reembolsada);
    }

    [Fact]
    public void Reserva_MarcarReembolsada_DeveAlterarFlag()
    {
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100m)
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);
        Assert.False(reserva.Reembolsada);

        reserva.MarcarReembolsada();

        Assert.True(reserva.Reembolsada);
    }

    // ==================== Spec 40: CancelarReserva (Service) ====================

    private Mock<IReservaRepository> _reservaRepoMock = null!;
    private Mock<IUsuarioRepository> _usuarioRepoMock = null!;
    private Mock<IEventoRepository> _eventoRepoMock = null!;
    private Mock<Domain.Interface.IEmailSender> _emailSenderMock = null!;
    private ReservaService _service = null!;

    private void SetupService()
    {
        _reservaRepoMock = new Mock<IReservaRepository>();
        _usuarioRepoMock = new Mock<IUsuarioRepository>();
        _eventoRepoMock = new Mock<IEventoRepository>();
        _emailSenderMock = new Mock<Domain.Interface.IEmailSender>();

        _service = new ReservaService(
            _reservaRepoMock.Object,
            _usuarioRepoMock.Object,
            _eventoRepoMock.Object,
            Mock.Of<ICupomRepository>(),
            Mock.Of<IIngressoRepository>(),
            _emailSenderMock.Object);
    }

    [Fact]
    public async Task CancelarReserva_Sucesso_ReservaPaga()
    {
        SetupService();
        var reservaId = Guid.NewGuid();
        var eventoId = Guid.NewGuid();
        var cpf = "52998224725";

        var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpf, eventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

        var evento = new Evento("Workshop .NET", 50,
            DateTime.UtcNow.AddDays(7), 100m, "12345678000199");

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
            .ReturnsAsync(evento);
        _usuarioRepoMock.Setup(r => r.BuscarCpf(cpf, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Usuario?)null);

        await _service.CancelarReserva(reservaId, cpf, CancellationToken.None);

        _reservaRepoMock.Verify(r => r.CancelarComTransacao(
            reservaId, It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(e => e.EnfileirarAsync(
            It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarReserva_NaoEncontrada_LancaDomainException()
    {
        SetupService();
        var reservaId = Guid.NewGuid();
        var cpf = "52998224725";

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reserva?)null);

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        Assert.Equal("Reserva não encontrada.", ex.Message);
        _reservaRepoMock.Verify(r => r.CancelarComTransacao(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelarReserva_OutroUsuario_LancaUnauthorizedAccessException()
    {
        SetupService();
        var reservaId = Guid.NewGuid();
        var cpfDono = "52998224725";
        var cpfOutro = "98765432100";

        var itens = new List<ItemReserva> { CriarItem(cpfDono, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpfDono, Guid.NewGuid(), itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CancelarReserva(reservaId, cpfOutro, CancellationToken.None));

        Assert.Equal("Esta reserva não pertence a você.", ex.Message);
    }

    [Fact]
    public async Task CancelarReserva_JaReembolsada_LancaDomainException()
    {
        SetupService();
        var reservaId = Guid.NewGuid();
        var cpf = "52998224725";

        var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpf, Guid.NewGuid(), itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);
        reserva.MarcarReembolsada();

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        Assert.Equal("Reserva já foi cancelada.", ex.Message);
    }

    [Fact]
    public async Task CancelarReserva_EventoJaComecou_LancaDomainException()
    {
        SetupService();
        var reservaId = Guid.NewGuid();
        var eventoId = Guid.NewGuid();
        var cpf = "52998224725";

        var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpf, eventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

        var evento = new Evento("Workshop .NET", 50,
            DateTime.UtcNow.AddDays(-1), 100m, "12345678000199");

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
            .ReturnsAsync(evento);

        var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        Assert.Equal("Não é possível cancelar. O evento já começou.", ex.Message);
    }

    [Fact]
    public async Task CancelarReserva_TransacaoFalha_NaoEnfileiraEmail()
    {
        SetupService();
        var reservaId = Guid.NewGuid();
        var eventoId = Guid.NewGuid();
        var cpf = "52998224725";

        var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpf, eventoId, itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

        var evento = new Evento("Workshop .NET", 50,
            DateTime.UtcNow.AddDays(7), 100m, "12345678000199");

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);
        _eventoRepoMock.Setup(r => r.GetByIdAsync(eventoId))
            .ReturnsAsync(evento);
        _reservaRepoMock.Setup(r => r.CancelarComTransacao(reservaId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SQL deadlock"));

        await Assert.ThrowsAsync<Exception>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        _emailSenderMock.Verify(e => e.EnfileirarAsync(
            It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
