using Domain.Entities;
using Domain.DTOs;

namespace Domain.Interface;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync();
    /// <summary>Spec 200: Busca eventos por vendedor (Guid).</summary>
    Task<IEnumerable<Evento>> GetAllByVendedorAsync(Guid vendedorId);
    Task<Evento?> GetByIdAsync(Guid id);

    Task CriarEventoCompletoAsync(Evento evento);

    Task UpdateAsync(Guid id, Evento evento);

    /// <summary>
    /// Retorna dados agregados sobre o impacto do cancelamento.
    /// </summary>
    Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, CancellationToken ct);

    /// <summary>
    /// Executa cancelamento do evento em transação atômica.
    /// Marca Evento.Cancelado, Ingressos.Status=3, Reservas.Reembolsada,
    /// ItensReserva.Reembolsado, Pagamentos.Status=3.
    /// </summary>
    Task CancelarComTransacao(Guid eventoId, bool gratuito, CancellationToken ct);
}
