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
    : base("Email ou senha inválidos.") {}
}

public class UsuarioNotFound : DomainException
{
    public UsuarioNotFound()
    : base("Usuário não encontrado") {}
}

public class CnpjJaCadastrado : DomainException
{
    public CnpjJaCadastrado()
    : base("CNPJ já cadastrado") {}
}

public class EmailJaCadastrado : DomainException
{
    public EmailJaCadastrado()
    : base("E-mail já cadastrado") {}
}

/// <summary>
/// ST-08: Usuário está inativo.
/// </summary>
public class UsuarioInativoException : DomainException
{
    public UsuarioInativoException()
    : base("Usuário inativo. Contate o administrador.") {}
}
