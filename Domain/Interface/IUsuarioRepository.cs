using Domain.Entities;

namespace Domain.Interface;

public interface IUsuarioRepository
{
    void CadastrarUsuario(Usuario usuario);

    /// <summary>
    /// ST-01: Persiste vendedor com todos os campos (CNPJ, NomeFantasia, etc.).
    /// </summary>
    int CadastrarVendedor(Usuario vendedor);

    Task<Usuario?> BuscarCpfOuEmail(string cpf, string email, CancellationToken ct);

    /// <summary>
    /// ST-01: Busca usuário por CNPJ ou Email para verificação de unicidade.
    /// </summary>
    Task<Usuario?> BuscarCnpjOuEmail(string cnpj, string email, CancellationToken ct);

    /// <summary>
    /// ST-01: Busca usuário por CNPJ.
    /// </summary>
    Task<Usuario?> BuscarPorCnpj(string cnpj, CancellationToken ct);
    
    Task<Usuario?> BuscarId(Guid perfilid, CancellationToken ct);

    Task<Usuario?> BuscarEmail(string email, CancellationToken ct);

    Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct);

    Task RemoverUsuario(Usuario usuario, CancellationToken ct);

    Task AtualizarSenha(string cpf, string novaSenha, CancellationToken ct);

    Task<IEnumerable<Usuario>> ListarTodos(CancellationToken ct);

    Task<IEnumerable<Usuario>> ListarTodosAsync(CancellationToken ct);

    Task AtualizarNomeAsync(Usuario usuario, CancellationToken ct);

     Task AtualizarEmailAsync(Usuario usuario, CancellationToken ct);


}
