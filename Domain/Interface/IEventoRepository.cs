using Domain.Entities;

namespace Infrastructure.Interfaces;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync();
    Task<Evento?> GetByIdAsync(Guid id);

    Task CriarEventoCompletoAsync(Evento evento);

    Task UpdateAsync(Guid id, Evento evento);
    Task DeleteAsync(Guid id);

}
