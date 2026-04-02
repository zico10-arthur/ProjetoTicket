namespace Domain.Entities;

public class Pagamento
{
    public Guid Id {get; private set;} = Guid.NewGuid();

    public Guid ReservaId { get; private set; }
    public Reserva Reserva { get; private set; }

    public decimal ValorPago { get; private set; }
    public DateTime DataPagamento { get; private set; }
    

    private Pagamento() {}

    public Pagamento(Guid reservaid, decimal valorpago, DateTime datapagamento)
    {
        ReservaId = reservaid;
        ValorPago = valorpago;
        DataPagamento = datapagamento;
        
    }
}
