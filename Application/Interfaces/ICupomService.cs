using Application.DTOs;

namespace Application.Interfaces;

public interface ICupomService
{
    Task CadastrarCupom(CadastrarCupomDTO dto, CancellationToken ct, Guid AdminLogado);

    Task DeletarCupom(Guid AdminLogado, string codigo, CancellationToken ct);

    Task AlterarValorMinimo(Guid AdminLogado, string codigo, decimal novoValor, CancellationToken ct);

    Task AlterarDataVencimento(Guid AdminLogado, string codigo, DateTime novaData, CancellationToken ct);

    Task AlternarStatusCupom(Guid AdminLogado, string codigo, CancellationToken ct);

    Task AlterarDesconto(Guid AdminLogado, string codigo, decimal novoDesconto, CancellationToken ct);

    Task<IEnumerable<Cupom>> ListarTodosCupons(Guid AdminLogado, CancellationToken ct);

    Task<IEnumerable<Cupom>> ListarCuponsValidos(CancellationToken ct);
}
