public class Cupom
{
    public string Codigo { get; private set; }
    public int PorcentagemDesconto { get; private set; }
    public decimal ValorMinimo { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }

    public bool EstaValidoParaUso => Ativo && DataExpiracao >= DateTime.Now;

    protected Cupom() { }

    private Cupom(string codigo, int percentDesc, decimal valorMin, DateTime? expiracao)
    {
        Codigo = codigo.ToUpper().Trim();
        PorcentagemDesconto = percentDesc;
        ValorMinimo = valorMin;
        DataExpiracao = expiracao;
        Ativo = true;
    }
}