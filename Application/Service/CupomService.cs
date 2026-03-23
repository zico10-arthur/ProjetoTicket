using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Application.Exceptions;

namespace Application.Service;

public class CupomService : ICupomService
{
    private readonly ICupomRepository _repository;

    public CupomService(ICupomRepository repository)
    {
        _repository = repository;
    }

    public async Task CadastrarCupom(CadastrarCupomDTO dto, CancellationToken ct)
    {
        Cupom? codigoBuscado = await _repository.BuscarCupomCodigo(dto.Codigo, ct);
        if (codigoBuscado != null) throw new CupomCadastrado();

        Cupom? novoCupom = Cupom.Criar(dto.Codigo, dto.PorcentagemDesconto, dto.ValorMinimo, dto.DataExpiracao);
        _repository.CadastrarCupom(novoCupom);
        //await _repository.UnitOfWork.SaveChangesAsync(ct);
    }
    

}
