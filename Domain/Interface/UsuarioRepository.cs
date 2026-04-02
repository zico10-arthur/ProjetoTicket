

namespace Domain.Interface;

public interface IUsuarioRepository
{
    void CadastrarUsuario(Usuario usuario);

    Task<Usuario?> BuscarCpfOuEmail(string cpf, string email, CancellationToken ct);
    
    Task<Usuario?> BuscarId(Guid perfilid, CancellationToken ct);

    Task<Usuario?> BuscarEmail(string email, CancellationToken ct);

    Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct);

    Task RemoverUsuario(Usuario usuario, CancellationToken ct);

    Task AtualizarSenha(string cpf, string novaSenha, CancellationToken ct);

    Task AtualizarEmailAsync(Usuario usuario, CancellationToken ct);

    Task AtualizarNomeAsync(Usuario usuario, CancellationToken ct);

    Task<IEnumerable<Usuario>> ListarTodosAsync(CancellationToken ct);
}
