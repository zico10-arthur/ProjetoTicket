using Application.DTOs;

namespace Application.Interfaces;

public interface IIngressoService
{
    Task<IEnumerable<IngressoResponseDTO>> ListarIngressosDoEventoAsync(Guid eventoId, CancellationToken ct);
}
