namespace Domain.Entities;

public class Ingresso
{
     public Guid Id {get; private set;} = Guid.NewGuid();

    public Guid EventoId {get; private set;}

    public Evento Evento {get; private set;}

    public decimal Preco {get; private set;}

    public string Posicao {get; private set;}

    public string Setor {get; private set;}

    public int Status {get; private set;} = 0;

    public DateTime? DataBloqueio { get; private set; }


    private Ingresso() { }

    public Ingresso(Guid eventoid, decimal preco, string posicao, string setor)
    {
        EventoId = eventoid;
        Preco = preco;
        Posicao = posicao;
        Setor = setor;
        Status = 0;
    }

    public void Reservar() => Status = 1;
    public void Vender() => Status = 2;
    public void Liberar() => Status = 0;
    
}
