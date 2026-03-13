using Domain.Exceptions;

namespace Domain.Exceptions;

public class CpfVazio : DomainException
{
    public CpfVazio()
    
    :base("O cpf precisa ser preenchido") {}
}

public class CpfInvalido : DomainException
{
    public CpfInvalido()
    :base("CPF inválido") {}
}
