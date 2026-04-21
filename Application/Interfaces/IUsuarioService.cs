using Application.DTOs;

namespace Application.Interfaces;

public interface IUsuarioService
{
    Task CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct);

    Task CadastrarVendedor(CadastrarUsuarioDTO dto, CancellationToken ct, Guid AdminLogado);

    Task<string> Login(LoginDTO dto, CancellationToken ct);

    Task<UsuarioSaidaDTO> UsuarioEspecifico(string cpf, CancellationToken ct);

    Task RemoverUsuario(string cpf, CancellationToken ct);

    Task AlterarSenha( AlterarSenhaDTO dto, string cpf, CancellationToken ct);

    Task AlterarEmailAsync(string cpf, AlterarEmailDTO dto ,CancellationToken ct);

    Task AlterarNomeAsync(string cpf, AlterarNomeDTO dto, CancellationToken ct);

    Task<IEnumerable<UsuarioResponseDTO>> ListarUsuariosAsync(CancellationToken ct);


}
