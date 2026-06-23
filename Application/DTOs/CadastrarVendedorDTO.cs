using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// ST-01: DTO para auto cadastro público de vendedor.
/// </summary>
public class CadastrarVendedorDTO
{
    private string _cnpj = string.Empty;
    private string _razaoSocial = string.Empty;
    private string _nomeFantasia = string.Empty;
    private string _email = string.Empty;

    [Required]
    public string Cnpj
    {
        get => _cnpj;
        set => _cnpj = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string RazaoSocial
    {
        get => _razaoSocial;
        set => _razaoSocial = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string NomeFantasia
    {
        get => _nomeFantasia;
        set => _nomeFantasia = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string Email
    {
        get => _email;
        set => _email = value?.Trim() ?? string.Empty;
    }

    [Required]
    public string Senha { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;
}
