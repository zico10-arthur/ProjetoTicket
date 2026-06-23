using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class AlterarEmailDTO
{
    private string _novoEmail = string.Empty;

    [Required(ErrorMessage = "Novo email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [MaxLength(200)]
    public string NovoEmail
    {
        get => _novoEmail;
        set => _novoEmail = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }
}