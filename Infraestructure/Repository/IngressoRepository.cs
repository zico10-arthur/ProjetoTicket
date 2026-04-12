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
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT Id, EventoId, Preco, Posicao, Setor, Status
            FROM dbo.Ingressos
            WHERE EventoId = @EventoId";

        var command = new CommandDefinition(
            sql,
            new { EventoId = eventoId },
            cancellationToken: ct
        );

        var ingressos = await connection.QueryAsync<Ingresso>(command);

        return ingressos;
    }

    public async Task<Ingresso?> BuscarIngressoId(Guid ingressoId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT Id, EventoId, Preco, Posicao, Setor, Status
            FROM Ingressos
            WHERE Id = @IngressoId";

        var command = new CommandDefinition(
            sql,
            new { IngressoId = ingressoId }, // Casing alinhado com o parâmetro @IngressoId
            cancellationToken: ct
        );

        var ingresso = await connection.QueryFirstOrDefaultAsync<Ingresso>(command);

        return ingresso;
    }

    public async Task<bool> BloquearIngressoTemporariamente(Guid ingressoId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            UPDATE Ingressos 
            SET Status = 1, DataBloqueio = GETDATE() 
            WHERE Id = @Id AND Status = 0";

        int linhasAfetadas = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = ingressoId }, cancellationToken: ct)
        );

        return linhasAfetadas > 0;
    }

    public async Task LiberarAssentosExpirados(int minutosExpiracao, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            UPDATE Ingressos 
            SET Status = 0, DataBloqueio = NULL 
            WHERE Status = 1 AND DataBloqueio <= DATEADD(minute, -@Minutos, GETDATE())";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Minutos = minutosExpiracao }, cancellationToken: ct)
        );
    }

}
