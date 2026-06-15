using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class EventoRequestDTO
{
    [Required]
    [MaxLength(100)]
    public string Nome { get; set; }

    public int CapacidadeTotal { get; set; }

    public DateTime DataEvento { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "O preço não pode ser negativo.")]
    public decimal PrecoPadrao { get; set; }

    public int Tipo { get; set; }

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [MaxLength(200)]
    public string? Local { get; set; }

    public string VendedorCpf { get; set; } = string.Empty;
}
