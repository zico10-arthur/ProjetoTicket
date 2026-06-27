using Domain.Entities;
using Domain.DTOs;

namespace Domain.Interface;

public interface IReservaRepository
{
    Task CadastrarReservaComItens(Reserva reserva, CancellationToken ct);
    Task<Reserva?> BuscarPorId(Guid id, CancellationToken ct);
    Task<IEnumerable<Reserva>> ListarPorCpf(string cpf, CancellationToken ct);
    Task<bool> ReservaExistenteParaCpfNoEvento(string cpf, Guid eventoId, CancellationToken ct);
    Task DeletarReservasNaoPagasExpiradas(int minutosExpiracao, CancellationToken ct);
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct);
    Task<IEnumerable<ReservaAdminDTO>> ListarTodasDetalhadasAdmin(CancellationToken ct);
    Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedor(string vendedorCpf, CancellationToken ct);

    /// <summary>
    /// Spec 40: Executa cancelamento em transação atômica.
    /// UPDATE Reservas.Reembolsada = 1,
    /// UPDATE ItensReserva.Reembolsado = 1,
    /// UPDATE Ingressos.Status = 0, DataBloqueio = NULL,
    /// UPDATE Pagamentos.Status = 2 (Reembolsado) se Status = 1 (Confirmado).
    /// </summary>
    Task CancelarComTransacao(Guid reservaId, CancellationToken ct);
}
