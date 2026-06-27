namespace Application.Interfaces;

public interface ITokenService
{
    string GerarToken(Domain.Entities.Usuario usuario);

    /// <summary>
    /// Spec 180: Gera token JWT com 15 min de validade e claim purpose=password-reset.
    /// NÃO inclui claims de autenticação (role, cpf, perfilId).
    /// </summary>
    string GerarTokenRedefinicaoSenha(Domain.Entities.Usuario usuario);

    /// <summary>
    /// Spec 180: Valida token JWT de redefinição.
    /// Retorna email se válido, null se inválido/expirado/purpose errado.
    /// NUNCA lança exceção.
    /// </summary>
    string? ValidarTokenRedefinicaoSenha(string token);
}
