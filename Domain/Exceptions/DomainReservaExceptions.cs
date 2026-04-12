using Domain.Exceptions;

namespace Domain.Exceptions;

public class CupomInvalidoParaUso : DomainException
{
    public CupomInvalidoParaUso()
    :base("O cupom informado está inativo ou expirado.") {}
}

public class ValorMinimoCupomExcedido: DomainException
{
    public ValorMinimoCupomExcedido(decimal valorMinimo)
    :base($"O valor mínimo para este cupom é de {valorMinimo:C}.") {}
}