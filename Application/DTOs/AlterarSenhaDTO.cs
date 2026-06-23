using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class AlterarSenhaDTO
{
    [Required(ErrorMessage = "Nova senha é obrigatória.")]
    [MaxLength(100)]
    public string NovaSenha { get; set; } = string.Empty;
}
