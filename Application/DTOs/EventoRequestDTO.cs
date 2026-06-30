using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class EventoRequestDTO
{
    private string _nome = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Nome
    {
        get => _nome;
        set => _nome = value?.Trim() ?? string.Empty;
    }

    public int CapacidadeTotal { get; set; }

    public DateTime DataEvento { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "O preço não pode ser negativo.")]
    public decimal PrecoPadrao { get; set; }

    public int Tipo { get; set; }

    [MaxLength(500)]
    public string? Descricao { get; set; }

    [MaxLength(200)]
    public string? Local { get; set; }

    // Spec 200: VendedorCpf removido — vendedor é identificado pelo JWT (userId).
}
