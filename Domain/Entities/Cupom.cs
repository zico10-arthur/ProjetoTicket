public class Cupom
{
    public string Codigo {get; private set;}

    public decimal PorcentagemDesconto {get; private set;}

    public decimal ValorMinimo{get; private set;}

    public Cupom(string codigo, decimal porcentagemdesconto, decimal valorminimo)
    {
        Codigo = codigo;
        PorcentagemDesconto = porcentagemdesconto;
        ValorMinimo = valorminimo;
    }
}