using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace SoldOutTickets.Tests;

public class CupomTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarCupom()
    {
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));

        Assert.NotNull(cupom);
        Assert.Equal("FESTA50", cupom.Codigo);
        Assert.Equal(10, cupom.PorcentagemDesconto);
        Assert.True(cupom.Ativo);
    }

    [Fact]
    public void Criar_ComCodigoVazio_DeveLancarCodigoVazio()
    {
        Assert.Throws<CodigoVazio>(() =>
            Cupom.Criar("", 10, 50.00m, DateTime.Now.AddDays(30)));
    }

    [Theory]
    [InlineData("ABC")]          // muito curto (< 6)
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ")] // muito longo (>= 50)
    public void Criar_ComTamanhoInvalido_DeveLancarTamanhoCodigoInvalido(string codigo)
    {
        Assert.Throws<TamanhoCodigoInvalido>(() =>
            Cupom.Criar(codigo, 10, 50.00m, DateTime.Now.AddDays(30)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(101)]
    public void Criar_ComDescontoInvalido_DeveLancarDescontoInvalido(int desconto)
    {
        Assert.Throws<DescontoInvalido>(() =>
            Cupom.Criar("FESTA50", desconto, 50.00m, DateTime.Now.AddDays(30)));
    }

    [Fact]
    public void Criar_ComValorMinimoZero_DeveLancarValorMinimoInvalido()
    {
        Assert.Throws<ValorMinimoInvalido>(() =>
            Cupom.Criar("FESTA50", 10, 0m, DateTime.Now.AddDays(30)));
    }

    [Fact]
    public void Criar_ComDataExpiracaoPassada_DeveLancarDataExpiracaoInvalida()
    {
        Assert.Throws<DataExpiracaoInvalida>(() =>
            Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(-1)));
    }

    [Fact]
    public void AlternarStatus_DeveInverterStatusAtivo()
    {
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));
        Assert.True(cupom.Ativo);

        cupom.AlternarStatus();
        Assert.False(cupom.Ativo);

        cupom.AlternarStatus();
        Assert.True(cupom.Ativo);
    }

    [Fact]
    public void EstaValidoParaUso_ComCupomAtivoENaoExpirado_DeveRetornarTrue()
    {
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));

        Assert.True(cupom.EstaValidoParaUso);
    }

    [Fact]
    public void EstaValidoParaUso_ComCupomInativo_DeveRetornarFalse()
    {
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));
        cupom.AlternarStatus();

        Assert.False(cupom.EstaValidoParaUso);
    }
}
