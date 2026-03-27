

namespace Domain.Interface;

public interface IUsuarioRepository
{
    void CadastrarUsuario(Usuario usuario);

    Task<Usuario?> BuscarCpfOuEmail(string cpf, string email, CancellationToken ct);
    
    Task<Usuario?> BuscarId(Guid perfilid, CancellationToken ct);

    Task<Usuario?> BuscarEmail(string email, CancellationToken ct);

    Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct);

}
