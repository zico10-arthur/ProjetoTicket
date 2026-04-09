using Domain.Entities;

namespace Domain.Interface;

public interface IIngressoRepository
{
    Task<IEnumerable<Ingresso>> ListarPorEventoIdAsync(Guid eventoId, CancellationToken ct);
}
