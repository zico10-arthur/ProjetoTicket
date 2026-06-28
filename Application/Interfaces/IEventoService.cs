using Application.DTOs;
using Domain.DTOs;

namespace Application.Interfaces;

public interface IEventoService
{
    Task<IEnumerable<EventoResponseDTO>> GetAllAsync();
    Task<IEnumerable<EventoResponseDTO>> GetAllByVendedorAsync(string vendedorCpf);
    Task<EventoResponseDTO?> GetByIdAsync(Guid id);

    Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, string vendedorCpf);
    Task UpdateAsync(Guid id, EventoRequestDTO dto, string vendedorCpf);

    /// <summary>
    /// Consulta o impacto do cancelamento antes de executá-lo.
    /// </summary>
    Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);

    /// <summary>
    /// Cancela o evento com reembolso obrigatório dos ingressos vendidos.
    /// Substitui o DeleteAsync anterior.
    /// </summary>
    Task CancelarEvento(Guid eventoId, string vendedorCpf, bool isAdmin, CancellationToken ct);
}
