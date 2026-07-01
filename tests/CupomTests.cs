using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace SoldOutTickets.Tests;

public class CupomTests
{
    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarCupom()
    {
        // Arrange
        var codigo = "FESTA50";
        var desconto = 10;
        var valorMinimo = 50.00m;
        var dataExpiracao = DateTime.Now.AddDays(30);

        // Act
        var cupom = Cupom.Criar(codigo, desconto, valorMinimo, dataExpiracao);

        // Assert
        Assert.NotNull(cupom);
        Assert.Equal("FESTA50", cupom.Codigo);
        Assert.Equal(10, cupom.PorcentagemDesconto);
        Assert.True(cupom.Ativo);
    }

    [Fact]
    public void Criar_ComCodigoVazio_DeveLancarCodigoVazio()
    {
        // Arrange
        var codigo = "";

        // Act
        Action act = () => Cupom.Criar(codigo, 10, 50.00m, DateTime.Now.AddDays(30));

        // Assert
        Assert.Throws<CodigoVazio>(act);
    }

    [Theory]
    [InlineData("ABC")]          // muito curto (< 6)
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZABCDEFGHIJKLMNOPQRSTUVWXYZ")] // muito longo (>= 50)
    public void Criar_ComTamanhoInvalido_DeveLancarTamanhoCodigoInvalido(string codigo)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => Cupom.Criar(codigo, 10, 50.00m, DateTime.Now.AddDays(30));

        // Assert
        Assert.Throws<TamanhoCodigoInvalido>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(101)]
    public void Criar_ComDescontoInvalido_DeveLancarDescontoInvalido(int desconto)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => Cupom.Criar("FESTA50", desconto, 50.00m, DateTime.Now.AddDays(30));

        // Assert
        Assert.Throws<DescontoInvalido>(act);
    }

    [Fact]
    public void Criar_ComValorMinimoZero_DeveLancarValorMinimoInvalido()
    {
        // Arrange
        var valorMinimo = 0m;

        // Act
        Action act = () => Cupom.Criar("FESTA50", 10, valorMinimo, DateTime.Now.AddDays(30));

        // Assert
        Assert.Throws<ValorMinimoInvalido>(act);
    }

    [Fact]
    public void Criar_ComDataExpiracaoPassada_DeveLancarDataExpiracaoInvalida()
    {
        // Arrange
        var dataExpiracao = DateTime.Now.AddDays(-1);

        // Act
        Action act = () => Cupom.Criar("FESTA50", 10, 50.00m, dataExpiracao);

        // Assert
        Assert.Throws<DataExpiracaoInvalida>(act);
    }

    [Fact]
    public void AlternarStatus_QuandoAtivo_DeveInverterParaInativo()
    {
        // Arrange
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));
        Assert.True(cupom.Ativo);

        // Act
        cupom.AlternarStatus();

        // Assert
        Assert.False(cupom.Ativo);

        // Act (2ª inversão)
        cupom.AlternarStatus();

        // Assert (2ª inversão)
        Assert.True(cupom.Ativo);
    }

    [Fact]
    public void EstaValidoParaUso_ComCupomAtivoENaoExpirado_DeveRetornarTrue()
    {
        // Arrange
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));

        // Act
        var resultado = cupom.EstaValidoParaUso;

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void EstaValidoParaUso_ComCupomInativo_DeveRetornarFalse()
    {
        // Arrange
        var cupom = Cupom.Criar("FESTA50", 10, 50.00m, DateTime.Now.AddDays(30));
        cupom.AlternarStatus();

        // Act
        var resultado = cupom.EstaValidoParaUso;

        // Assert
        Assert.False(resultado);
    }
}
