

namespace Domain.Interface;

public interface IUsuarioRepository
{
    void CadastrarUsuario(Usuario usuario);
    Task<Usuario?> BuscarCpf(string cpf, CancellationToken ct);
}
