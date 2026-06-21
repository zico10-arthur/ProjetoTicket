using Domain.Enums;

namespace Domain.Entities;

public class Pagamento
{
    public Guid Id { get; private set; }
    public Guid ReservaId { get; private set; }
    public decimal ValorPago { get; private set; }
    public StatusPagamento Status { get; private set; }
    public string Metodo { get; private set; }
    public DateTime DataPagamento { get; private set; }
    public DateTime DataCriacao { get; private set; }

    public Reserva Reserva { get; private set; }

    private Pagamento() { }

    public Pagamento(Guid reservaId, decimal valorPago, string metodo)
    {
        Id = Guid.NewGuid();
        ReservaId = reservaId;
        ValorPago = valorPago;
        Metodo = metodo;
        Status = StatusPagamento.Confirmado;
        DataPagamento = DateTime.UtcNow;
        DataCriacao = DateTime.UtcNow;
    }

    public void MarcarReembolsado()
    {
        Status = StatusPagamento.Reembolsado;
    }
}
