using Application.DTOs;
using Domain.Entities;
using Domain.DTOs;

namespace Application.Interfaces;

public interface IReservaService
{
    /// <summary>Spec 200: usuarioId como Guid.</summary>
    Task<Guid> FazerReserva(Guid usuarioId, ReservarDTO dto, CancellationToken ct);
    
    /// <summary>Spec 200: usuarioId como Guid.</summary>
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(Guid usuarioId, CancellationToken ct);
    
    /// <summary>Spec 200: vendedorId como Guid.</summary>
    Task<IEnumerable<ReservaVendedorDTO>> ListarVendasDoVendedor(Guid vendedorId, CancellationToken ct);

    /// <summary>
    /// Spec 40: Cancela uma reserva própria. Valida propriedade, data do evento,
    /// e reembolso prévio. Executa transação atômica e enfileira e-mail de reembolso.
    /// Spec 200: usuarioId como Guid.
    /// </summary>
    Task CancelarReserva(Guid reservaId, Guid usuarioId, CancellationToken ct);
}