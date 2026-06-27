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
            // 1. Inserir Reserva
            const string sqlReserva = @"
                INSERT INTO Reservas (Id, UsuarioCpf, EventoId, CupomUtilizado, ValorFinalPago, VendedorCpf)
                VALUES (@Id, @UsuarioCpf, @EventoId, @CupomUtilizado, @ValorFinalPago, @VendedorCpf)";

            await conn.ExecuteAsync(new CommandDefinition(sqlReserva, new
            {
                reserva.Id,
                reserva.UsuarioCpf,
                reserva.EventoId,
                reserva.CupomUtilizado,
                reserva.ValorFinalPago,
                reserva.VendedorCpf
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

    public async Task<IEnumerable<Reserva>> ListarPorCpf(string cpf, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = "SELECT * FROM Reservas WHERE UsuarioCpf = @Cpf";

        return await connection.QueryAsync<Reserva>(
            new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: ct)
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

    public async Task<IEnumerable<ReservaDetalhadaDTO>> ListarReservasDetalhadasPorCpf(string cpf, CancellationToken ct)
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
                r.Reembolsada
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
            INNER JOIN Ingressos i ON ir.IngressoId = i.Id
            WHERE r.UsuarioCpf = @Cpf
            GROUP BY r.Id, e.Nome, e.DataEvento, r.CupomUtilizado, r.ValorFinalPago, r.Pago, r.Reembolsada";

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
                STRING_AGG(i.Posicao, ', ') AS PosicaoIngresso,
                STRING_AGG(i.Setor, ', ') AS SetorIngresso,
                r.CupomUtilizado,
                r.ValorFinalPago,
                r.Pago,
                r.Reembolsada,
                u.Nome AS NomeUsuario,
                u.Cpf AS CpfUsuario
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN ItensReserva ir ON ir.ReservaId = r.Id
            INNER JOIN Ingressos i ON ir.IngressoId = i.Id
            INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf
            GROUP BY r.Id, e.Nome, e.DataEvento, r.CupomUtilizado, r.ValorFinalPago, r.Pago, r.Reembolsada, u.Nome, u.Cpf
            ORDER BY e.Nome, r.Id";

        return await connection.QueryAsync<ReservaAdminDTO>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<IEnumerable<ReservaVendedorDTO>> ListarReservasDetalhadasPorVendedor(
        string vendedorCpf, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"
            SELECT 
                r.Id,
                e.Nome AS NomeEvento,
                e.DataEvento,
                r.ValorFinalPago,
                r.Pago,
                r.Reembolsada,
                u.Nome AS NomeComprador,
                u.Cpf AS CpfComprador
            FROM Reservas r
            INNER JOIN Eventos e ON r.EventoId = e.Id
            INNER JOIN Usuarios u ON r.UsuarioCpf = u.Cpf
            WHERE r.VendedorCpf = @VendedorCpf
            ORDER BY e.Nome, r.Id";

        return await connection.QueryAsync<ReservaVendedorDTO>(
            new CommandDefinition(sql, new { VendedorCpf = vendedorCpf }, cancellationToken: ct));
    }

    public async Task CancelarComTransacao(Guid reservaId, CancellationToken ct)
    {
        using var connection = (SqlConnection)_factory.CreateConnection();
        await connection.OpenAsync(ct);
        using var transaction = connection.BeginTransaction();

        try
        {
            // 1. Marcar reserva como reembolsada
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE Reservas SET Reembolsada = 1 WHERE Id = @reservaId",
                new { reservaId }, transaction, cancellationToken: ct));

            // 2. Marcar todos os itens da reserva como reembolsados
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE ItensReserva SET Reembolsado = 1 WHERE ReservaId = @reservaId",
                new { reservaId }, transaction, cancellationToken: ct));

            // 3. Liberar ingressos (Status = 0) e limpar DataBloqueio
            await connection.ExecuteAsync(new CommandDefinition(@"
                UPDATE Ingressos
                SET Status = 0, DataBloqueio = NULL
                WHERE Id IN (
                    SELECT IngressoId FROM ItensReserva WHERE ReservaId = @reservaId
                )",
                new { reservaId }, transaction, cancellationToken: ct));

            // 4. Marcar pagamento como reembolsado (StatusPagamento.Reembolsado = 2)
            //    Apenas se Status = 1 (Confirmado)
            await connection.ExecuteAsync(new CommandDefinition(
                "UPDATE Pagamentos SET Status = 2 WHERE ReservaId = @reservaId AND Status = 1",
                new { reservaId }, transaction, cancellationToken: ct));

            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}
