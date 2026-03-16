using Domain.Exceptions;

namespace Application.Exceptions;

public class UsuarioCadastrado : DomainException
{
    public UsuarioCadastrado()
    :base("Usuário já cadastrado") {}
}

