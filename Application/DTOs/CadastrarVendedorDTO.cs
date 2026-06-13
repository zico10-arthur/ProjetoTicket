using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

/// <summary>
/// ST-01: DTO para auto cadastro público de vendedor.
/// </summary>
public class CadastrarVendedorDTO
{
    [Required]
    public string Cnpj { get; set; } = string.Empty;

    [Required]
    public string RazaoSocial { get; set; } = string.Empty;

    [Required]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;

    public string Telefone { get; set; } = string.Empty;
}
