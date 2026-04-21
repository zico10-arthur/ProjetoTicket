namespace Domain.DTOs;

public class ReservaDetalhadaDTO
{
    public Guid Id { get; set; }
    public string NomeEvento { get; set; } = "";
    public DateTime DataEvento { get; set; }
    public string PosicaoIngresso { get; set; } = "";
    public string SetorIngresso { get; set; } = "";
    public string? CupomUtilizado { get; set; }
    public decimal ValorFinalPago { get; set; }
}