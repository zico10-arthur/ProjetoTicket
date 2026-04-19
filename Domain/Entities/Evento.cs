using Domain.Entities;

public class Evento
{
     public Guid id {get;private set;}  = Guid.NewGuid();

    public string Nome{get; private set;}

    public int CapacidadeTotal{get; private set;}

    public DateTime DataEvento {get; private set;}

    public decimal PrecoPadrao {get; private set;}

    public string VendedorCpf {get; private set;} = string.Empty;

    public List<Ingresso> Ingressos { get; private set; } = new();

    private Evento()
    {
        
    }

    public Evento(string nome, int capacidadetotal, DateTime dataevento, decimal precopadrao, string vendedorCpf)
    {
        Nome = nome;
        CapacidadeTotal = capacidadetotal;
        DataEvento = dataevento;
        PrecoPadrao = precopadrao;
        VendedorCpf = vendedorCpf;
    }

}