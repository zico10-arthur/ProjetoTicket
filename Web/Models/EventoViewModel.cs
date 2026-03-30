namespace Web.Models;

public class EventoViewModel
{
    public string Titulo { get; set; } = string.Empty;
    public DateTime Data { get; set; }
    public int Capacidade { get; set; }
    public decimal Preco { get; set; }
}