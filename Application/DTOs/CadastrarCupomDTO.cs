namespace Application.DTOs;

public class CadastrarCupomDTO
{
    public string Codigo { get; set; } = string.Empty;
    public int PorcentagemDesconto { get; set; }
    public decimal ValorMinimo { get; set; }
    public DateTime? DataExpiracao { get; set; }
}
