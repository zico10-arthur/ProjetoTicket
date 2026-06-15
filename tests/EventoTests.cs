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
        var evento = new Evento("Workshop .NET", 50, DateTime.UtcNow.AddDays(10), 0, CpfVendedor,
            TipoEvento.Palestra, "Workshop prático", "Auditório Central");

        Assert.Equal(TipoEvento.Palestra, evento.Tipo);
        Assert.Equal("Workshop prático", evento.Descricao);
        Assert.Equal("Auditório Central", evento.Local);
        Assert.False(evento.Cancelado);
    }

    [Fact]
    public void CriarEvento_ComTipoTeatro_DeveAtribuirTipoCorreto()
    {
        var evento = new Evento("Peça Hamlet", 100, DateTime.UtcNow.AddDays(20), 80m, CpfVendedor,
            TipoEvento.Teatro, "Peça clássica", "Teatro Municipal");

        Assert.Equal(TipoEvento.Teatro, evento.Tipo);
    }

    [Fact]
    public void CriarEvento_ComTipoPadrao_DeveSerTeatro()
    {
        var evento = new Evento("Evento Padrão", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);

        Assert.Equal(TipoEvento.Teatro, evento.Tipo);
    }

    // ==================== Gratuito ====================

    [Fact]
    public void Gratuito_QuandoPrecoZero_DeveRetornarTrue()
    {
        var evento = new Evento("Evento Grátis", 50, DateTime.UtcNow.AddDays(10), 0, CpfVendedor,
            TipoEvento.Palestra);

        Assert.True(evento.Gratuito);
    }

    [Fact]
    public void Gratuito_QuandoPrecoMaiorQueZero_DeveRetornarFalse()
    {
        var evento = new Evento("Evento Pago", 50, DateTime.UtcNow.AddDays(10), 49.90m, CpfVendedor);

        Assert.False(evento.Gratuito);
    }

    // ==================== Cancelar ====================

    [Fact]
    public void Cancelar_DeveMarcarCanceladoComoTrue()
    {
        var evento = new Evento("Evento", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);
        Assert.False(evento.Cancelado);

        evento.Cancelar();

        Assert.True(evento.Cancelado);
    }

    // ==================== GerarLoteIngressos — Palestra ====================

    [Fact]
    public void GerarLoteIngressos_Palestra_DeveGerarTodosComoGeral()
    {
        var evento = new Evento("Workshop", 5, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Palestra);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        Assert.Equal(5, evento.Ingressos.Count);
        Assert.All(evento.Ingressos, i => Assert.Equal("Geral", i.Setor));
        Assert.All(evento.Ingressos, i => Assert.Equal(50m, i.Preco));
    }

    [Fact]
    public void GerarLoteIngressos_Palestra_DeveNumerarAssentosSequencialmente()
    {
        var evento = new Evento("Workshop", 3, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Palestra);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        Assert.Equal("Assento 1", evento.Ingressos[0].Posicao);
        Assert.Equal("Assento 2", evento.Ingressos[1].Posicao);
        Assert.Equal("Assento 3", evento.Ingressos[2].Posicao);
    }

    [Fact]
    public void GerarLoteIngressos_PalestraGratuito_DeveGerarComPrecoZero()
    {
        var evento = new Evento("Workshop Grátis", 3, DateTime.UtcNow.AddDays(10), 0, CpfVendedor,
            TipoEvento.Palestra);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        Assert.All(evento.Ingressos, i => Assert.Equal(0m, i.Preco));
    }

    // ==================== GerarLoteIngressos — Teatro ====================

    [Fact]
    public void GerarLoteIngressos_Teatro_DeveGerar10PorcentoVip()
    {
        var evento = new Evento("Peça", 100, DateTime.UtcNow.AddDays(10), 100m, CpfVendedor,
            TipoEvento.Teatro);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        Assert.Equal(100, evento.Ingressos.Count);

        var vipCount = evento.Ingressos.Count(i => i.Setor == "VIP");
        var geralCount = evento.Ingressos.Count(i => i.Setor == "Geral");

        Assert.Equal(10, vipCount);  // 10% de 100
        Assert.Equal(90, geralCount);
    }

    [Fact]
    public void GerarLoteIngressos_Teatro_VipDeveTerPreco50PorcentoMaior()
    {
        var evento = new Evento("Peça", 100, DateTime.UtcNow.AddDays(10), 100m, CpfVendedor,
            TipoEvento.Teatro);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        var ingressosVip = evento.Ingressos.Where(i => i.Setor == "VIP");
        var ingressosGeral = evento.Ingressos.Where(i => i.Setor == "Geral");

        Assert.All(ingressosVip, i => Assert.Equal(150m, i.Preco));  // 100 * 1.5
        Assert.All(ingressosGeral, i => Assert.Equal(100m, i.Preco));
    }

    [Fact]
    public void GerarLoteIngressos_Teatro_DeveTerFilasDe20Assentos()
    {
        var evento = new Evento("Peça", 25, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Teatro);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        // Fila 1 deve ter 20 assentos, Fila 2 deve ter 5
        var fila1 = evento.Ingressos.Where(i => i.Posicao.StartsWith("Fila 1")).ToList();
        var fila2 = evento.Ingressos.Where(i => i.Posicao.StartsWith("Fila 2")).ToList();

        Assert.Equal(20, fila1.Count);
        Assert.Equal(5, fila2.Count);
    }

    [Fact]
    public void GerarLoteIngressos_Teatro_FormatoPosicaoCorreto()
    {
        var evento = new Evento("Peça", 1, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor,
            TipoEvento.Teatro);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        Assert.Equal("Fila 1 | Assento 1", evento.Ingressos[0].Posicao);
    }

    // ==================== Validações ====================

    [Fact]
    public void GerarLoteIngressos_ComQuantidadeZero_DeveLancarExcecao()
    {
        var evento = new Evento("Evento", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);

        var ex = Assert.Throws<ArgumentException>(() => evento.GerarLoteIngressos(0));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void GerarLoteIngressos_ComQuantidadeNegativa_DeveLancarExcecao()
    {
        var evento = new Evento("Evento", 50, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);

        var ex = Assert.Throws<ArgumentException>(() => evento.GerarLoteIngressos(-1));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void GerarLoteIngressos_JaGerado_DeveLancarExcecao()
    {
        var evento = new Evento("Evento", 10, DateTime.UtcNow.AddDays(10), 50m, CpfVendedor);
        evento.GerarLoteIngressos(evento.CapacidadeTotal);

        var ex = Assert.Throws<InvalidOperationException>(() => evento.GerarLoteIngressos(evento.CapacidadeTotal));
        Assert.Contains("já foram gerados", ex.Message);
    }
}
