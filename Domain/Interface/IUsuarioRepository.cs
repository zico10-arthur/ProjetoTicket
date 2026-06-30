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

    /// <summary>
    /// Spec 200: Busca usuário por Id (Guid PK).
    /// </summary>
    Task<Usuario?> BuscarPorId(Guid id, CancellationToken ct);

    /// <summary>
    /// Spec 200: Remove usuário por Id.
    /// </summary>
    Task RemoverUsuario(Guid id, CancellationToken ct);

    /// <summary>
    /// Spec 200: Atualiza senha por Id.
    /// </summary>
    Task AtualizarSenha(Guid id, string novaSenha, CancellationToken ct);

    Task<IEnumerable<Usuario>> ListarTodos(CancellationToken ct);

    Task<IEnumerable<Usuario>> ListarTodosAsync(CancellationToken ct);

    Task AtualizarNomeAsync(Guid id, string novoNome, CancellationToken ct);

    Task AtualizarEmailAsync(Guid id, string novoEmail, CancellationToken ct);

}
