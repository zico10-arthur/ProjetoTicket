using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CadastrarUsuarioDTO
{
    private string _nome = string.Empty;
    private string _email = string.Empty;
    private string _cpf = string.Empty;

    [Required(ErrorMessage = "Nome é obrigatório.")]
    [MaxLength(200)]
    public string Nome
    {
        get => _nome;
        set => _nome = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Email é obrigatório.")]
    [EmailAddress(ErrorMessage = "Email inválido.")]
    [MaxLength(200)]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "CPF é obrigatório.")]
    [MaxLength(14)]
    public string Cpf
    {
        get => _cpf;
        set => _cpf = value?.Trim() ?? string.Empty;
    }

    [Required(ErrorMessage = "Senha é obrigatória.")]
    [MaxLength(100)]
    public string Senha { get; set; } = string.Empty;
}
