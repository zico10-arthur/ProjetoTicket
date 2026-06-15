using Domain.Entities;
using Domain.Exceptions;
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
}
