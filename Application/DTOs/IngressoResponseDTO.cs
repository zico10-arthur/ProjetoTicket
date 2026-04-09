namespace Application.DTOs;

public class IngressoResponseDTO
{
    public Guid Id { get; set; }
    public string Posicao { get; set; }
    public string Setor { get; set; }
    public decimal Preco { get; set; }
    public string Status { get; set; }
}
