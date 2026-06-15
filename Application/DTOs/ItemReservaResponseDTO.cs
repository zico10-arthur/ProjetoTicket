namespace Application.DTOs;

public class ItemReservaResponseDTO
{
    public string CpfParticipante { get; set; } = string.Empty;
    public Guid IngressoId { get; set; }
    public decimal PrecoUnitario { get; set; }
}
