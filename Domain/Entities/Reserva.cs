using Domain.Entities;

public class Reserva
{
    public Guid Id {get; private set;} = Guid.NewGuid();

    public Usuario usuario {get; private set;}

    public string UsuarioCpf{get; private set;}

    public Evento Evento {get; private set;}

    public Guid EventoId {get; private set;}

    public List<ItemReserva> Itens { get; private set; } = new();

    public string CupomUtilizado {get; private set;}

    public decimal ValorFinalPago => Itens.Sum(x => x.ValorUnitario);

    private Reserva(){}
    public Reserva(string usuariocpf, Guid eventoid, string cupomutilizado)
    {
        UsuarioCpf = usuariocpf;
        EventoId = eventoid;
        CupomUtilizado = cupomutilizado;
    }
}