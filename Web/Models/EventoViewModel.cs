namespace Web.Models;

public class EventoViewModel
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public int CapacidadeTotal { get; set; }
    public decimal PrecoPadrao { get; set; }
    public int Tipo { get; set; }
    public bool Gratuito { get; set; }
    public bool Cancelado { get; set; }
    public string? Descricao { get; set; }
    public string? Local { get; set; }

    public string TipoLabel => Tipo == 1 ? "Palestra" : "Teatro";

    public bool IsGratuito => Gratuito || PrecoPadrao == 0;
}
