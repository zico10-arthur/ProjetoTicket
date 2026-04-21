using Domain.Entities;

namespace Domain.Interface;

public interface IEventoRepository
{
    Task<IEnumerable<Evento>> GetAllAsync();
    Task<IEnumerable<Evento>> GetAllByVendedorAsync(string vendedorCpf);
    Task<Evento?> GetByIdAsync(Guid id);

    Task CriarEventoCompletoAsync(Evento evento);

    Task UpdateAsync(Guid id, Evento evento);
    Task DeleteAsync(Guid id);

}
