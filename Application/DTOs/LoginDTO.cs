using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class LoginDTO
{
    private string _email = string.Empty;

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    [Required(ErrorMessage = "Senha é obrigatória.")]
    public string Senha { get; set; } = string.Empty;
}
