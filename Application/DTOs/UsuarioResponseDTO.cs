using System.Dynamic;

namespace Application.DTOs;

public class UsuarioResponseDTO
{
    public string Cpf {get; set;} = string.Empty;
    public string Nome {get;set;} = string.Empty;
    public string Email {get;set;} = string.Empty;
    public string Perfil{get;set;} = "Sem Perfil";
}