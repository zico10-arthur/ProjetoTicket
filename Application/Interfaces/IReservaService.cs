using Application.DTOs;
using Domain.Entities;
using Domain.DTOs;

namespace Application.Interfaces;

public interface IReservaService
{
    Task<Guid> FazerReserva(string UsuarioCpf, ReservarDTO dto, CancellationToken ct);
    Task<IEnumerable<Reserva>> ListarReservasPorCpf(string cpf, CancellationToken ct);
    Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct);
}