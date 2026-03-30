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

        const string sql = @"INSERT INTO Cupons(Codigo, PorcentagemDesconto, ValorMinimo, DataExpiracao, Ativo)
        VALUES (@Codigo, @PorcentagemDesconto, @ValorMinimo, @DataExpiracao, @Ativo)";

        connection.Execute(sql, new
        {
            Codigo = cupom.Codigo, 
            PorcentagemDesconto = cupom.PorcentagemDesconto, 
            ValorMinimo = cupom.ValorMinimo,
            DataExpiracao = cupom.DataExpiracao,
            Ativo = cupom.Ativo
        });
    }

    public async Task DeletarCupom(string codigo, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "DELETE FROM Cupons WHERE Codigo = @Codigo";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Codigo = codigo.ToUpper().Trim() },
                cancellationToken: ct
            )
        );
    }


    public async Task<Cupom?> BuscarCupomCodigo(string codigo, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"SELECT Codigo, PorcentagemDesconto, ValorMinimo, DataExpiracao, Ativo
                         FROM Cupons
                         WHERE Codigo = @Codigo";

        return await connection.QueryFirstOrDefaultAsync<Cupom>(
        new CommandDefinition(
            sql,
            new {Codigo = codigo.ToUpper().Trim()},
            cancellationToken: ct
        )
        );
    }

    public async Task AlterarValorMinimo(Cupom cupom, decimal novoValor, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
        UPDATE Cupons 
        SET ValorMinimo = @NovoValor 
        WHERE Codigo = @Codigo";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { 
                    NovoValor = novoValor,
                    Codigo = cupom.Codigo
                },
                cancellationToken: ct
            )
        );
    }

    public async Task AlterarDataVencimento(Cupom cupom, DateTime novaData, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
        UPDATE Cupons 
        SET DataExpiracao = @NovaData 
        WHERE Codigo = @Codigo";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { 
                    NovaData = novaData,
                    Codigo = cupom.Codigo
                },
                cancellationToken: ct
            )
        );
    }

    public async Task AlterarStatusCupom(string codigo, bool novoStatus, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            UPDATE Cupons 
            SET Ativo = @Ativo 
            WHERE Codigo = @Codigo";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { Ativo = novoStatus, Codigo = codigo },
                cancellationToken: ct
            )
        );
    }


    public async Task AlterarDesconto(string codigo, decimal novoDesconto, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
        UPDATE Cupons 
        SET PorcentagemDesconto = @NovoDesconto 
        WHERE Codigo = @Codigo";

        await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new { 
                    novoDesconto = novoDesconto,
                    Codigo = codigo
                },
                cancellationToken: ct
            )
        );
    }


    public async Task<IEnumerable<Cupom>> ListarTodosCupons(CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT
                Codigo, 
                PorcentagemDesconto, 
                ValorMinimo, 
                DataExpiracao, 
                CAST(CASE WHEN GETDATE() > DataExpiracao THEN 0 ELSE Ativo END AS BIT) AS Ativo
                
            FROM Cupons";

        return await connection.QueryAsync<Cupom>(sql);
    }

    public async Task<IEnumerable<Cupom>> ListarCuponsValidos(CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT Codigo, PorcentagemDesconto, ValorMinimo, DataExpiracao, Ativo 
            FROM Cupons
            WHERE Ativo = 1 AND DataExpiracao >= GETDATE()";

        return await connection.QueryAsync<Cupom>(sql);
    }

}
