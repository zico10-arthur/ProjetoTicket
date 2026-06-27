using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Interface;
using Infraestructure.Email;

namespace Application.Service;

public class PagamentoService : IPagamentoService
{
    private readonly IPagamentoRepository _pagamentoRepo;
    private readonly IReservaRepository _reservaRepo;
    private readonly IEventoRepository _eventoRepo;
    private readonly Domain.Interface.IEmailSender _emailSender;
    private readonly IUsuarioRepository _usuarioRepo;

    public PagamentoService(
        IPagamentoRepository pagamentoRepo,
        IReservaRepository reservaRepo,
        IEventoRepository eventoRepo,
        Domain.Interface.IEmailSender emailSender,
        IUsuarioRepository usuarioRepo)
    {
        _pagamentoRepo = pagamentoRepo;
        _reservaRepo = reservaRepo;
        _eventoRepo = eventoRepo;
        _emailSender = emailSender;
        _usuarioRepo = usuarioRepo;
    }

    public async Task<CheckoutResponseDTO> ConfirmarCheckout(
        Guid reservaId, string usuarioCpf, string metodo, CancellationToken ct)
    {
        // 1. Buscar reserva
        var reserva = await _reservaRepo.BuscarPorId(reservaId, ct);
        if (reserva == null)
            throw new DomainException("Reserva não encontrada.");

        // 2. Verificar propriedade
        if (reserva.UsuarioCpf != usuarioCpf)
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

        // Spec 180: Enfileirar e-mail de confirmação de pagamento
        var comprador = await _usuarioRepo.BuscarCpf(reserva.UsuarioCpf, ct);
        var destinatario = comprador?.Email ?? reserva.UsuarioCpf;
        var emailPagamento = EmailTemplates.PagamentoConfirmado(
            destinatario, evento.Nome, pagamento.ValorPago, metodo, pagamento.DataPagamento);
        await _emailSender.EnfileirarAsync(emailPagamento, ct);

        return new CheckoutResponseDTO(
            pagamento.Id,
            reservaId,
            pagamento.ValorPago,
            pagamento.Status.ToString(),
            pagamento.DataPagamento,
            "Pagamento confirmado com sucesso!"
        );
    }

    public async Task<IEnumerable<PagamentoAdminDTO>> ListarTodosAdmin(CancellationToken ct)
    {
        var pagamentos = await _pagamentoRepo.ListarTodosAdmin(ct);

        return pagamentos.Select(p => new PagamentoAdminDTO(
            p.Id,
            p.ReservaId,
            p.Reserva?.UsuarioCpf ?? "",
            p.Reserva?.EventoId.ToString() ?? "",
            p.ValorPago,
            p.Status.ToString(),
            p.Metodo,
            p.DataPagamento
        ));
    }
}
