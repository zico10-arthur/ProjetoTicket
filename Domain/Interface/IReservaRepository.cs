using Domain.Entities;
using Domain.DTOs;

namespace Domain.Interface;

public interface IReservaRepository
{
    Task CadastrarReservaComItens(Reserva reserva, CancellationToken ct, bool eventoGratuito = false);
    Task<Reserva?> BuscarPorId(Guid id, CancellationToken ct);
    Task<IEnumerable<Reserva>> ListarPorCpf(string cpf, CancellationToken ct);
    Task<bool> ReservaExistenteParaCpfNoEvento(string cpf, Guid eventoId, CancellationToken ct);
    Task DeletarReservasNaoPagasExpiradas(int minutosExpiracao, CancellationToken ct);
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct);
    Task<IEnumerable<ReservaAdminDTO>> ListarTodasDetalhadasAdmin(CancellationToken ct);
}