namespace Application.DTOs;

/// <summary>
/// ST-01: DTO de resposta para o cadastro de vendedor.
/// </summary>
public class VendedorCadastradoDTO
{
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Plano { get; set; } = string.Empty;
}
