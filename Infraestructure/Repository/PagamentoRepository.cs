using Dapper;
using Domain.Entities;
using Domain.Interface;
using Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Infraestructure.Repository;

public class PagamentoRepository : IPagamentoRepository
{
    private readonly ConnectionFactory _factory;

    public PagamentoRepository(ConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task CriarComTransacao(
        Pagamento pagamento, Reserva reserva, Evento evento, CancellationToken ct)
    {
        using var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);
        using var transacao = conn.BeginTransaction();

        try
        {
            // 1. Inserir pagamento
            const string sqlPagamento = @"
                INSERT INTO Pagamentos (Id, ReservaId, ValorPago, Status, Metodo, DataPagamento, DataCriacao)
                VALUES (@Id, @ReservaId, @ValorPago, @Status, @Metodo, @DataPagamento, @DataCriacao)";

            await conn.ExecuteAsync(new CommandDefinition(sqlPagamento, new
            {
                pagamento.Id,
                pagamento.ReservaId,
                pagamento.ValorPago,
                Status = (int)pagamento.Status,
                pagamento.Metodo,
                pagamento.DataPagamento,
                pagamento.DataCriacao
            }, transacao, cancellationToken: ct));

            // 2. Marcar reserva como paga
            const string sqlReserva = "UPDATE Reservas SET Pago = 1 WHERE Id = @ReservaId";
            await conn.ExecuteAsync(new CommandDefinition(sqlReserva,
                new { ReservaId = reserva.Id }, transacao, cancellationToken: ct));

            // 3. Atualizar status dos ingressos para Vendido (Status=2)
            //    Para eventos tipo Palestra, ItensReserva.IngressoId pode ser NULL,
            //    então o sub-SELECT não retorna linhas — seguro.
            const string sqlIngressos = @"
                UPDATE Ingressos SET Status = 2
                WHERE Id IN (SELECT IngressoId FROM ItensReserva WHERE ReservaId = @ReservaId)";

            await conn.ExecuteAsync(new CommandDefinition(sqlIngressos,
                new { ReservaId = reserva.Id }, transacao, cancellationToken: ct));

            await transacao.CommitAsync(ct);
        }
        catch
        {
            await transacao.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<Pagamento>> ListarTodosAdmin(CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT p.*, r.UsuarioCpf, r.EventoId
            FROM Pagamentos p
            INNER JOIN Reservas r ON p.ReservaId = r.Id
            ORDER BY p.DataPagamento DESC";

        var pagamentos = await connection.QueryAsync<Pagamento, Reserva, Pagamento>(
            new CommandDefinition(sql, cancellationToken: ct),
            (pagamento, reserva) =>
            {
                typeof(Pagamento).GetProperty("Reserva")?.SetValue(pagamento, reserva);
                return pagamento;
            },
            splitOn: "UsuarioCpf"
        );

        return pagamentos.ToList();
    }

    public async Task<Pagamento?> BuscarPorReservaId(Guid reservaId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "SELECT * FROM Pagamentos WHERE ReservaId = @ReservaId";

        return await connection.QuerySingleOrDefaultAsync<Pagamento>(
            new CommandDefinition(sql, new { ReservaId = reservaId }, cancellationToken: ct)
        );
    }
}
