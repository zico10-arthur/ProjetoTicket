using Application.DTOs;
using Application.Interfaces;
using Application.Exceptions;
using Domain.Entities;
using Domain.Interface;
using Domain.Validators;
using Domain.DTOs;

namespace Application.Service;

public class ReservaService : IReservaService
{
    private readonly IReservaRepository _repositoryReserva;
    private readonly IUsuarioRepository _repositoryUsuario;
    private readonly IEventoRepository _repositoryEvento;
    private readonly ICupomRepository _repositoryCupom;
    private readonly IIngressoRepository _repositoryIngresso;

    public ReservaService(
        IReservaRepository repositoryReserva, 
        IUsuarioRepository repositoryUsuario, 
        IEventoRepository repositoryEvento,
        ICupomRepository repositoryCupom,
        IIngressoRepository repositoryIngresso)
    {
        _repositoryUsuario = repositoryUsuario;
        _repositoryReserva = repositoryReserva;
        _repositoryEvento = repositoryEvento;
        _repositoryCupom = repositoryCupom;
        _repositoryIngresso = repositoryIngresso;
    }

    /// <summary>
    /// Spec 200: usuarioId (Guid) em vez de usuarioCpf.
    /// </summary>
    public async Task<Guid> FazerReserva(Guid usuarioId, ReservarDTO dto, CancellationToken ct)
    {
        // ST-04.4: Validar itens (1 a 4)
        if (dto.Itens == null || dto.Itens.Count == 0)
            throw new Domain.Exceptions.DomainException("É necessário pelo menos 1 participante.");

        // ST-04.4: Buscar evento
        Evento? evento = await _repositoryEvento.GetByIdAsync(dto.EventoId);
        if (evento == null) throw new EventoInexistente();

        // ST-04.8: Cupom não aplicável em evento gratuito
        Cupom? cupom = null;
        if (!string.IsNullOrWhiteSpace(dto.CupomCodigo))
        {
            if (evento.Gratuito)
                throw new CupomNaoAplicavelEventoGratuito();

            cupom = await _repositoryCupom.BuscarCupomCodigo(dto.CupomCodigo, ct);
            if (cupom == null) throw new CupomInexistente();
        }

        var itens = new List<ItemReserva>();

        foreach (var itemDto in dto.Itens)
        {
            // ST-04.5: Validar CPF de cada participante
            CpfValidator.Validar(itemDto.CpfParticipante);

            // Buscar ingresso
            Ingresso? ingresso = await _repositoryIngresso.BuscarIngressoId(itemDto.IngressoId, ct);
            if (ingresso == null) throw new IngressoInexistente();

            // Validar que o ingresso pertence ao evento
            if (ingresso.EventoId != dto.EventoId)
                throw new IngressoNaoPertenceAoEvento();

            // Validar que ingresso está livre
            if (ingresso.Status != 0)
                throw new AssentoNaoDisponivel();

            itens.Add(new ItemReserva(itemDto.CpfParticipante, itemDto.IngressoId, ingresso.Preco));
        }

        // ST-04.6: Anti-cambista — CPF não pode ter reserva ativa neste evento
        foreach (var item in itens)
        {
            bool jaReservado = await _repositoryReserva.ReservaExistenteParaCpfNoEvento(
                item.CpfParticipante, dto.EventoId, ct);
            if (jaReservado)
                throw new CpfJaReservadoNoEvento(item.CpfParticipante);
        }

        // ST-04.7: Criar reserva com cupom sobre valor total (Spec 200: usuarioId + vendedorId)
        Reserva novaReserva = Reserva.Criar(usuarioId, dto.EventoId, itens, cupom, evento.VendedorId);

        await _repositoryReserva.CadastrarReservaComItens(novaReserva, ct);

        return novaReserva.Id;
    }

    /// <summary>
    /// Spec 200: usuarioId (Guid).
    /// </summary>
    public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(Guid usuarioId, CancellationToken ct)
    {
        return await _repositoryReserva.ListarReservasDetalhadasPorUsuarioId(usuarioId, ct);
    }

    /// <summary>
    /// Spec 200: vendedorId (Guid).
    /// </summary>
    public async Task<IEnumerable<ReservaVendedorDTO>> ListarVendasDoVendedor(
        Guid vendedorId, CancellationToken ct)
    {
        return await _repositoryReserva.ListarReservasDetalhadasPorVendedorId(vendedorId, ct);
    }

}