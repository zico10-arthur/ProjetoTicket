using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CadastrarCupomDTO
{
    private string _codigo = string.Empty;

    [Required(ErrorMessage = "Código é obrigatório.")]
    [MaxLength(50)]
    public string Codigo
    {
        get => _codigo;
        set => _codigo = value?.Trim() ?? string.Empty;
    }

    [Required]
    [Range(1, 100, ErrorMessage = "Desconto deve ser entre 1 e 100.")]
    public int PorcentagemDesconto { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Valor mínimo não pode ser negativo.")]
    public decimal ValorMinimo { get; set; }

    public DateTime? DataExpiracao { get; set; }
}
