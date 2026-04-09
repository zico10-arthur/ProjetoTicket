using Domain.Entities;
using Domain.Interface;
using Infrastructure.Database;
using Dapper;


namespace Infraestructure.Repository;

public class IngressoRepository : IIngressoRepository
{
    private readonly ConnectionFactory _factory;

    public IngressoRepository(ConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task<IEnumerable<Ingresso>> ListarPorEventoIdAsync(Guid eventoId, CancellationToken ct)
{
    using var conn = _factory.CreateConnection();

    const string sql = @"
        SELECT Id, EventoId, Preco, Posicao, Setor, Status
        FROM dbo.Ingressos
        WHERE EventoId = @EventoId";

    var command = new CommandDefinition(
        sql,
        new { EventoId = eventoId },
        cancellationToken: ct
    );

    var ingressos = await conn.QueryAsync<Ingresso>(command);

    return ingressos;
}




}
