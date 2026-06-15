namespace Application.DTOs;

public class EventoResponseDTO
{
    
    public Guid Id { get; set; }

    public string Nome { get; set; }

    public int CapacidadeTotal{get; set;}

    public DateTime DataEvento {get; set;}

    public decimal PrecoPadrao {get; set;}

    public string VendedorCpf { get; set; } = string.Empty;

    public int Tipo { get; set; }

    public string? Descricao { get; set; }

    public string? Local { get; set; }

    public bool Cancelado { get; set; }

    public bool Gratuito { get; set; }

}
