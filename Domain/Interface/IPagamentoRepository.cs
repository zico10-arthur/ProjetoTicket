using Domain.Entities;

namespace Domain.Interface;

public interface IPagamentoRepository
{
    Task CriarComTransacao(Pagamento pagamento, Reserva reserva, Evento evento, CancellationToken ct);
    Task<List<Pagamento>> ListarTodosAdmin(CancellationToken ct);
    Task<Pagamento?> BuscarPorReservaId(Guid reservaId, CancellationToken ct);
}
