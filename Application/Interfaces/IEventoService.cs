using Application.DTOs;

namespace Application.Interfaces;

public interface IEventoService
{
    Task<IEnumerable<EventoResponseDTO>> GetAllAsync();
    Task<EventoResponseDTO?> GetByIdAsync(Guid id);
    Task CreateAsync(EventoRequestDTO dto);
    Task UpdateAsync(Guid id, EventoRequestDTO dto);
    Task DeleteAsync(Guid id);
}
