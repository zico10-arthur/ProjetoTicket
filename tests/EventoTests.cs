using Domain.Entities;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// ST-03: Testes de Tipo de Evento (Teatro/Palestra), geração de ingressos e gratuito.
/// </summary>
public class EventoTests
{
    private const string CpfVendedor = "52987654321";

    // ==================== Criação da Entidade ====================

    [Fact]
    public void CriarEvento_ComTipoPalestra_DeveAtribuirTipoCorreto()
    {
        // Arrange
        var nome = "Workshop .NET";
        var capacidade = 50;
        var data = DateTime.UtcNow.AddDays(10);
        var preco = 0;
        var tipo = TipoEvento.Palestra;
        var descricao = "Workshop prático";
        var local = "Auditório Central";

        // Act
        var evento = new Evento(nome, capacidade, data, preco, CpfVendedor, tipo, descricao, local);

        // Assert
        Assert.Equal(TipoEvento.Palestra, evento.Tipo);
        Assert.Equal("Workshop prático", evento.Descricao);
        Assert.Equal("Auditório Central", evento.Local);
        Assert.False(evento.Cancelado);
    }

    [Fact]
    public void CriarEvento_ComTipoTeatro_DeveAtribuirTipoCorreto()
    {
        // Arrange
        var nome = "Peça Hamlet";
        var capacidade = 100;
        var data = DateTime.UtcNow.AddDays(20);
        var preco = 80m;
        var tipo = TipoEvento.Teatro;
        var descricao = "Peça clássica";
        var local = "Teatro Municipal";

        // Act
        var evento = new Evento(nome, capacidade, data, preco, CpfVendedor, tipo, descricao, local);

        // Assert
        Assert.Equal(TipoEvento.Teatro, evento.Tipo);
    }

    [Fact]
    public void CriarEvento_ComTipoPadrao_DeveSerTeatro()
    {
        // Arrange
        var nome = "Evento Padrão";

        // Act
        var evento = new Evento(nome, 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);

        // Assert
        Assert.Equal(TipoEvento.Teatro, evento.Tipo);
    }

    // ==================== Gratuito ====================

    [Fact]
    public void Gratuito_QuandoPrecoZero_DeveRetornarTrue()
    {
        // Arrange
        var preco = 0;

        // Act
        var evento = new Evento("Evento Grátis", 50, DateTime.UtcNow.AddDays(10), preco, CpfVendedor,
            TipoEvento.Palestra);

        // Assert
        Assert.True(evento.Gratuito);
    }

    [Fact]
    public void Gratuito_QuandoPrecoMaiorQueZero_DeveRetornarFalse()
    {
        // Arrange
        var preco = 49.90m;

        // Act
        var evento = new Evento("Evento Pago", 50, DateTime.UtcNow.AddDays(10), preco, CpfVendedor);

        // Assert
        Assert.False(evento.Gratuito);
    }

    // ==================== Cancelar ====================

    [Fact]
    public void Cancelar_QuandoNaoCancelado_DeveMarcarComoTrue()
    {
        // Arrange
        var evento = new Evento("Evento", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);
        Assert.False(evento.Cancelado);

        // Act
        evento.Cancelar();

        // Assert
        Assert.True(evento.Cancelado);
    }

    // ==================== GerarLoteIngressos — Palestra ====================

    [Fact]
    public void GerarLoteIngressos_Palestra_DeveGerarTodosComoGeral()
    {
        // Arrange
        var evento = new Evento("Workshop", 5, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Palestra);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        Assert.Equal(5, evento.Ingressos.Count);
        Assert.All(evento.Ingressos, i => Assert.Equal("Geral", i.Setor));
        Assert.All(evento.Ingressos, i => Assert.Equal(50m, i.Preco));
    }

