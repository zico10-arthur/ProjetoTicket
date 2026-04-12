using Dapper;
using Infrastructure.Database;
using Domain.Entities;
using Domain.Interface;
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

        return await conn.QueryAsync<Evento>("SELECT * FROM dbo.Eventos");

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
                INSERT INTO dbo.Eventos (id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao)
                VALUES (@Id, @Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao)";

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
            "UPDATE dbo.Eventos SET Nome = @Nome, CapacidadeTotal = @CapacidadeTotal, DataEvento = @DataEvento, PrecoPadrao = @PrecoPadrao WHERE id = @Id",
            new { id, evento.Nome, evento.CapacidadeTotal, evento.DataEvento, evento.PrecoPadrao });

    }


    public async Task DeleteAsync(Guid id)
    {
        
        using var conn = (SqlConnection)_connectionFactory.CreateConnection();
        await conn.OpenAsync();
        using var transacao = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync("DELETE FROM dbo.Ingressos WHERE EventoId = @Id", new { Id = id }, transacao);
            await conn.ExecuteAsync("DELETE FROM dbo.Eventos WHERE id = @Id", new { Id = id }, transacao);
            await transacao.CommitAsync();
        }
        catch
        {
            await transacao.RollbackAsync();
            throw;
        }
    }

}
