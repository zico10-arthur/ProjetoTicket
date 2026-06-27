namespace Application.Exceptions;

public class TokenRedefinicaoInvalido : Exception
{
    public TokenRedefinicaoInvalido()
        : base("Token expirado ou inválido.") { }
}
