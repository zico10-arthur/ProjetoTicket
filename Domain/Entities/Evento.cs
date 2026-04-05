public class Evento
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Nome { get; private set; }

    public int CapacidadeTotal { get; private set; }

    public DateTime DataEvento { get; private set; }

    public decimal PrecoPadrao { get; private set; }

    public Evento() {}

    public Evento(string nome, int capacidadetotal, DateTime dataevento, decimal precopadrao)
    {
        Nome = nome;
        CapacidadeTotal = capacidadetotal;
        DataEvento = dataevento;
        PrecoPadrao = precopadrao;
    }
}