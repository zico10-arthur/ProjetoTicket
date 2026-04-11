using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Interfaces;
using Domain.Entities;
using AutoMapper;

namespace Application.Services;

public class EventoService : IEventoService
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

        if (!eventos.Any())
        {
            
            throw new KeyNotFoundException("Nenhum evento encontrado");

        }

        return _mapper.Map<IEnumerable<EventoResponseDTO>>(eventos);

    }


    public async Task<EventoResponseDTO?> GetByIdAsync(Guid id)
    {
        
        var evento = await _eventoRepository.GetByIdAsync(id);

        if(evento is null)
        {
            
            throw new KeyNotFoundException($"Evento com ID {id} não encontrado");

        }

        return _mapper.Map<EventoResponseDTO?>(evento);

    }


    private static void ValidarEvento(EventoRequestDTO eventoDto)
    {
        if (eventoDto.DataEvento <= DateTime.UtcNow)
            throw new ArgumentException("A data do evento deve ser futura.");
    }

    public async Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, string usuarioCpf)
{
    if (eventoDto == null)
        throw new ArgumentNullException(nameof(eventoDto));

    var evento = _mapper.Map<Evento>(eventoDto);
    evento.DefinirUsuarioCpf(usuarioCpf);

    if (evento.CapacidadeTotal <= 0)
        throw new InvalidOperationException("Capacidade deve ser maior que zero");

    evento.GerarLoteIngressos(evento.CapacidadeTotal);

    await _eventoRepository.CriarEventoCompletoAsync(evento);

    return evento.id;
}


    public async Task UpdateAsync(Guid id, EventoRequestDTO eventoDto)
    {
        
        var evento = await _eventoRepository.GetByIdAsync(id);


        if(evento is null)
        {
            
            throw new KeyNotFoundException($"Evento {id} não encontrado");

        }

        ValidarEvento(eventoDto);

        var eventoAtualizado = _mapper.Map<Evento>(eventoDto);
        await _eventoRepository.UpdateAsync(id, eventoAtualizado);

    }


    public async Task DeleteAsync(Guid id)
    {

        var evento = await _eventoRepository.GetByIdAsync(id);


        if (evento is null)
        {

            throw new KeyNotFoundException($"Evento {id} não encontrado");

        }

        if (evento.DataEvento <= DateTime.UtcNow)
        {
            
            throw new InvalidOperationException("Não é possível deletar um evento que já ocorreu.");

        }


        await _eventoRepository.DeleteAsync(id);

    }


}
