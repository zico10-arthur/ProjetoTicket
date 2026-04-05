using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class EventoRequestDTO
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Capacidade deve ser maior que zero.")]
    public int CapacidadeTotal { get; set; }

    public DateTime DataEvento { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "O preço deve ser maior que zero.")]
    public decimal PrecoPadrao { get; set; }
}
