using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace SoldOutTickets.Tests;

public class ReservaTests
{
    private static readonly Guid PerfilComprador = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");
    private const string CpfValido = "52998224725";

    private Evento CriarEvento()
    {
        return new Evento("Show de Rock", 100, DateTime.Now.AddDays(30), 100.00m, "52998224725");
    }

    private Ingresso CriarIngresso(Evento evento)
    {
        return new Ingresso(evento.id, 100.00m, "Fila 1 | Assento 1", "Geral");
    }

    [Fact]
    public void Criar_SemCupom_DeveUsarPrecoCheio()
    {
        var evento = CriarEvento();
        var ingresso = CriarIngresso(evento);

        var reserva = Reserva.Criar(CpfValido, evento, ingresso);

        Assert.NotNull(reserva);
        Assert.Equal(100.00m, reserva.ValorFinalPago);
    }

    [Fact]
    public void Criar_ComCupomValido_DeveAplicarDesconto()
    {
        var evento = CriarEvento();
        var ingresso = CriarIngresso(evento);
        var cupom = Cupom.Criar("DESC10", 10, 50.00m, DateTime.Now.AddDays(30));

        var reserva = Reserva.Criar(CpfValido, evento, ingresso, cupom);

        Assert.Equal(90.00m, reserva.ValorFinalPago);
        Assert.Equal("DESC10", reserva.CupomUtilizado);
    }

    [Fact]
    public void Criar_ComCupomAbaixoDoValorMinimo_DeveLancarExcecao()
    {
        var evento = CriarEvento();
        var ingresso = CriarIngresso(evento);
        var cupom = Cupom.Criar("ALTO50", 10, 200.00m, DateTime.Now.AddDays(30));

        Assert.Throws<ValorMinimoCupomExcedido>(() =>
            Reserva.Criar(CpfValido, evento, ingresso, cupom));
    }

    [Fact]
    public void Criar_ComCupomInativo_DeveLancarExcecao()
    {
        var evento = CriarEvento();
        var ingresso = CriarIngresso(evento);
        var cupom = Cupom.Criar("INATI50", 10, 50.00m, DateTime.Now.AddDays(30));
        cupom.AlternarStatus();

        Assert.Throws<CupomInvalidoParaUso>(() =>
            Reserva.Criar(CpfValido, evento, ingresso, cupom));
    }

    [Fact]
    public void Criar_ComCpfInvalido_DeveLancarExcecao()
    {
        var evento = CriarEvento();
        var ingresso = CriarIngresso(evento);

        Assert.Throws<CpfInvalido>(() =>
            Reserva.Criar("12345678900", evento, ingresso));
    }
}
