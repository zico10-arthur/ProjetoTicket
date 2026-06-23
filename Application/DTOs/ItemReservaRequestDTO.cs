using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class ItemReservaRequestDTO
{
    private string _cpfParticipante = string.Empty;

    [Required(ErrorMessage = "CPF do participante é obrigatório.")]
    [MaxLength(14)]
    public string CpfParticipante
    {
        get => _cpfParticipante;
        set => _cpfParticipante = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "IngressoId é obrigatório.")]
    public Guid IngressoId { get; set; }
}
