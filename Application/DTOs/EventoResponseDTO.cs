namespace Application.DTOs;

public class EventoResponseDTO
{
    
    public Guid Id { get; set; }

    public string Nome { get; set; }

    public int CapacidadeTotal{get; set;}

    public DateTime DataEvento {get; set;}

    public decimal PrecoPadrao {get; set;}

    /// <summary>Spec 200: VendedorId (Guid?) em vez de VendedorCpf.</summary>
    public Guid? VendedorId { get; set; }

    public int Tipo { get; set; }

    public string? Descricao { get; set; }

    public string? Local { get; set; }

    public bool Cancelado { get; set; }

    public bool Gratuito { get; set; }

}
