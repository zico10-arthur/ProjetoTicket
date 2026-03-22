using Dapper;
using Domain.Exceptions;
using Infrastructure.Database;
using Domain.Interface;


namespace Infraestructure.Repository;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly ConnectionFactory _factory;

    public UsuarioRepository(ConnectionFactory factory)
    {
        _factory = factory;
    }

    public void CadastrarUsuario(Usuario usuario)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"INSERT INTO Usuarios(Cpf, Nome, Email, PerfilId, Senha)
        VALUES (@Cpf, @Nome, @Email, @PerfilId, @Senha)";

        connection.Execute(sql, new
        {
            Cpf = usuario.Cpf,
            Nome = usuario.Nome,
            Email = usuario.Email,
            PerfilId = usuario.PerfilId,
            Senha = usuario.Senha
        });
    }

    public async Task<Usuario?> BuscarCpfOuEmail(string cpf, string email, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"SELECT Cpf, Nome, Email, PerfilId, Senha
                         FROM Usuarios
                         WHERE Cpf = @Cpf OR Email = @Email";

        return await connection.QueryFirstOrDefaultAsync<Usuario>(
        new CommandDefinition(
            sql,
            new { Cpf = cpf, Email = email },
            cancellationToken: ct
        )
        );
    }

    public async Task<Usuario?> BuscarId(Guid perfilid, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"SELECT Cpf, Nome, Email, PerfilId, Senha
                         FROM Usuarios
                         WHERE PerfilId = @PerfilId";

        return await connection.QueryFirstOrDefaultAsync<Usuario>(
        new CommandDefinition(
            sql,
            new { PerfilId = perfilid },
            cancellationToken: ct
        )
        );
    }

    public async Task<Usuario?> BuscarEmail(string email, CancellationToken ct)
    {
        using var connection = _factory.CreateConnection();

        const string sql = @"SELECT Cpf, Nome, Email, PerfilId, Senha
                         FROM Usuarios
                         WHERE Email = @Email";

        return await connection.QueryFirstOrDefaultAsync<Usuario>(
        new CommandDefinition(
            sql,
            new { Email = email },
            cancellationToken: ct
        )
        );
    }
}