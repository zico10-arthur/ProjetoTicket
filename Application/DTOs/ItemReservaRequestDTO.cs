namespace Application.DTOs;

public class ItemReservaRequestDTO
{
    public string CpfParticipante { get; set; } = string.Empty;
    public Guid IngressoId { get; set; }
}
