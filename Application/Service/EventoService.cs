using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Domain.Entities;
using AutoMapper;

namespace Application.Service;

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

    public async Task<IEnumerable<EventoResponseDTO>> GetAllByVendedorAsync(string vendedorCpf)
    {
        var eventos = await _eventoRepository.GetAllByVendedorAsync(vendedorCpf);
        return _mapper.Map<IEnumerable<EventoResponseDTO>>(eventos);
    }

    public async Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, string vendedorCpf)
    {
        if (eventoDto == null)
            throw new ArgumentNullException(nameof(eventoDto));

        eventoDto.VendedorCpf = vendedorCpf;
        var evento = _mapper.Map<Evento>(eventoDto);

        if (evento.CapacidadeTotal <= 0)
            throw new InvalidOperationException("Capacidade deve ser maior que zero");

        evento.GerarLoteIngressos(evento.CapacidadeTotal);
        await _eventoRepository.CriarEventoCompletoAsync(evento);
        return evento.id;
    }

    public async Task UpdateAsync(Guid id, EventoRequestDTO eventoDto, string vendedorCpf)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);
        if (evento is null) throw new KeyNotFoundException($"Evento {id} não encontrado");
        if (evento.VendedorCpf != vendedorCpf) throw new UnauthorizedAccessException("Você não tem permissão para editar este evento.");

        ValidarEvento(eventoDto);
        var eventoAtualizado = _mapper.Map<Evento>(eventoDto);
        await _eventoRepository.UpdateAsync(id, eventoAtualizado);
    }

    public async Task DeleteAsync(Guid id, string vendedorCpf, bool isAdmin = false)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);
        if (evento is null) throw new KeyNotFoundException($"Evento {id} não encontrado");
        if (!isAdmin && evento.VendedorCpf != vendedorCpf) throw new UnauthorizedAccessException("Você não tem permissão para excluir este evento.");

        await _eventoRepository.DeleteAsync(id);
    }


}
