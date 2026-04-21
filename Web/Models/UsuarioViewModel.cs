namespace Web.Models;

public class UsuarioViewModel
{
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public Guid PerfilId { get; set; } 
    public string Senha { get; set; } = string.Empty;
}