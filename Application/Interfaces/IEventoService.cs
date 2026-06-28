using Application.DTOs;
using Domain.DTOs;

namespace Application.Interfaces;

public interface IEventoService
{
    Task<IEnumerable<EventoResponseDTO>> GetAllAsync();
    
    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task<IEnumerable<EventoResponseDTO>> GetAllByVendedorAsync(Guid vendedorId);
    
    Task<EventoResponseDTO?> GetByIdAsync(Guid id);

    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, Guid vendedorId);
    
    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task UpdateAsync(Guid id, EventoRequestDTO dto, Guid vendedorId);
    
    /// <summary>
    /// Spec 50: Consulta o impacto do cancelamento antes de executá-lo.
    /// Spec 200: vendedorId como Guid.
    /// </summary>
    Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, Guid vendedorId, bool isAdmin, CancellationToken ct);

    /// <summary>
    /// Spec 50: Cancela o evento com reembolso obrigatório dos ingressos vendidos.
    /// Spec 200: vendedorId como Guid.
    /// </summary>
    Task CancelarEvento(Guid eventoId, Guid vendedorId, bool isAdmin, CancellationToken ct);
}