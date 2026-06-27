using Dapper;
using Domain.Entities;
using Domain.Interface;
using Domain.DTOs;
using Infrastructure.Database;
using Microsoft.Data.SqlClient;

namespace Infraestructure.Repository;

public class ReservaRepository : IReservaRepository
{
    private readonly ConnectionFactory _factory;

    public ReservaRepository(ConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task CadastrarReservaComItens(Reserva reserva, CancellationToken ct)
    {
        using var conn = (SqlConnection)_factory.CreateConnection();
        await conn.OpenAsync(ct);
        using var transacao = conn.BeginTransaction();

        try
        {
            // 1. Inserir Reserva (Spec 200: UsuarioId, VendedorId)
            const string sqlReserva = @"
                INSERT INTO Reservas (Id, UsuarioId, EventoId, CupomUtilizado, ValorFinalPago, VendedorId)
                VALUES (@Id, @UsuarioId, @EventoId, @CupomUtilizado, @ValorFinalPago, @VendedorId)";

            await conn.ExecuteAsync(new CommandDefinition(sqlReserva, new
            {
                reserva.Id,
                reserva.UsuarioId,
                reserva.EventoId,
                reserva.CupomUtilizado,
                reserva.ValorFinalPago,
                reserva.VendedorId
            }, transacao, cancellationToken: ct));

            // 2. Inserir ItensReserva
            foreach (var item in reserva.Itens)
            {
                item.VincularReserva(reserva.Id);
            }

            const string sqlItem = @"
                INSERT INTO ItensReserva (Id, ReservaId, CpfParticipante, IngressoId, PrecoUnitario, Reembolsado)
                VALUES (@Id, @ReservaId, @CpfParticipante, @IngressoId, @PrecoUnitario, @Reembolsado)";

            await conn.ExecuteAsync(new CommandDefinition(sqlItem, reserva.Itens, transacao, cancellationToken: ct));

            // 3. Bloquear ingressos
            const string sqlBloqueio = @"
                UPDATE Ingressos SET Status = 1, DataBloqueio = GETDATE()
                WHERE Id IN @Ids AND Status = 0";

            var ids = reserva.Itens.Select(i => i.IngressoId).ToList();
            var bloqueados = await conn.ExecuteAsync(new CommandDefinition(
                sqlBloqueio, new { Ids = ids }, transacao, cancellationToken: ct));

            if (bloqueados != ids.Count)
                throw new InvalidOperationException("Um ou mais assentos não estão mais disponíveis.");

            await transacao.CommitAsync(ct);
        }
        catch
        {
            await transacao.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<Reserva?> BuscarPorId(Guid id, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "SELECT * FROM Reservas WHERE Id = @Id";

        return await connection.QuerySingleOrDefaultAsync<Reserva>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Spec 200: Listar reservas por UsuarioId (Guid).
    /// </summary>
    public async Task<IEnumerable<Reserva>> ListarPorUsuarioId(Guid usuarioId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "SELECT * FROM Reservas WHERE UsuarioId = @UsuarioId";

        return await connection.QueryAsync<Reserva>(
            new CommandDefinition(sql, new { UsuarioId = usuarioId }, cancellationToken: ct)
        );
    }

    public async Task<bool> ReservaExistenteParaCpfNoEvento(string cpf, Guid eventoId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 
                FROM Reservas r
                INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
                WHERE ir.CpfParticipante = @Cpf AND r.EventoId = @EventoId
            ) THEN 1 ELSE 0 END AS BIT)";

        return await connection.QuerySingleAsync<bool>(
            new CommandDefinition(sql, new { Cpf = cpf, EventoId = eventoId }, cancellationToken: ct)
        );
    }

    public async Task DeletarReservasNaoPagasExpiradas(int minutosExpiracao, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            UPDATE i SET i.Status = 0
            FROM Ingressos i
            INNER JOIN ItensReserva ir ON ir.IngressoId = i.Id
            INNER JOIN Reservas r ON r.Id = ir.ReservaId
            WHERE i.Status = 1 AND r.DataBloqueio <= DATEADD(minute, -@Minutos, GETDATE());

            DELETE FROM ItensReserva
            WHERE ReservaId IN (
                SELECT Id FROM Reservas
                WHERE DataBloqueio <= DATEADD(minute, -@Minutos, GETDATE())
            );

            DELETE FROM Reservas
            WHERE DataBloqueio <= DATEADD(minute, -@Minutos, GETDATE());";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Minutos = minutosExpiracao }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Spec 200: Listar reservas detalhadas por UsuarioId (Guid).
    /// </summary>
    public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorUsuarioId(Guid usuarioId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT 
                r.Id,
                e.Nome AS NomeEvento,
                e.DataEvento,
                STRING_AGG(i.Posicao, ', ') AS PosicaoIngresso,
                STRING_AGG(i.Setor, ', ') AS SetorIngresso,
                r.CupomUtilizado,
                r.ValorFinalPago,
                r.Pago
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
            INNER JOIN Ingressos i ON ir.IngressoId = i.Id
            WHERE r.UsuarioId = @UsuarioId
            GROUP BY r.Id, e.Nome, e.DataEvento, r.CupomUtilizado, r.ValorFinalPago, r.Pago";

        return await connection.QueryAsync<ReservaDetalhadaDTO>(
            new CommandDefinition(sql, new { UsuarioId = usuarioId }, cancellationToken: ct)
        );
    }

    /// <summary>
    /// Spec 200: JOIN com Usuarios via UsuarioId (Guid).
    /// </summary>
    public async Task<IEnumerable<ReservaAdminDTO>> ListarTodasDetalhadasAdmin(CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT 
                r.Id,
                e.Nome AS NomeEvento,
                e.DataEvento,
                STRING_AGG(i.Posicao, ', ') AS PosicaoIngresso,
                STRING_AGG(i.Setor, ', ') AS SetorIngresso,
                r.CupomUtilizado,
                r.ValorFinalPago,
                r.Pago,
                u.Nome AS NomeUsuario,
                u.Id AS UsuarioId
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
            INNER JOIN Ingressos i ON ir.IngressoId = i.Id
            INNER JOIN Usuarios u ON r.UsuarioId = u.Id
            GROUP BY r.Id, e.Nome, e.DataEvento, r.CupomUtilizado, r.ValorFinalPago, r.Pago, u.Nome, u.Id
            ORDER BY e.Nome, r.Id";

        return await connection.QueryAsync<ReservaAdminDTO>(new CommandDefinition(sql, cancellationToken: ct));
    }

    /// <summary>
    /// Spec 200: Listar reservas por VendedorId (Guid).
    /// </summary>
    public async Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedorId(
        Guid vendedorId, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT 
                r.Id,
                e.Nome AS NomeEvento,
                e.DataEvento,
                r.ValorFinalPago,
                r.Pago,
                u.Nome AS NomeComprador,
                u.Id AS CompradorId
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN Usuarios u ON r.UsuarioId = u.Id
            WHERE r.VendedorId = @VendedorId
            ORDER BY e.Nome, r.Id";

        return await connection.QueryAsync<ReservaVendedorDTO>(
            new CommandDefinition(sql, new { VendedorId = vendedorId }, cancellationToken: ct));
    }
}