using Domain.Interface;
using Application.Interfaces;
using AutoMapper;
using Infraestructure.Repository;
using Infrastructure.Interfaces;
using Application.DTOs;
namespace Application.Service;


public class IngressoService : IIngressoService
{
    private readonly IIngressoRepository _repository;
    private readonly IMapper _mapper;

    private readonly IEventoRepository _ERepository;

    public IngressoService(IIngressoRepository repository, IMapper mapper, IEventoRepository eRepository)
    {
        _repository = repository;
        _mapper = mapper;
        _ERepository = eRepository;
    }

    public async Task<IEnumerable<IngressoResponseDTO>> ListarIngressosDoEventoAsync(Guid eventoId, CancellationToken ct)
{
    var existe = await _ERepository.GetByIdAsync(eventoId);

    if (existe == null)
    throw new KeyNotFoundException("Evento não encontrado");

    var ingressos = await _repository.ListarPorEventoIdAsync(eventoId, ct);

    return _mapper.Map<IEnumerable<IngressoResponseDTO>>(ingressos);
}

}
