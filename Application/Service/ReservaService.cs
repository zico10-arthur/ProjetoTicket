using Application.DTOs;
using Application.Interfaces;
using Application.Exceptions;
using Domain.Entities;
using Domain.Interface;
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

    public async Task<Guid> FazerReserva(string UsuarioCpf, ReservarDTO dto, CancellationToken ct)
    {
        Ingresso? ingresso = await _repositoryIngresso.BuscarIngressoId(dto.IngressoId, ct);
        if (ingresso == null) throw new IngressoInexistente();

        if (ingresso.Status == 2) throw new IngressoIndisponivel();

        Evento? evento = await _repositoryEvento.GetByIdAsync(dto.EventoId);
        if (evento == null) throw new EventoInexistente();

        Cupom? cupom = null;
        if (!string.IsNullOrWhiteSpace(dto.CupomUtilizado))
        {
            cupom = await _repositoryCupom.BuscarCupomCodigo(dto.CupomUtilizado, ct);
            if (cupom == null) throw new CupomInexistente();
        }

        bool jaPossuiReserva = await _repositoryReserva.ReservaExistenteParaUsuario(UsuarioCpf, dto.EventoId, ct);
        if (jaPossuiReserva) throw new ReservaDuplicada();

        Reserva novaReserva = Reserva.Criar(UsuarioCpf, evento, ingresso, cupom);

        bool travouIngresso = await _repositoryIngresso.BloquearIngressoTemporariamente(ingresso.Id, ct);
    
        if (!travouIngresso)
            throw new Exception("Erro ao segurar o assento. Tente novamente.");

        await _repositoryReserva.CadastrarReserva(novaReserva, ct);

        return novaReserva.Id;
    }

    public async Task<IEnumerable<Reserva>> ListarReservasPorCpf(string cpf, CancellationToken ct)
    {
        return await _repositoryReserva.ListarPorCpf(cpf, ct);
    }

    public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarMinhasReservas(string cpf, CancellationToken ct)
    {
        return await _repositoryReserva.ListarReservasDetalhadasPorCpf(cpf, ct);
    }

}