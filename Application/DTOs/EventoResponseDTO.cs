namespace Application.DTOs;

public class EventoResponseDTO
{
    
    public Guid id {get; set;}

    public string Nome{get; set;}

    public int CapacidadeTotal{get; set;}

    public DateTime DataEvento {get; set;}

    public decimal PrecoPadrao {get; set;}

}
