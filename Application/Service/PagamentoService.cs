using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interface;

namespace Application.Service;

public class PagamentoService : IPagamentoService
{
    private readonly IPagamentoRepository _pagamentoRepo;
    private readonly IReservaRepository _reservaRepo;
    private readonly IEventoRepository _eventoRepo;

    public PagamentoService(
        IPagamentoRepository pagamentoRepo,
        IReservaRepository reservaRepo,
        IEventoRepository eventoRepo)
    {
        _pagamentoRepo = pagamentoRepo;
        _reservaRepo = reservaRepo;
        _eventoRepo = eventoRepo;
    }

    /// <summary>
    /// Spec 200: usuarioId (Guid) em vez de usuarioCpf.
    /// </summary>
    public async Task<CheckoutResponseDTO> ConfirmarCheckout(
        Guid reservaId, Guid usuarioId, string metodo, CancellationToken ct)
    {
        // 1. Buscar reserva
        var reserva = await _reservaRepo.BuscarPorId(reservaId, ct);
        if (reserva == null)
            throw new DomainException("Reserva não encontrada.");

        // 2. Verificar propriedade (Spec 200: UsuarioId em vez de UsuarioCpf)
        if (reserva.UsuarioId != usuarioId)
            throw new UnauthorizedAccessException("Esta reserva não pertence ao usuário logado.");

        // 3. Verificar se já foi paga
        if (reserva.Pago)
            throw new DomainException("Reserva já foi paga.");

        // 4. Verificar se evento já começou
        var evento = await _eventoRepo.GetByIdAsync(reserva.EventoId);
        if (evento == null)
            throw new DomainException("Evento não encontrado.");

        if (evento.DataEvento <= DateTime.UtcNow)
            throw new DomainException("Não é possível pagar. O evento já começou.");

        // 5. Criar entidade Pagamento
        var pagamento = new Pagamento(reservaId, reserva.ValorFinalPago, metodo);

        // 6. Transação atômica
        await _pagamentoRepo.CriarComTransacao(pagamento, reserva, evento, ct);

        return new CheckoutResponseDTO(
            pagamento.Id,
            reservaId,
            pagamento.ValorPago,
            pagamento.Status.ToString(),
            pagamento.DataPagamento,
            "Pagamento confirmado com sucesso!"
        );
    }

    /// <summary>
    /// Spec 200: Reserva.UsuarioId em vez de UsuarioCpf.
    /// </summary>
    public async Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct)
    {
        var pagamentos = await _pagamentoRepo.ListarTodosAdmin(ct);

        return pagamentos.Select(p => new PagamentoAdminDTO(
            p.Id,
            p.ReservaId,
            p.Reserva?.UsuarioId.ToString() ?? "",
            p.Reserva?.EventoId.ToString() ?? "",
            p.ValorPago,
            p.Status.ToString(),
            p.Metodo,
            p.DataPagamento
        ));
    }
}