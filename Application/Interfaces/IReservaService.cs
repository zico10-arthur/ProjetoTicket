using Application.DTOs;
using Domain.Entities;
using Domain.DTOs;

namespace Application.Interfaces;

public interface IReservaService
{
    Task<Guid> FazerReserva(string UsuarioCpf, ReservarDTO dto, CancellationToken ct);
    Task<IEnumerable<Reserva>> ListarReservasPorCpf(string cpf, CancellationToken ct);
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct);
    Task<IEnumerable<ReservaVendedorDTO>> ListarVendasDoVendedor(string vendedorCpf, CancellationToken ct);

    /// <summary>
    /// Spec 40: Cancela uma reserva própria. Valida propriedade, data do evento,
    /// e reembolso prévio. Executa transação atômica e enfileira e-mail de reembolso.
    /// </summary>
    Task CancelarReserva(Guid reservaId, string usuarioCpf, CancellationToken ct);
}
