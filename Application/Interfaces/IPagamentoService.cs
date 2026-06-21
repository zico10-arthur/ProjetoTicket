using Application.DTOs;

namespace Application.Interfaces;

public interface IPagamentoService
{
    Task<CheckoutResponseDTO> ConfirmarCheckout(Guid reservaId, string usuarioCpf, string metodo, CancellationToken ct);
    Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct);
}