    [Fact]
    public void GerarLoteIngressos_Palestra_DeveNumerarAssentosSequencialmente()
    {
        // Arrange
        var evento = new Evento("Workshop", 3, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Palestra);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        Assert.Equal("Assento 1", evento.Ingressos[0].Posicao);
        Assert.Equal("Assento 2", evento.Ingressos[1].Posicao);
        Assert.Equal("Assento 3", evento.Ingressos[2].Posicao);
    }

    [Fact]
    public void GerarLoteIngressos_PalestraGratuito_DeveGerarComPrecoZero()
    {
        // Arrange
        var evento = new Evento("Workshop Grátis", 3, DateTime.UtcNow.AddDays(10), 0, CpfVendedor,
            TipoEvento.Palestra);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        Assert.All(evento.Ingressos, i => Assert.Equal(0m, i.Preco));
    }

    // ==================== GerarLoteIngressos — Teatro ====================

    [Fact]
    public void GerarLoteIngressos_Teatro_DeveGerar10PorcentoVip()
    {
        // Arrange
        var evento = new Evento("Peça", 100, DateTime.UtcNow.AddDays(10), 100m, CpfVendedor,
            TipoEvento.Teatro);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        Assert.Equal(100, evento.Ingressos.Count);

        var vipCount = evento.Ingressos.Count(i => i.Setor == "VIP");
        var geralCount = evento.Ingressos.Count(i => i.Setor == "Geral");

        Assert.Equal(10, vipCount);  // 10% de 100
        Assert.Equal(90, geralCount);
    }

    [Fact]
    public void GerarLoteIngressos_Teatro_VipDeveTerPreco50PorcentoMaior()
    {
        // Arrange
        var evento = new Evento("Peça", 100, DateTime.UtcNow.AddDays(10), 100m, CpfVendedor,
            TipoEvento.Teatro);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        var ingressosVip = evento.Ingressos.Where(i => i.Setor == "VIP");
        var ingressosGeral = evento.Ingressos.Where(i => i.Setor == "Geral");

        Assert.All(ingressosVip, i => Assert.Equal(150m, i.Preco));  // 100 * 1.5
        Assert.All(ingressosGeral, i => Assert.Equal(100m, i.Preco));
    }

    [Fact]
    public void GerarLoteIngressos_Teatro_DeveTerFilasDe20Assentos()
    {
        // Arrange
        var evento = new Evento("Peça", 25, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Teatro);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        // Fila 1 deve ter 20 assentos, Fila 2 deve ter 5
        var fila1 = evento.Ingressos.Where(i => i.Posicao.StartsWith("Fila 1")).ToList();
        var fila2 = evento.Ingressos.Where(i => i.Posicao.StartsWith("Fila 2")).ToList();

        Assert.Equal(20, fila1.Count);
        Assert.Equal(5, fila2.Count);
    }

    [Fact]
    public void GerarLoteIngressos_Teatro_DeveTerFormatoPosicaoCorreto()
    {
        // Arrange
        var evento = new Evento("Peça", 1, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Teatro);

        // Act
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Assert
        Assert.Equal("Fila 1 | Assento 1", evento.Ingressos[0].Posicao);
    }

    // ==================== Validações ====================

    [Fact]
    public void GerarLoteIngressos_ComQuantidadeZero_DeveLancarExcecao()
    {
        // Arrange
        var evento = new Evento("Evento", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);

        // Act
        var ex = Assert.Throws<ArgumentException>(() => evento.GerarLoteIngressos(0));

        // Assert
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void GerarLoteIngressos_ComQuantidadeNegativa_DeveLancarExcecao()
    {
        // Arrange
        var evento = new Evento("Evento", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);

        // Act
        var ex = Assert.Throws<ArgumentException>(() => evento.GerarLoteIngressos(-1));

        // Assert
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void GerarLoteIngressos_JaGerado_DeveLancarExcecao()
    {
        // Arrange
        var evento = new Evento("Evento", 10, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => evento.GerarLoteIngressos(evento.CapacidadeTotal));

        // Assert
        Assert.Contains("já foram gerados", ex.Message);
    }
}
