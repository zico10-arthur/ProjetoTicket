namespace Web.Models;

public class EventoViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public int CapacidadeTotal { get; set; }
    public decimal PrecoPadrao { get; set; }
}
