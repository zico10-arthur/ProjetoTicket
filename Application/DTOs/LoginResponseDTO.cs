namespace Application.DTOs;

/// <summary>
/// ST-08: Resposta do login unificado com JWT e dados do usuário.
/// Spec 200: UsuarioLoginDTO inclui Id (Guid).
/// </summary>
public class LoginResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public UsuarioLoginDTO Usuario { get; set; } = new();
}

public class UsuarioLoginDTO
{
    /// <summary>Spec 200: Id como Guid PK.</summary>
    public Guid Id { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = string.Empty;
}