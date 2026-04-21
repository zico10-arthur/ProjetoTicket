using Domain.Exceptions;

namespace Domain.Exceptions;

public class CupomExpirado : DomainException
{
    public CupomExpirado()
    :base("Cupom Inválido!O cupom selecionado expirou") {}
}

public class DataExpiracaoInvalida : DomainException
{
    public DataExpiracaoInvalida()
    :base("Data Inválida!A data de expiracao deve ser maior ou igual a data do dia de hoje.") {}
}

public class ValorMinimoInvalido : DomainException
{
    public ValorMinimoInvalido()
    :base("Valor Mínimo Inválido!O valor mínimo que o produto apto para desconto dever ter, deve ser maior do que 0.") {}
}

public class DescontoInvalido : DomainException
{
    public DescontoInvalido()
    :base("Desconto Inválido!O desconto deve ser um inteiro maior do que 0 e menor ou igual a 100.") {}
}

public class CodigoVazio : DomainException
{
    public CodigoVazio()
    :base("Código Inválido!O código não pode ser vazio.") {}
}

public class TamanhoCodigoInvalido : DomainException
{
    public TamanhoCodigoInvalido()
    :base("Código Inválido!O código deve ter mais de 5 e menos de 50 digitos.") {}
}

public class FormatoCodigoInvalido : DomainException
{
    public FormatoCodigoInvalido()
    :base("Código Inválido!O código deve ter uma palavra mais o valor do desconto no final. Por exemplo: PROMOCAO50.") {}
}