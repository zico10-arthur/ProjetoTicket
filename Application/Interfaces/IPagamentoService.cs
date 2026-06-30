using Application.DTOs;

namespace Application.Interfaces;

public interface IPagamentoService
{
    /// <summary>Spec 200: usuarioId como Guid.</summary>
    Task<CheckoutResponseDTO> ConfirmarCheckout(Guid reservaId, Guid usuarioId, string metodo, CancellationToken ct);
    Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct);
}