using Domain.Exceptions;

namespace Application.Exceptions;

public class CupomCadastrado : DomainException
{
    public CupomCadastrado()
    :base("Cupom já cadastrado") {}
}

public class CadastroCupomNaoAutorizado : DomainException
{
    public CadastroCupomNaoAutorizado()
    :base("Apenas administradores podem cadastrar cupons"){}
}

public class AlteracaoCupomNaoAutorizada : DomainException
{
    public AlteracaoCupomNaoAutorizada()
    :base("Apenas administradores podem alterar cupons"){}
}

public class CupomInexistente : DomainException
{
    public CupomInexistente()
    :base("Esse cupom não existe"){}
}

public class ValorMinimoInvalido : DomainException
{
    public ValorMinimoInvalido()
    :base("Valor Mínimo Inválido!O valor mínimo que o produto apto para desconto dever ter, deve ser maior do que 0.") {}
}

public class DataExpiracaoInvalida : DomainException
{
    public DataExpiracaoInvalida()
    :base("Data Inválida!A data de expiracao deve ser maior ou igual a data do dia de hoje.") {}
}

public class CupomVencido : DomainException
{
    public CupomVencido()
    :base("O data de validade do cupom expirou. Ele encontra-se desativado!!") {}
}

public class SemCupons : DomainException
{
    public SemCupons()
    :base("Nenhum cupom cadastrado no sistema.") {}
}

public class SemCuponsValidos : DomainException
{
    public SemCuponsValidos()
    :base("Nenhum cupom válido disponível no momento.") {}
}

public class DescontoInvalido : DomainException
{
    public DescontoInvalido()
    :base("Desconto Inválido!O desconto deve ser um inteiro maior do que 0 e menor ou igual a 100.") {}
}
