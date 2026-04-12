using Domain.Entities;

namespace Domain.Interface;

public interface IIngressoRepository
{
    Task<IEnumerable<Ingresso>> ListarPorEventoIdAsync(Guid eventoId, CancellationToken ct);

    Task<Ingresso?> BuscarIngressoId(Guid ingressoId, CancellationToken ct);

    Task<bool> BloquearIngressoTemporariamente(Guid ingressoId, CancellationToken ct);

    Task LiberarAssentosExpirados(int minutosExpiracao, CancellationToken ct);
}
