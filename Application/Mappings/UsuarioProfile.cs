using AutoMapper;
using Application.DTOs;
using Domain.Entities;
namespace Application.Mappings;

public class UsuarioProfile : Profile
{
    public UsuarioProfile()
    {
        CreateMap<Usuario, UsuarioSaidaDTO>();
        CreateMap<Perfil, PerfilDTO>();
    }
}
