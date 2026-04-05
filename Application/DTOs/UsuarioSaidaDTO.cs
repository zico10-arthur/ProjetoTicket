using Domain.Entities;

namespace Application.DTOs;

public class UsuarioSaidaDTO
{
    public string Cpf {get; set;} = string.Empty;

    public string Nome{get; set;} = string.Empty;

    public string Email{get;  set;} = string.Empty;

    public PerfilDTO Perfil {get; set;}
}
