using Dapper;
using Domain.Entities;
using Domain.Interface;
using Domain.DTOs;
using Infrastructure.Database;

namespace Infraestructure.Repository;

public class ReservaRepository : IReservaRepository
{
    private readonly ConnectionFactory _factory;

    public ReservaRepository(ConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task CadastrarReserva(Reserva reserva, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            INSERT INTO Reservas (Id, UsuarioCpf, EventoId, IngressoId, CupomUtilizado, ValorFinalPago)
            VALUES (@Id, @UsuarioCpf, @EventoId, @IngressoId, @CupomUtilizado, @ValorFinalPago)";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new
            {
                Id = reserva.Id,
                UsuarioCpf = reserva.UsuarioCpf,
                EventoId = reserva.EventoId,
                IngressoId = reserva.IngressoId,
                CupomUtilizado = reserva.CupomUtilizado,
                ValorFinalPago = reserva.ValorFinalPago
            }, cancellationToken: ct)
        );
    }

    public async Task<IEnumerable<Reserva?>> BuscarCpfEEventoId(string cpf, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "SELECT * FROM Reservas WHERE UsuarioCpf = @Cpf";

        return await connection.QueryAsync<Reserva>(
            new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: ct)
        );
    }

    public async Task<IEnumerable<Reserva>> ListarPorCpf(string cpf, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "SELECT * FROM Reservas WHERE UsuarioCpf = @Cpf";

        return await connection.QueryAsync<Reserva>(
            new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: ct)
        );
    }

    public async Task<bool> ReservaExistenteParaUsuario(string cpf, Guid eventoId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT CAST(CASE WHEN COUNT(1) > 0 THEN 1 ELSE 0 END AS BIT) 
            FROM Reservas 
            WHERE UsuarioCpf = @Cpf AND EventoId = @EventoId";

        return await connection.QuerySingleAsync<bool>(
            new CommandDefinition(sql, new { Cpf = cpf, EventoId = eventoId }, cancellationToken: ct)
        );
    }

    public async Task DeletarReservasNaoPagasExpiradas(int minutosExpiracao, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            DELETE r
            FROM Reservas r
            INNER JOIN Ingressos i ON r.IngressoId = i.Id
            WHERE i.Status = 1 AND i.DataBloqueio <= DATEADD(minute, -@Minutos, GETDATE())";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Minutos = minutosExpiracao }, cancellationToken: ct)
        );
    }

    public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT 
                r.Id,
                e.Nome AS NomeEvento,
                e.DataEvento,
                i.Posicao AS PosicaoIngresso,
                i.Setor AS SetorIngresso,
                r.CupomUtilizado,
                r.ValorFinalPago,
                u.Nome AS NomeUsuario,
                u.Cpf AS CpfUsuario
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN Ingressos i ON r.IngressoId = i.Id
            INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf";

        return await connection.QueryAsync<ReservaDetalhadaDTO>(
            new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: ct)
        );
    }

    public async Task<IEnumerable<ReservaAdminDTO>> ListarTodasDetalhadasAdmin(CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT 
                r.Id,
                e.Nome AS NomeEvento,
                e.DataEvento,
                i.Posicao AS PosicaoIngresso,
                i.Setor AS SetorIngresso,
                r.CupomUtilizado,
                r.ValorFinalPago,
                u.Nome AS NomeUsuario,
                u.Cpf AS CpfUsuario
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN Ingressos i ON r.IngressoId = i.Id
            INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf
            ORDER BY e.Nome, r.Id"; // Ordenado por evento para facilitar o agrupamento

        return await connection.QueryAsync<ReservaAdminDTO>(new CommandDefinition(sql, cancellationToken: ct));
    }

}