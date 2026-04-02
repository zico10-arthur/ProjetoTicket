namespace Domain.Entities;

public class ItemReserva
{
    public Guid Id {get; private set;} = Guid.NewGuid();

    public Guid ReservaId {get; private set;}

    public decimal ValorUnitario {get; private set;}

    public Ingresso Ingresso { get; private set; }

    public Guid IngressoId { get; private set; }

    private ItemReserva(){}

    public ItemReserva(Guid reservaId, decimal valorUnitario, Guid ingressoid)
    {
        ReservaId = reservaId;
        ValorUnitario = valorUnitario;
        IngressoId = ingressoid;    
    }
}
