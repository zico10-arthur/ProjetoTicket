using Application.DTOs;

namespace Application.Interfaces;

public interface IUsuarioService
{
    Task CadastrarUsuario(CadastrarUsuarioDTO dto, CancellationToken ct);
}
