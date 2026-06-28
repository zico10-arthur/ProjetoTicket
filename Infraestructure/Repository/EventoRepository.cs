using Dapper;
using Infrastructure.Database;
using Domain.Entities;
using Domain.Interface;
using Domain.DTOs;
using Microsoft.Data.SqlClient;


namespace Infraestructure.Repository;

public class EventoRepository : IEventoRepository
{
    
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _connectionString;

    public EventoRepository(ConnectionFactory connectionFactory)
    {
        
        _connectionFactory = connectionFactory;

    }


    public async Task<IEnumerable<Evento>> GetAllAsync()
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<Evento>("SELECT * FROM dbo.Eventos WHERE Cancelado = 0");
    }

    public async Task<IEnumerable<Evento>> GetAllByVendedorAsync(string vendedorCpf)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.QueryAsync<Evento>("SELECT * FROM dbo.Eventos WHERE VendedorCpf = @VendedorCpf", new { VendedorCpf = vendedorCpf });
    }


    public async Task<Evento?> GetByIdAsync(Guid id)
    {
        
        using var conn = _connectionFactory.CreateConnection();

        return await conn.QueryFirstOrDefaultAsync<Evento>("SELECT * FROM dbo.Eventos WHERE id = @Id", new{Id = id});

    }


    public async Task CriarEventoCompletoAsync(Evento evento)
    {
        using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        await conn.OpenAsync();

       
        using var transacao = conn.BeginTransaction();

        try
        {
            const string sqlEvento = @"
                INSERT INTO dbo.Eventos (id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao, VendedorCpf, Tipo, Descricao, Local, Cancelado)
                VALUES (@Id, @Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao, @VendedorCpf, @Tipo, @Descricao, @Local, @Cancelado)";

            await conn.ExecuteAsync(sqlEvento, evento, transacao);

           
            const string sqlIngresso = @"
                INSERT INTO dbo.Ingressos (Id, EventoId, Preco, Posicao, Setor, Status)
                VALUES (@Id, @EventoId, @Preco, @Posicao, @Setor, @Status)";

            await conn.ExecuteAsync(sqlIngresso, evento.Ingressos, transacao);

            await transacao.CommitAsync();
        }
        catch (Exception ex)
        {
            await transacao.RollbackAsync();
            throw new Exception($"Falha ao criar evento com ingressos: {ex.Message} | Inner: {ex.InnerException?.Message}", ex);
        }
    }


    public async Task UpdateAsync(Guid id, Evento evento)
    {
        
        using var conn = _connectionFactory.CreateConnection();

        await conn.ExecuteAsync(
            "UPDATE dbo.Eventos SET Nome = @Nome, CapacidadeTotal = @CapacidadeTotal, DataEvento = @DataEvento, PrecoPadrao = @PrecoPadrao, Tipo = @Tipo, Descricao = @Descricao, Local = @Local WHERE id = @Id",
            new { id, evento.Nome, evento.CapacidadeTotal, evento.DataEvento, evento.PrecoPadrao, evento.Tipo, evento.Descricao, evento.Local });

    }


    public async Task<StatusCancelamentoDTO> ObterStatusCancelamento(Guid eventoId, CancellationToken ct)
    {
        using var conn = _connectionFactory.CreateConnection();

        const string sql = @"
            SELECT
                e.Id              AS EventoId,
                e.Nome            AS Nome,
                CAST(CASE WHEN e.PrecoPadrao = 0 THEN 1 ELSE 0 END AS BIT) AS Gratuito,
                COUNT(i.Id)       AS TotalIngressosVendidos,
                COUNT(DISTINCT r.Id) AS TotalReservasAtivas,
                CAST(CASE WHEN e.PrecoPadrao > 0 AND COUNT(i.Id) > 0 THEN 1 ELSE 0 END AS BIT) AS ReembolsoNecessario,
                ISNULL(SUM(ir.PrecoUnitario), 0) AS ValorTotalReembolso
            FROM dbo.Eventos e
            LEFT JOIN dbo.Ingressos i ON i.EventoId = e.Id AND i.Status = 2
            LEFT JOIN dbo.ItensReserva ir ON ir.IngressoId = i.Id
            LEFT JOIN dbo.Reservas r ON r.EventoId = e.Id AND r.Reembolsada = 0
            WHERE e.Id = @eventoId
            GROUP BY e.Id, e.Nome, e.PrecoPadrao";

        return await conn.QuerySingleOrDefaultAsync<StatusCancelamentoDTO>(
            new CommandDefinition(sql, new { eventoId }, cancellationToken: ct));
    }


    public async Task CancelarComTransacao(Guid eventoId, bool gratuito, CancellationToken ct)
    {
        using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        await conn.OpenAsync(ct);
        using var transacao = conn.BeginTransaction();

        try
        {
            // 1. Marcar evento como cancelado
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Eventos SET Cancelado = 1 WHERE Id = @eventoId",
                new { eventoId }, transacao, cancellationToken: ct));

            // 2. Se evento pago: marcar ingressos como reembolsados
            if (!gratuito)
            {
                await conn.ExecuteAsync(new CommandDefinition(
                    "UPDATE dbo.Ingressos SET Status = 3 WHERE EventoId = @eventoId AND Status = 2",
                    new { eventoId }, transacao, cancellationToken: ct));
            }

            // 3. Marcar todas as reservas como reembolsadas
            await conn.ExecuteAsync(new CommandDefinition(
                "UPDATE dbo.Reservas SET Reembolsada = 1 WHERE EventoId = @eventoId AND Reembolsada = 0",
                new { eventoId }, transacao, cancellationToken: ct));

            // 4. Marcar todos os itens como reembolsados
            await conn.ExecuteAsync(new CommandDefinition(@"
                UPDATE dbo.ItensReserva SET Reembolsado = 1
                WHERE ReservaId IN (SELECT Id FROM dbo.Reservas WHERE EventoId = @eventoId)",
                new { eventoId }, transacao, cancellationToken: ct));

            // 5. Marcar pagamentos como reembolsados (se evento pago)
            if (!gratuito)
            {
                await conn.ExecuteAsync(new CommandDefinition(@"
                    UPDATE dbo.Pagamentos SET Status = 2
                    WHERE ReservaId IN (SELECT Id FROM dbo.Reservas WHERE EventoId = @eventoId)
                    AND Status = 1",
                    new { eventoId }, transacao, cancellationToken: ct));
            }

            await transacao.CommitAsync(ct);
        }
        catch
        {
            await transacao.RollbackAsync(ct);
            throw;
        }
    }

}
