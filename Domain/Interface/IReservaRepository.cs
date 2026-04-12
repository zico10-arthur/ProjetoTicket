using Domain.Entities;
using Domain.DTOs;

namespace Domain.Interface;

public interface IReservaRepository
{
    Task CadastrarReserva(Reserva reserva, CancellationToken ct);
    Task<IEnumerable<Reserva>> ListarPorCpf(string cpf, CancellationToken ct);
    Task<bool> ReservaExistenteParaUsuario(string cpf, Guid eventoId, CancellationToken ct);
    Task DeletarReservasNaoPagasExpiradas(int minutosExpiracao, CancellationToken ct);
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct);
    Task<IEnumerable<ReservaAdminDTO>> ListarTodasDetalhadasAdmin(CancellationToken ct);
}