using Domain.Entities;

public class Evento
{
    public Guid id {get;private set;}  = Guid.NewGuid();

    public string Nome{get; private set;}

    public int CapacidadeTotal{get; private set;}

    public DateTime DataEvento {get; private set;}

    public decimal PrecoPadrao {get; private set;}

    public string VendedorCpf {get; private set;} = string.Empty;

    public TipoEvento Tipo { get; private set; }

    public string? Descricao { get; private set; }

    public string? Local { get; private set; }

    public bool Cancelado { get; private set; }

    public DateTime DataCriacao { get; private set; }

    public bool Gratuito => PrecoPadrao == 0;

    public List<Ingresso> Ingressos { get; private set; } = new();

    private Evento()
    {
        
    }

    public Evento(string nome, int capacidadetotal, DateTime dataevento, decimal precopadrao, string vendedorCpf,
        TipoEvento tipo = TipoEvento.Teatro, string? descricao = null, string? local = null)
    {
        Nome = nome;
        CapacidadeTotal = capacidadetotal;
        DataEvento = dataevento;
        PrecoPadrao = precopadrao;
        VendedorCpf = vendedorCpf;
        Tipo = tipo;
        Descricao = descricao;
        Local = local;
        DataCriacao = DateTime.UtcNow;
    }

    public void Cancelar()
    {
        Cancelado = true;
    }

    public void GerarLoteIngressos(int quantidadeDesejada)
    {
        if (quantidadeDesejada <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero");

        if (Ingressos.Any())
            throw new InvalidOperationException("Ingressos já foram gerados.");

        if (Tipo == TipoEvento.Palestra)
            GerarPalestra(quantidadeDesejada);
        else
            GerarTeatro(quantidadeDesejada);
    }

    private void GerarPalestra(int capacidade)
    {
        for (int i = 1; i <= capacidade; i++)
        {
            Ingressos.Add(new Ingresso(
                this.id,
                PrecoPadrao,
                $"Assento {i}",
                "Geral"
            ));
        }
    }

    private void GerarTeatro(int capacidade)
    {
        int quantidadeVip = (int)(capacidade * 0.1);
        int assentosPorFila = 20;
        int fila = 1, assento = 1;

        for (int i = 0; i < quantidadeVip; i++)
        {
            Ingressos.Add(new Ingresso(
                this.id,
                PrecoPadrao * 1.5m,
                $"Fila {fila} | Assento {assento}",
                "VIP"
            ));
            if (++assento > assentosPorFila) { fila++; assento = 1; }
        }

        for (int i = quantidadeVip; i < capacidade; i++)
        {
            Ingressos.Add(new Ingresso(
                this.id,
                PrecoPadrao,
                $"Fila {fila} | Assento {assento}",
                "Geral"
            ));
            if (++assento > assentosPorFila) { fila++; assento = 1; }
        }
    }

}