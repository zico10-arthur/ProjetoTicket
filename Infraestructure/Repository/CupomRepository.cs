using Dapper;
using Domain.Exceptions;
using Infrastructure.Database;
using Domain.Interface;


namespace Infraestructure.Repository;

public class CupomRepository : ICupomRepository
{
    private readonly ConnectionFactory _factory;

    public CupomRepository(ConnectionFactory factory)
    {
        _factory = factory;
    }


    public void CadastrarCupom(Cupom cupom)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"INSERT INTO Cupons(IdEvento, Codigo, PorcentagemDesconto, ValorMinimo, DataExpiracao, Ativo)
        VALUES (@IdEvento, @Codigo, @PorcentagemDesconto, @ValorMinimo, @DataExpiracao, @Ativo)";

        connection.Execute(sql, new
        {
            IdEvento = cupom.IdEvento, 
            Codigo = cupom.Codigo, 
            PorcentagemDesconto = cupom.PorcentagemDesconto, 
            ValorMinimo = cupom.ValorMinimo,
            DataExpiracao = cupom.DataExpiracao,
            Ativo = cupom.Ativo
        });
    }


    public async Task<Cupom?> BuscarCupomCodigo(string codigo, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"SELECT IdEvento, Codigo, PorcentagemDesconto, ValorMinimo, DataExpiracao, Ativo
                         FROM Cupons
                         WHERE Codigo = @Codigo";

        return await connection.QueryFirstOrDefaultAsync<Cupom>(
        new CommandDefinition(
            sql,
            new {Codigo = codigo},
            cancellationToken: ct
        )
        );
    }


    public async Task<Cupom?> BuscarCupomIdEvento(Guid idEvento, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"SELECT IdEvento, Codigo, PorcentagemDesconto, ValorMinimo, DataExpiracao, Ativo
                         FROM Cupons
                         WHERE IdEvento = @IdEvento";

        return await connection.QueryFirstOrDefaultAsync<Cupom>(
        new CommandDefinition(
            sql,
            new {IdEvento = idEvento},
            cancellationToken: ct
        )
        );
    }
}
