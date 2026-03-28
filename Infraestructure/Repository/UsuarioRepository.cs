using Dapper;
using Domain.Exceptions;
using Infrastructure.Database;
using Domain.Interface;
using Domain.Entities;


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

    public async Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct)
{
    cpf = (cpf ?? string.Empty).Replace(".", "").Replace("-", "").Trim();

    using var connection = _factory.CreateConnection();

    const string sql = @"
        SELECT 
            u.Cpf,
            u.Nome,
            u.Email,
            u.Senha,
            u.PerfilId,
            p.Id,
            p.Nome
        FROM Usuarios u
        INNER JOIN Perfis p ON u.PerfilId = p.Id
        WHERE u.Cpf = @Cpf";

    var result = await connection.QueryAsync<Usuario, Perfil, Usuario>(
        sql,
        (usuario, perfil) =>
        {
            usuario.Perfil = perfil;
            return usuario;
        },
        new { Cpf = cpf },
        splitOn: "Id" 
    );

    return result.FirstOrDefault();
}

    public async Task RemoverUsuario(Usuario usuario, CancellationToken ct)
    {
        string cpf = (usuario.Cpf?? string.Empty).Replace(".", "").Replace("-", "").Trim();

        using var connection =  _factory.CreateConnection();

        const string sql = "DELETE FROM Usuarios WHERE Cpf = @Cpf";

        int linhasAfetadas = await connection.ExecuteAsync(
        new CommandDefinition(
            sql, 
            new { Cpf = cpf }, 
            cancellationToken: ct
        )
    );

    }

    public async Task AtualizarSenha(string cpf, string novaSenha, CancellationToken ct)
{
    cpf = (cpf ?? string.Empty).Replace(".", "").Replace("-", "").Trim();

    using var connection = _factory.CreateConnection();

    const string sql = "UPDATE Usuarios SET Senha = @Senha WHERE Cpf = @Cpf";

    int linhasAfetadas = await connection.ExecuteAsync(new CommandDefinition(
        sql, 
        new { Senha = novaSenha, Cpf = cpf }, 
        cancellationToken: ct
    ));

}

}