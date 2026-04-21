namespace Application.DTOs;

public class ReservarDTO
{
    public Guid EventoId { get; set; }
    public Guid IngressoId { get; set; }
    public string? CupomUtilizado { get; set; }
}