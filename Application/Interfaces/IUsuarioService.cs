using Application.DTOs;

namespace Application.Interfaces;

public interface IUsuarioService
{
    Task CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct);

    Task CadastrarVendedor(CadastrarUsuarioDTO dto, CancellationToken ct, Guid AdminLogado);

    Task<Usuario> Login(LoginDTO dto, CancellationToken ct);
}
