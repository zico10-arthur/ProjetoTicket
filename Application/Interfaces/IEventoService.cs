using Application.DTOs;

namespace Application.Interfaces;

public interface IEventoService
{
    Task<IEnumerable<EventoResponseDTO>> GetAllAsync();
    
    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task<IEnumerable<EventoResponseDTO>> GetAllByVendedorAsync(Guid vendedorId);
    
    Task<EventoResponseDTO?> GetByIdAsync(Guid id);

    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task<Guid> CriarEventoAsync(EventoRequestDTO eventoDto, Guid vendedorId);
    
    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task UpdateAsync(Guid id, EventoRequestDTO dto, Guid vendedorId);
    
    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task DeleteAsync(Guid id, Guid vendedorId, bool isAdmin = false);
}