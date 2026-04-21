using Domain.Exceptions;

namespace Application.Exceptions;

public class UsuarioCadastrado : DomainException
{
    public UsuarioCadastrado()
    :base("Usuário já cadastrado") {}
}

public class UsuarioNaoAutorizado : DomainException
{
    public UsuarioNaoAutorizado()
    :base("Apenas administradores podem cadastrar vendedores"){}
}

public class LoginErro : DomainException
{
    public LoginErro()
    : base("Usuário não encontrado ou senha inválida") {}
}

public class UsuarioNotFound : DomainException
{
    public UsuarioNotFound()
    : base("Usuário não encontrado") {}
}

