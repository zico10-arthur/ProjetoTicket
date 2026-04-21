using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mappings;

public class IngressoProfile : Profile
{
     public IngressoProfile()
    {
        CreateMap<Ingresso, IngressoResponseDTO>();
    }
}
