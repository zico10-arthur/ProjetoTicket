using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class AlterarNomeDTO
{
    private string _novoNome = string.Empty;

    [Required(ErrorMessage = "Novo nome é obrigatório.")]
    [MaxLength(200)]
    public string NovoNome
    {
        get => _novoNome;
        set => _novoNome = value?.Trim() ?? string.Empty;
    }
}