namespace Web.Models;

public class CupomViewModel
{
    public string Codigo { get; set; } = string.Empty;
    public int PorcentagemDesconto { get; set; }
    public decimal ValorMinimo { get; set; }
    public DateTime DataExpiracao { get; set; }
    public bool Ativo { get; set; }
}