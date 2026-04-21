using Application.DTOs;

namespace Application.Interfaces;

public interface IEventoService
{
    Task<IEnumerable<EventoResponseDTO>> GetAllAsync();
    Task<IEnumerable<EventoResponseDTO>> GetAllByVendedorAsync(string vendedorCpf);
    Task<EventoResponseDTO?> GetByIdAsync(Guid id);

    Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, string vendedorCpf);
    Task UpdateAsync(Guid id, EventoRequestDTO dto, string vendedorCpf);
    Task DeleteAsync(Guid id, string vendedorCpf, bool isAdmin = false);
}
