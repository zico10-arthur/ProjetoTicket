namespace Application.DTOs;

/// <summary>
/// ST-01: DTO de resposta para o cadastro de vendedor.
/// Spec 200: Inclui Id (Guid), remove Cpf.
/// </summary>
public class VendedorCadastradoDTO
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string NomeFantasia { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
    public string Plano { get; set; } = string.Empty;
}