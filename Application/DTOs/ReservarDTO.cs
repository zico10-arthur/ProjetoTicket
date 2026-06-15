namespace Application.DTOs;

public class ReservarDTO
{
    public Guid EventoId { get; set; }
    public List<ItemReservaRequestDTO> Itens { get; set; } = new();
    public string? CupomCodigo { get; set; }
}
