namespace Domain.Entities;

public class ItemReserva
{
    public Guid Id { get; private set; }
    public Guid ReservaId { get; private set; }
    public string CpfParticipante { get; private set; }
    public Guid IngressoId { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public bool Reembolsado { get; private set; }

    private ItemReserva() { }

    public ItemReserva(string cpfParticipante, Guid ingressoId, decimal precoUnitario)
    {
        Id = Guid.NewGuid();
        CpfParticipante = cpfParticipante;
        IngressoId = ingressoId;
        PrecoUnitario = precoUnitario;
        Reembolsado = false;
    }

    public void VincularReserva(Guid reservaId)
    {
        ReservaId = reservaId;
    }

    public void MarcarReembolsado()
    {
        Reembolsado = true;
    }
}
