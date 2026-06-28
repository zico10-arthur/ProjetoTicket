using AutoMapper;
using Application.DTOs;
using Domain.Entities;
using Domain.DTOs;

namespace Application.Mappings;

public class ReservaProfile : Profile
{
    public ReservaProfile()
    {
        CreateMap<ItemReservaRequestDTO, ItemReserva>()
            .ConstructUsing(dto => new ItemReserva(dto.CpfParticipante, dto.IngressoId, 0))
            .ForMember(dest => dest.PrecoUnitario, opt => opt.Ignore());

        CreateMap<ItemReserva, ItemReservaResponseDTO>();
    }
}
