using Domain.Exceptions;

namespace Application.Exceptions;

public class CupomCadastrado : DomainException
{
    public CupomCadastrado()
    :base("Cupom já cadastrado") {}
}

