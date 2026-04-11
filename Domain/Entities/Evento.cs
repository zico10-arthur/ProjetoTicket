using Domain.Entities;

public class Evento
{
    public Guid id {get;private set;}  = Guid.NewGuid();

    public string Nome{get; private set;}

    public int CapacidadeTotal{get; private set;}

    public DateTime DataEvento {get; private set;}

    public decimal PrecoPadrao {get; private set;}

    public string UsuarioCpf { get; private set; }

    public List<Ingresso> Ingressos { get; private set; } = new();

    private Evento()
    {
        
    }

    public Evento(string nome, int capacidadetotal, DateTime dataevento, decimal precopadrao, string usuarioCpf)
    {
        Nome = nome;
        CapacidadeTotal = capacidadetotal;
        DataEvento = dataevento;
        PrecoPadrao = precopadrao;
        UsuarioCpf = usuarioCpf;
    }

    public void DefinirUsuarioCpf(string cpf) => UsuarioCpf = cpf;

    public void GerarLoteIngressos(int quantidadeDesejada)
{
    if (quantidadeDesejada <= 0)
        throw new ArgumentException("Quantidade deve ser maior que zero");

    if (Ingressos.Any())
        throw new InvalidOperationException("Ingressos já foram gerados.");

    int quantidadeVip = (int)(quantidadeDesejada * 0.1);
    int assentosPorFila = 20;

    for (int i = 1; i <= quantidadeDesejada; i++)
    {
        string setorAtual = (i <= quantidadeVip) ? "VIP" : "Geral";

        int fila = ((i - 1) / assentosPorFila) + 1;
        int assento = ((i - 1) % assentosPorFila) + 1;

        string posicao = $"Fila {fila} | Assento {assento}";

        decimal preco = setorAtual == "VIP" ? PrecoPadrao * 1.5m : PrecoPadrao;

        var ingresso = new Ingresso(
            this.id,
            preco,
            posicao,
            setorAtual
        );

        Ingressos.Add(ingresso);
    }
}

}