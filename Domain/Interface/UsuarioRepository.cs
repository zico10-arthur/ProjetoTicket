

namespace Domain.Interface;

public interface IUsuarioRepository
{
    void CadastrarUsuario(Usuario usuario);

    Task<Usuario?> BuscarCpfOuEmail(string cpf, string email, CancellationToken ct);
    
}
