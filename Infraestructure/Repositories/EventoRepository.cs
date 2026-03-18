using Dapper;
using Infrastructure.Database;
using Infraestructure;
using Domain.Entities;
using Infrastructure.Interfaces;


namespace Infraestructure.Repositories;

public class EventoRepository : IEventoRepository
{
    
    private readonly ConnectionFactory _connectionFactory;

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


    public async Task CreateAsync(Evento evento)
    {
        
        using var conn = _connectionFactory.CreateConnection();

        await conn.ExecuteAsync(
            "INSERT INTO dbo.Eventos(id, Nome, CapacidadeTotal, DataEvento, PrecoPadrao) VALUES(@Id, @Nome, @CapacidadeTotal, @DataEvento, @PrecoPadrao)",
            evento);

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
        
        using var conn = _connectionFactory.CreateConnection();

        await conn.ExecuteAsync("DELETE FROM dbo.Eventos WHERE id = @Id", new { Id = id });

    }

}
