using Domain.Entities;

namespace Domain.Interface;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync();
    /// <summary>Spec 200: Busca eventos por vendedor (Guid).</summary>
    Task<IEnumerable<Evento>> GetAllByVendedorAsync(Guid vendedorId);
    Task<Evento?> GetByIdAsync(Guid id);

    Task CriarEventoCompletoAsync(Evento evento);

    Task UpdateAsync(Guid id, Evento evento);
    Task DeleteAsync(Guid id);

}
