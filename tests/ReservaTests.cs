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
    private static readonly Guid UsuarioId = Guid.Parse("A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1");
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
        // Arrange
        var ingressoId = Guid.NewGuid();
        var cpf = "52998224725";
        var preco = 50.00m;

        // Act
        var item = new ItemReserva(cpf, ingressoId, preco);

        // Assert
        Assert.Equal("52998224725", item.CpfParticipante);
        Assert.Equal(ingressoId, item.IngressoId);
        Assert.Equal(50.00m, item.PrecoUnitario);
        Assert.False(item.Reembolsado);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    [Fact]
    public void ItemReserva_MarcarReembolsado_DeveAlterarFlag()
    {
        // Arrange
        var item = new ItemReserva("52998224725", Guid.NewGuid(), 50.00m);
        Assert.False(item.Reembolsado);

        // Act
        item.MarcarReembolsado();

        // Assert
        Assert.True(item.Reembolsado);
    }

    [Fact]
    public void ItemReserva_VincularReserva_DeveAssociarReservaId()
    {
        // Arrange
        var item = new ItemReserva("52998224725", Guid.NewGuid(), 50.00m);
        var reservaId = Guid.NewGuid();

        // Act
        item.VincularReserva(reservaId);

        // Assert
        Assert.Equal(reservaId, item.ReservaId);
    }

    // ==================== Reserva: Sem cupom ====================

    [Fact]
    public void Criar_SemCupom_DeveUsarPrecoCheio()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };

        // Act
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        // Assert
        Assert.NotNull(reserva);
        Assert.Equal(100.00m, reserva.ValorFinalPago);
        Assert.Null(reserva.CupomUtilizado);
        Assert.Single(reserva.Itens);
    }

    // ==================== Reserva: Com cupom ====================

    [Fact]
    public void Criar_ComCupomValido_DeveAplicarDesconto()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };
        var cupom = Cupom.Criar("DESC10", 10, 50.00m, DateTime.Now.AddDays(30));

        // Act
        var reserva = Reserva.Criar(CpfValido, EventoId, itens, cupom);

        // Assert
        Assert.Equal(90.00m, reserva.ValorFinalPago);
        Assert.Equal("DESC10", reserva.CupomUtilizado);
    }

    [Fact]
    public void Criar_ComCupomAbaixoDoValorMinimo_DeveLancarExcecao()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };
        var cupom = Cupom.Criar("ALTO50", 10, 200.00m, DateTime.Now.AddDays(30));

        // Act
        Action act = () => Reserva.Criar(CpfValido, EventoId, itens, cupom);

        // Assert
        Assert.Throws<ValorMinimoCupomExcedido>(act);
    }

    [Fact]
    public void Criar_ComCupomInativo_DeveLancarExcecao()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100.00m)
        };
        var cupom = Cupom.Criar("INATI50", 10, 50.00m, DateTime.Now.AddDays(30));
        cupom.AlternarStatus();

        // Act
        Action act = () => Reserva.Criar(CpfValido, EventoId, itens, cupom);

        // Assert
        Assert.Throws<CupomInvalidoParaUso>(act);
    }

    // ==================== Validação de CPF ====================

    [Fact]
    public void Criar_ComCpfInvalido_DeveLancarExcecao()
    {
        // Arrange
        var cpfInvalido = "12345678900";
        var itens = new List<ItemReserva>
        {
            CriarItem(cpfInvalido, Guid.NewGuid(), 100.00m)
        };

        // Act
        Action act = () => Reserva.Criar(CpfValido, EventoId, itens);

        // Assert
        Assert.Throws<CpfInvalido>(act);
    }

    [Fact]
    public void Criar_ComCpfVazio_DeveLancarExcecao()
    {
        // Arrange
        var cpfVazio = "";
        var itens = new List<ItemReserva>
        {
            CriarItem(cpfVazio, Guid.NewGuid(), 100.00m)
        };

        // Act
        Action act = () => Reserva.Criar(CpfValido, EventoId, itens);

        // Assert
        Assert.Throws<CpfVazio>(act);
    }

    // ==================== Validação de itens ====================

    [Fact]
    public void Criar_ComZeroItens_DeveLancarExcecao()
    {
        // Arrange
        var itensVazios = new List<ItemReserva>();

        // Act
        var ex = Assert.Throws<DomainException>(() =>
            Reserva.Criar(CpfValido, EventoId, itensVazios));

        // Assert
        Assert.Contains("pelo menos 1", ex.Message);
    }

    [Fact]
    public void Criar_ComMaisDe4Itens_DeveLancarExcecao()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 50m),
            CriarItem(CpfValido2, Guid.NewGuid(), 50m),
            CriarItem(CpfValido3, Guid.NewGuid(), 50m),
            CriarItem("11122233344", Guid.NewGuid(), 50m),
            CriarItem("55566677788", Guid.NewGuid(), 50m),  // 5º item
        };

        // Act
        var ex = Assert.Throws<DomainException>(() =>
            Reserva.Criar(CpfValido, EventoId, itens));

        // Assert
        Assert.Contains("4", ex.Message);
    }

    // ==================== Cupom sobre valor total ====================

    [Fact]
    public void Criar_ComMultiplosItensECupom_DeveAplicarSobreTotal()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100m),
            CriarItem(CpfValido2, Guid.NewGuid(), 100m),
            CriarItem(CpfValido3, Guid.NewGuid(), 50m),
        };
        // Total = 250, Cupom 10% = 25 de desconto
        var cupom = Cupom.Criar("DESC10", 10, 100.00m, DateTime.Now.AddDays(30));

        // Act
        var reserva = Reserva.Criar(CpfValido, EventoId, itens, cupom);

        // Assert
        Assert.Equal(225.00m, reserva.ValorFinalPago);
        Assert.Equal(3, reserva.Itens.Count);
    }

    // ==================== CPF duplicado na mesma reserva ====================

    [Fact]
    public void Criar_ComCpfDuplicado_DeveLancarExcecao()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 50m),
            CriarItem(CpfValido, Guid.NewGuid(), 50m),  // mesmo CPF
        };

        // Act
        var ex = Assert.Throws<DomainException>(() =>
            Reserva.Criar(CpfValido, EventoId, itens));

        // Assert
        Assert.Contains("mais de uma vez", ex.Message);
    }

    // ==================== PodeAdicionarMaisItens ====================

    [Fact]
    public void PodeAdicionarMaisItens_ComMenosDe4_DeveRetornarTrue()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 50m)
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        // Act
        var resultado = reserva.PodeAdicionarMaisItens;

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void PodeAdicionarMaisItens_Com4_DeveRetornarFalse()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 10m),
            CriarItem(CpfValido2, Guid.NewGuid(), 10m),
            CriarItem(CpfValido3, Guid.NewGuid(), 10m),
            CriarItem(CpfValido4, Guid.NewGuid(), 10m),
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        // Act
        var resultado = reserva.PodeAdicionarMaisItens;

        // Assert
        Assert.False(resultado);
    }

    // ==================== Constante LimiteMaximoItens ====================

    [Fact]
    public void Criar_Com4Itens_DeveCriarNormalmente()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 25m),
            CriarItem(CpfValido2, Guid.NewGuid(), 25m),
            CriarItem(CpfValido3, Guid.NewGuid(), 25m),
            CriarItem(CpfValido4, Guid.NewGuid(), 25m),
        };

        // Act
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        // Assert
        Assert.Equal(4, reserva.Itens.Count);
        Assert.Equal(100m, reserva.ValorFinalPago);
    }

    // ==================== Spec 40: Reembolso ====================

    [Fact]
    public void Reserva_Reembolsada_InicializaFalse()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100m)
        };

        // Act
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);

        // Assert
        Assert.False(reserva.Reembolsada);
    }

    [Fact]
    public void Reserva_MarcarReembolsada_DeveAlterarFlag()
    {
        // Arrange
        var itens = new List<ItemReserva>
        {
            CriarItem(CpfValido, Guid.NewGuid(), 100m)
        };
        var reserva = Reserva.Criar(CpfValido, EventoId, itens);
        Assert.False(reserva.Reembolsada);

        // Act
        reserva.MarcarReembolsada();

        // Assert
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
        // Arrange
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

        // Act
        await _service.CancelarReserva(reservaId, cpf, CancellationToken.None);

        // Assert
        _reservaRepoMock.Verify(r => r.CancelarComTransacao(
            reservaId, It.IsAny<CancellationToken>()), Times.Once);
        _emailSenderMock.Verify(e => e.EnfileirarAsync(
            It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CancelarReserva_NaoEncontrada_LancaDomainException()
    {
        // Arrange
        SetupService();
        var reservaId = Guid.NewGuid();
        var cpf = "52998224725";

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Reserva?)null);

        // Act
        var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        // Assert
        Assert.Equal("Reserva não encontrada.", ex.Message);
        _reservaRepoMock.Verify(r => r.CancelarComTransacao(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CancelarReserva_OutroUsuario_LancaUnauthorizedAccessException()
    {
        // Arrange
        SetupService();
        var reservaId = Guid.NewGuid();
        var cpfDono = "52998224725";
        var cpfOutro = "98765432100";

        var itens = new List<ItemReserva> { CriarItem(cpfDono, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpfDono, Guid.NewGuid(), itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        // Act
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CancelarReserva(reservaId, cpfOutro, CancellationToken.None));

        // Assert
        Assert.Equal("Esta reserva não pertence a você.", ex.Message);
    }

    [Fact]
    public async Task CancelarReserva_JaReembolsada_LancaDomainException()
    {
        // Arrange
        SetupService();
        var reservaId = Guid.NewGuid();
        var cpf = "52998224725";

        var itens = new List<ItemReserva> { CriarItem(cpf, Guid.NewGuid(), 100m) };
        var reserva = Reserva.Criar(cpf, Guid.NewGuid(), itens);
        typeof(Reserva).GetProperty("Id")!.SetValue(reserva, reservaId);
        reserva.MarcarReembolsada();

        _reservaRepoMock.Setup(r => r.BuscarPorId(reservaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reserva);

        // Act
        var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        // Assert
        Assert.Equal("Reserva já foi cancelada.", ex.Message);
    }

    [Fact]
    public async Task CancelarReserva_EventoJaComecou_LancaDomainException()
    {
        // Arrange
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

        // Act
        var ex = await Assert.ThrowsAsync<Domain.Exceptions.DomainException>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        // Assert
        Assert.Equal("Não é possível cancelar. O evento já começou.", ex.Message);
    }

    [Fact]
    public async Task CancelarReserva_TransacaoFalha_NaoEnfileiraEmail()
    {
        // Arrange
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

        // Act
        await Assert.ThrowsAsync<Exception>(() =>
            _service.CancelarReserva(reservaId, cpf, CancellationToken.None));

        // Assert
        _emailSenderMock.Verify(e => e.EnfileirarAsync(
            It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ==================== Spec 110: Visão Unificada de Cancelamento ====================

    [Fact]
    public async Task ListarMinhasReservas_Comprador_DeveVerApenasSuas()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        var dto = new ReservaDetalhadaDTO
        {
            Id = Guid.NewGuid(), NomeEvento = "Workshop",
            DataEvento = DateTime.UtcNow.AddDays(10), Reembolsada = false
        };
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dto });

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(dto.Id, result.First().Id);
    }

    [Fact]
    public async Task ListarMinhasReservas_Vendedor_DeveVerApenasSuas()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        var dto = new ReservaDetalhadaDTO
        {
            Id = Guid.NewGuid(), NomeEvento = "Peça Teatro",
            DataEvento = DateTime.UtcNow.AddDays(5), Reembolsada = false
        };
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dto });

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal("Peça Teatro", result.First().NomeEvento);
    }

    [Fact]
    public async Task ListarMinhasReservas_Admin_DeveVerApenasSuas()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReservaDetalhadaDTO>());

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListarMinhasReservas_ComItensReembolsados_DeveRetornarItensReembolsados()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        var dto = new ReservaDetalhadaDTO
        {
            Id = Guid.NewGuid(), NomeEvento = "Evento Cancelado",
            DataEvento = DateTime.UtcNow.AddDays(1), Reembolsada = true,
            Itens = new List<ItemReservaResponseDTO>
            {
                new() { Id = Guid.NewGuid(), CpfParticipante = "11111111111", PrecoUnitario = 50, Reembolsado = true },
                new() { Id = Guid.NewGuid(), CpfParticipante = "22222222222", PrecoUnitario = 50, Reembolsado = true }
            }
        };
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dto });

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);
        var reserva = result.First();

        // Assert
        Assert.True(reserva.Reembolsada);
        Assert.Equal(2, reserva.Itens.Count);
        Assert.All(reserva.Itens, item => Assert.True(item.Reembolsado));
    }

    [Fact]
    public async Task PodeCancelar_EventoFuturoNaoReembolsado_DeveRetornarTrue()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        var dto = new ReservaDetalhadaDTO
        {
            Id = Guid.NewGuid(), NomeEvento = "Evento Futuro",
            DataEvento = DateTime.UtcNow.AddDays(30), Reembolsada = false
        };
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dto });

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);

        // Assert
        Assert.True(result.First().PodeCancelar);
    }

    [Fact]
    public async Task PodeCancelar_JaReembolsada_DeveRetornarFalse()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        var dto = new ReservaDetalhadaDTO
        {
            Id = Guid.NewGuid(), NomeEvento = "Evento Reembolsado",
            DataEvento = DateTime.UtcNow.AddDays(30), Reembolsada = true
        };
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dto });

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);

        // Assert
        Assert.False(result.First().PodeCancelar);
    }

    [Fact]
    public async Task PodeCancelar_EventoPassado_DeveRetornarFalse()
    {
        // Arrange
        SetupService();
        var usuarioId = Guid.NewGuid();
        var dto = new ReservaDetalhadaDTO
        {
            Id = Guid.NewGuid(), NomeEvento = "Evento Passado",
            DataEvento = DateTime.UtcNow.AddDays(-1), Reembolsada = false
        };
        _reservaRepoMock.Setup(r => r.ListarReservasDetalhadasPorUsuarioId(usuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { dto });

        // Act
        var result = await _service.ListarMinhasReservas(usuarioId, CancellationToken.None);

        // Assert
        Assert.False(result.First().PodeCancelar);
    }
}
