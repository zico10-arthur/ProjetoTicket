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

        const string sql = @"INSERT INTO Usuarios(Cpf, Nome, Email, PerfilId)
        VALUES (@Cpf, @Nome, @Email, @PerfilId)";

        connection.Execute(sql, new
        {
            Cpf = usuario.Cpf,
            Nome = usuario.Nome,
            Email = usuario.Email,
            PerfilId = usuario.PerfilId
        });
    }

    public async Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct)
{
    using var connection = _factory.CreateConnection();

    const string sql = @"SELECT Cpf, Nome, Email, PerfilId
                             FROM Usuarios
                             WHERE Cpf = @Cpf";

    return await connection.QueryFirstOrDefaultAsync<Usuario>(
        new CommandDefinition(sql, new { Cpf = cpf }, cancellationToken: ct)
    );
}
}
