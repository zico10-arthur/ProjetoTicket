namespace Domain.DTOs;

public class ItemReservaResponseDTO
{
    public Guid Id { get; set; }
    public string CpfParticipante { get; set; } = string.Empty;
    public Guid IngressoId { get; set; }
    public decimal PrecoUnitario { get; set; }
    public bool Reembolsado { get; set; }
}
