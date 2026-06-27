namespace Web.Components.Features.SeatMap;

/// <summary>
/// Representa um assento no mapa. Mapeie diretamente da API quando disponível.
/// </summary>
public sealed class SeatModel
{
    public int Id { get; init; }
    public string RowLabel { get; init; } = "";
    public int SeatNumber { get; init; }
    public SeatBlock Block { get; init; }
    public SeatStatus Status { get; set; }
    public decimal Price { get; init; }

    public string DisplayLabel => $"{RowLabel}{SeatNumber}";
}
