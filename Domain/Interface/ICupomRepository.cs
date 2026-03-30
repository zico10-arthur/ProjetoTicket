namespace Domain.Interface;

public interface ICupomRepository
{
    void CadastrarCupom(Cupom cupom);

    Task DeletarCupom(string codigo, CancellationToken ct);

    Task<Cupom?> BuscarCupomCodigo(string codigo, CancellationToken ct);

    Task AlterarValorMinimo(Cupom cupom, decimal novoValor, CancellationToken ct);

    Task AlterarDataVencimento(Cupom cupom, DateTime novaData, CancellationToken ct);

    Task AlterarStatusCupom(string codigo, bool novoStatus, CancellationToken ct);

    Task AlterarDesconto(string codigo, decimal novoDesconto, CancellationToken ct);
    
    Task<IEnumerable<Cupom>> ListarTodosCupons(CancellationToken ct);

    Task<IEnumerable<Cupom>> ListarCuponsValidos(CancellationToken ct);

}
