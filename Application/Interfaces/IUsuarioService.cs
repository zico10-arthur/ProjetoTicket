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

    Task<UsuarioSaidaDTO> UsuarioEspecifico(string cpf, CancellationToken ct);

    Task RemoverUsuario(string cpf, CancellationToken ct);

    Task AlterarSenha( AlterarSenhaDTO dto, string cpf, CancellationToken ct);

    Task AlterarEmailAsync(string cpf, AlterarEmailDTO dto ,CancellationToken ct);

    Task AlterarNomeAsync(string cpf, AlterarNomeDTO dto, CancellationToken ct);

    Task<IEnumerable<UsuarioResponseDTO>> ListarUsuariosAsync(CancellationToken ct);


    /// <summary>
    /// Spec 180: Busca usuário por email, gera token JWT de redefinição,
    /// enfileira e-mail com link. Se email não existir ou usuário inativo,
    /// retorna sem fazer nada (não vaza informação).
    /// </summary>
    Task SolicitarRedefinicaoSenha(string email, CancellationToken ct);

    /// <summary>
    /// Spec 180: Valida token JWT de redefinição, valida nova senha,
    /// aplica BCrypt e atualiza no banco.
    /// Lança TokenRedefinicaoInvalido se token inválido/expirado.
    /// Lança exceção de validação se senha fraca.
    /// </summary>
    Task RedefinirSenha(string token, string novaSenha, CancellationToken ct);

}
