namespace Application.DTOs;

public class UsuarioResponseDTO
{
    /// <summary>Spec 200: Id como Guid PK.</summary>
    public Guid Id { get; set; }
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Perfil { get; set; } = "Sem Perfil";
}