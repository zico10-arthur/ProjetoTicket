using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Application.Exceptions;
using Domain.Entities;

namespace Application.Service;

public class CupomService : ICupomService
{
    private readonly ICupomRepository _repositoryCupom;
    private readonly IUsuarioRepository _repositoryUsuario;

    public CupomService(ICupomRepository repositoryCupom, IUsuarioRepository repositoryUsuario)
    {
        _repositoryUsuario = repositoryUsuario;
        _repositoryCupom = repositoryCupom;
    }

    public async Task CadastrarCupom(CadastrarCupomDTO dto, CancellationToken ct, Guid AdminLogado)
    {
        Cupom? cupomBuscado = await _repositoryCupom.BuscarCupomCodigo(dto.Codigo, ct);
        if (cupomBuscado != null) throw new CupomCadastrado();

        Cupom novoCupom = Cupom.Criar(dto.Codigo, dto.PorcentagemDesconto, dto.ValorMinimo, dto.DataExpiracao);
        await Task.Run(() => _repositoryCupom.CadastrarCupom(novoCupom), ct);
    }

    public async Task DeletarCupom(Guid AdminLogado, string codigo, CancellationToken ct)
    {
        Cupom? cupomBuscado = await _repositoryCupom.BuscarCupomCodigo(codigo, ct);
        if (cupomBuscado == null) throw new CupomInexistente();

        await _repositoryCupom.DeletarCupom(codigo, ct);
    }

    public async Task AlterarValorMinimo(Guid AdminLogado, string codigo, decimal novoValor, CancellationToken ct)
    {
        Cupom? cupomBuscado = await _repositoryCupom.BuscarCupomCodigo(codigo, ct);
        if (cupomBuscado == null) throw new CupomInexistente();

        if (novoValor <= 0) throw new ValorMinimoInvalido();

        await _repositoryCupom.AlterarValorMinimo(cupomBuscado, novoValor, ct);
    }

    public async Task AlterarDataVencimento(Guid AdminLogado, string codigo, DateTime novaData, CancellationToken ct)
    {
        Cupom? cupomBuscado = await _repositoryCupom.BuscarCupomCodigo(codigo, ct);
        if (cupomBuscado == null) throw new CupomInexistente();

        if (novaData < DateTime.Now) throw new DataExpiracaoInvalida();

        await _repositoryCupom.AlterarDataVencimento(cupomBuscado, novaData, ct);
    }

    public async Task AlternarStatusCupom(Guid AdminLogado, string codigo, CancellationToken ct)
    {
        Cupom? cupomBuscado = await _repositoryCupom.BuscarCupomCodigo(codigo, ct);
        if (cupomBuscado == null) throw new CupomInexistente();

        cupomBuscado.AlternarStatus();
        await _repositoryCupom.AlterarStatusCupom(cupomBuscado.Codigo, cupomBuscado.Ativo, ct);
    }

    public async Task AlterarDesconto(Guid AdminLogado, string codigo, decimal novoDesconto, CancellationToken ct)
    {
        Cupom? cupomBuscado = await _repositoryCupom.BuscarCupomCodigo(codigo, ct);
        if (cupomBuscado == null) throw new CupomInexistente();

        if (novoDesconto <= 0 || novoDesconto > 100) throw new DescontoInvalido();

        await _repositoryCupom.AlterarDesconto(cupomBuscado.Codigo, novoDesconto, ct);
    }

    public async Task<IEnumerable<Cupom>> ListarTodosCupons(Guid AdminLogado, CancellationToken ct)
    {
        return await _repositoryCupom.ListarTodosCupons(ct);
    }

    public async Task<IEnumerable<Cupom>> ListarCuponsValidos(CancellationToken ct)
    {
        return await _repositoryCupom.ListarCuponsValidos(ct);
    }
}
