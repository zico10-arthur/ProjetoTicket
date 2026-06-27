using Domain.Exceptions;
using Domain.Validators;

namespace Domain.Entities;

public class Reserva
{
    private const int LimiteMaximoItens = 4;

    public Guid Id { get; private set; } = Guid.NewGuid();
    /// <summary>Spec 200: Guid FK para Usuarios.</summary>
    public Guid UsuarioId { get; private set; }
    public Guid EventoId { get; private set; }
    /// <summary>Spec 200: Guid FK para vendedor (nullable).</summary>
    public Guid? VendedorId { get; private set; }
    public string? CupomUtilizado { get; private set; }
    public decimal ValorFinalPago { get; private set; }
    public bool Pago { get; private set; }
    public List<ItemReserva> Itens { get; private set; } = new();

    public bool PodeAdicionarMaisItens => Itens.Count < LimiteMaximoItens;

    protected Reserva() { }

    private Reserva(Guid usuarioId, Guid eventoId, string? cupomUtilizado, decimal valorFinalPago)
    {
        UsuarioId = usuarioId;
        EventoId = eventoId;
        CupomUtilizado = cupomUtilizado;
        ValorFinalPago = valorFinalPago;
        Pago = false;
    }

    public void MarcarPago()
    {
        Pago = true;
    }

    /// <summary>
    /// Cria uma reserva com 1 a 4 itens. Valida CPFs, duplicatas, e aplica cupom sobre o valor total.
    /// Spec 200: usuarioId (Guid), vendedorId (Guid?).
    /// </summary>
    public static Reserva Criar(Guid usuarioId, Guid eventoId, List<ItemReserva> itens, Cupom? cupom = null, Guid? vendedorId = null)
    {
        ValidarItens(itens);

        foreach (var item in itens)
            CpfValidator.Validar(item.CpfParticipante);

        decimal valorTotal = itens.Sum(i => i.PrecoUnitario);
        string? codigoCupom = null;
        decimal valorFinal = valorTotal;

        if (cupom != null)
        {
            ValidarUsoDoCupom(cupom, valorTotal);
            decimal desconto = valorTotal * (cupom.PorcentagemDesconto / 100m);
            valorFinal = Math.Max(0, valorTotal - desconto);
            codigoCupom = cupom.Codigo;
        }

        return new Reserva(usuarioId, eventoId, codigoCupom, valorFinal)
        {
            Itens = itens,
            VendedorId = vendedorId
        };
    }

    private static void ValidarItens(List<ItemReserva> itens)
    {
        if (itens == null || itens.Count < 1)
            throw new DomainException("É necessário pelo menos 1 participante.");

        if (itens.Count > LimiteMaximoItens)
            throw new DomainException($"Limite máximo de {LimiteMaximoItens} participantes por reserva.");

        var cpfs = itens.Select(i => i.CpfParticipante).ToList();
        var duplicado = cpfs.GroupBy(c => c).FirstOrDefault(g => g.Count() > 1);
        if (duplicado != null)
            throw new DomainException($"CPF {duplicado.Key} informado mais de uma vez.");
    }

    private static void ValidarUsoDoCupom(Cupom cupom, decimal valorBase)
    {
        if (!cupom.EstaValidoParaUso)
            throw new CupomInvalidoParaUso();

        if (valorBase < cupom.ValorMinimo)
            throw new ValorMinimoCupomExcedido(cupom.ValorMinimo);
    }
}
