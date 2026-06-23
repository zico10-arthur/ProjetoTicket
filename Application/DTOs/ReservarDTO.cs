using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class ReservarDTO
{
    [Required(ErrorMessage = "EventoId é obrigatório.")]
    public Guid EventoId { get; set; }

    [Required(ErrorMessage = "Itens é obrigatório.")]
    [MinLength(1, ErrorMessage = "Pelo menos um item é obrigatório.")]
    public List<ItemReservaRequestDTO> Itens { get; set; } = new();

    public string? CupomCodigo { get; set; }
}
