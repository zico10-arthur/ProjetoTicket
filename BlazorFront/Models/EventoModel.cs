namespace BlazorFront.Models;

public class EventoModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public int CapacidadeTotal { get; set; }
    public DateTime DataEvento { get; set; }
    public decimal PrecoPadrao { get; set; }
}
