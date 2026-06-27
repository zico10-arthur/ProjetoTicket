using Domain.Entities;
using Domain.DTOs;

namespace Domain.Interface;

public interface IReservaRepository
{
    Task CadastrarReservaComItens(Reserva reserva, CancellationToken ct);
    Task<Reserva?> BuscarPorId(Guid id, CancellationToken ct);
    Task<IEnumerable<Reserva>> ListarPorUsuarioId(Guid usuarioId, CancellationToken ct);
    Task<bool> ReservaExistenteParaCpfNoEvento(string cpf, Guid eventoId, CancellationToken ct);
    Task DeletarReservasNaoPagasExpiradas(int minutosExpiracao, CancellationToken ct);
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorUsuarioId(Guid usuarioId, CancellationToken ct);
    Task<IEnumerable<ReservaAdminDTO>> ListarTodasDetalhadasAdmin(CancellationToken ct);
    Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedorId(Guid vendedorId, CancellationToken ct);
}
