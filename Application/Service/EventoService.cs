using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Domain.Entities;
using Domain.DTOs;
using Domain.Exceptions;
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

        if (evento is null)
        {
            throw new KeyNotFoundException($"Evento com ID {id} não encontrado");
        }

        return _mapper.Map<EventoResponseDTO?>(evento);
    }

    private static void ValidarEvento(EventoRequestDTO eventoDto)
    {
        if (eventoDto.DataEvento <= DateTime.UtcNow)
            throw new ArgumentException("A data do evento deve ser futura.");

        if (eventoDto.CapacidadeTotal <= 0)
            throw new ArgumentException("A capacidade deve ser maior que zero.");

        if (eventoDto.Tipo != 0 && eventoDto.Tipo != 1)
            throw new ArgumentException("Tipo deve ser 0 (Teatro) ou 1 (Palestra).");
    }

    /// <summary>
    /// Spec 200: vendedorId (Guid) em vez de vendedorCpf.
    /// </summary>
    public async Task<IEnumerable<EventoResponseDTO>> GetAllByVendedorAsync(Guid vendedorId)
    {
        var eventos = await _eventoRepository.GetAllByVendedorAsync(vendedorId);
        return _mapper.Map<IEnumerable<EventoResponseDTO>>(eventos);
    }

    /// <summary>
    /// Spec 200: vendedorId (Guid) em vez de vendedorCpf.
    /// </summary>
    public async Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, Guid vendedorId)
    {
        if (eventoDto == null)
            throw new ArgumentNullException(nameof(eventoDto));

        ValidarEvento(eventoDto);

        var evento = _mapper.Map<Evento>(eventoDto);
        // Spec 200: VendedorId setado via Guid, não via DTO
        typeof(Evento).GetProperty("VendedorId")?.SetValue(evento, vendedorId);

        evento.GerarLoteIngressos(evento.CapacidadeTotal);
        await _eventoRepository.CriarEventoCompletoAsync(evento);
        return evento.id;
    }

    /// <summary>
    /// Spec 200: vendedorId (Guid) em vez de vendedorCpf.
    /// </summary>
    public async Task UpdateAsync(Guid id, EventoRequestDTO eventoDto, Guid vendedorId)
    {
        var evento = await _eventoRepository.GetByIdAsync(id);
        if (evento is null) throw new KeyNotFoundException($"Evento {id} não encontrado");
        if (evento.VendedorId != vendedorId) throw new UnauthorizedAccessException("Você não tem permissão para editar este evento.");

        ValidarEvento(eventoDto);
        var eventoAtualizado = _mapper.Map<Evento>(eventoDto);
        await _eventoRepository.UpdateAsync(id, eventoAtualizado);
    }

    /// <summary>
    /// Spec 50: Consulta o impacto do cancelamento antes de executá-lo.
    /// Spec 200: vendedorId (Guid) em vez de vendedorCpf.
    /// </summary>
    public async Task<StatusCancelamentoDTO> ObterStatusCancelamento(
        Guid eventoId, Guid vendedorId, bool isAdmin, CancellationToken ct)
    {
        var evento = await _eventoRepository.GetByIdAsync(eventoId);
        if (evento == null)
            throw new KeyNotFoundException($"Evento {eventoId} não encontrado.");

        if (!isAdmin && evento.VendedorId != vendedorId)
            throw new UnauthorizedAccessException("Você não tem permissão para acessar este evento.");

        var status = await _eventoRepository.ObterStatusCancelamento(eventoId, ct);

        var mensagem = status.ReembolsoNecessario
            ? $"{status.TotalIngressosVendidos} ingressos vendidos. O cancelamento exigirá reembolso de R$ {status.ValorTotalReembolso:F2}. Deseja continuar?"
            : "Nenhum ingresso vendido. O cancelamento não exige reembolso.";

        return status with { Mensagem = mensagem };
    }

    /// <summary>
    /// Spec 50: Cancela o evento com reembolso obrigatório dos ingressos vendidos.
    /// Spec 200: vendedorId (Guid) em vez de vendedorCpf.
    /// </summary>
    public async Task CancelarEvento(Guid eventoId, Guid vendedorId, bool isAdmin, CancellationToken ct)
    {
        var evento = await _eventoRepository.GetByIdAsync(eventoId);
        if (evento == null)
            throw new KeyNotFoundException($"Evento {eventoId} não encontrado.");

        if (!isAdmin && evento.VendedorId != vendedorId)
            throw new UnauthorizedAccessException("Você não tem permissão para cancelar este evento.");

        if (evento.Cancelado)
            throw new DomainException("Evento já foi cancelado.");

        await _eventoRepository.CancelarComTransacao(eventoId, evento.Gratuito, ct);
    }

}