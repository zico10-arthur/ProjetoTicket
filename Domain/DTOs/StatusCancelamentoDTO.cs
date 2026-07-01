namespace Domain.DTOs;

public class StatusCancelamentoDTO
{
    public Guid EventoId { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Gratuito { get; set; }
    public int TotalIngressosVendidos { get; set; }
    public int TotalReservasAtivas { get; set; }
    public bool ReembolsoNecessario { get; set; }
    public decimal ValorTotalReembolso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
}
