using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Interfaces;
using Domain.Entities;
using AutoMapper;

namespace Application.Services;

public class EventoService
{
 
    private readonly IEventoRepository _eventoRepository;
    private readonly IMapper _mapper;

    public EventoService(IEventoRepository eventoRepository, IMapper mapper)
    {
        _eventoRepository = eventoRepository;
        _mapper = mapper;
    }


    public async Task<IEnumerable<EventoResponseDTO>> GetAllAsync()
    {
        
        var eventos = await _eventoRepository.GetAllAsync();

        //aqui entra a lógica de negócios

        return _mapper.Map<IEnumerable<EventoResponseDTO>>(eventos);

    }


    











}
