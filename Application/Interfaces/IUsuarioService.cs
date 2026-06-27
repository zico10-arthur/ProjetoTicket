using Application.DTOs;

namespace Application.Interfaces;

public interface IUsuarioService
{
    Task CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct);

    /// <summary>
    /// ST-01: Auto cadastro público de vendedor.
    /// </summary>
    Task<VendedorCadastradoDTO> CadastrarVendedor(CadastrarVendedorDTO dto, CancellationToken ct);

    /// <summary>
    /// ST-08: Login unificado com BCrypt. Retorna JWT + dados do usuário.
    /// </summary>
    Task<LoginResponseDTO> Login(LoginDTO dto, CancellationToken ct);

    /// <summary>
    /// Spec 200: Busca usuário por Id (Guid).
    /// </summary>
    Task<UsuarioSaidaDTO> UsuarioEspecifico(Guid id, CancellationToken ct);

    /// <summary>
    /// Spec 200: Remove usuário por Id.
    /// </summary>
    Task RemoverUsuario(Guid id, CancellationToken ct);

    Task AlterarSenha(AlterarSenhaDTO dto, Guid id, CancellationToken ct);

    Task AlterarEmailAsync(Guid id, AlterarEmailDTO dto, CancellationToken ct);

    Task AlterarNomeAsync(Guid id, AlterarNomeDTO dto, CancellationToken ct);

    Task<IEnumerable<UsuarioResponseDTO>> ListarUsuariosAsync(CancellationToken ct);

}