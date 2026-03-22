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

public class NomeVazio : DomainException
{
    public NomeVazio()
    :base("O nome do usuário tem que ser preenchido") {}
}

public class NomeInvalido : DomainException
{
    public NomeInvalido()
    :base("Nome inválido") {}
}

public class EmailVazio : DomainException
{
    public EmailVazio()
    :base("O email tem que ser preenchido") {}
}

public class EmailInvalido : DomainException
{
    public EmailInvalido()
    : base("O email é inválido"){}
}

public class SenhaVazia : DomainException
{
    public SenhaVazia()
    : base("A senha deve ser preenchida"){}
}

public class Senha8digitos : DomainException
{
    public Senha8digitos()
    : base("A senha deve ter 8 dígitos"){}
}

public class SenhaInvalida : DomainException
{
    public SenhaInvalida()
    : base("Senha inválida"){}
}