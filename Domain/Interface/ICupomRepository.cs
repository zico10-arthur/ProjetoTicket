namespace Domain.Interface;

public interface ICupomRepository
{
    void CadastrarCupom(Cupom cupom);

    Task<Cupom?> BuscarCupomCodigo(string codigo, CancellationToken ct);

    Task<Cupom?> BuscarCupomIdEvento(Guid idEvento, CancellationToken ct);
}
