using AutoMapper;
using Application.DTOs;
using Domain.Entities;

namespace Application.Mappings;

public class EventoProfile : Profile
{
    public EventoProfile()
    {
        CreateMap<Evento, EventoResponseDTO>();
        CreateMap<EventoRequestDTO, Evento>();
    }
}
