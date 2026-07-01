namespace Web.Components.Features.SeatMap;

/// <summary>
/// Representa um assento no mapa. Mapeie diretamente da API quando disponível.
/// </summary>
public sealed class SeatModel
{
    public const int MaxSelection = 4;

    public int Id { get; init; }
    public Guid IngressoId { get; init; }
    public string RowLabel { get; init; } = "";
    public int SeatNumber { get; init; }
    public int? GlobalNumber { get; init; }
    public SeatBlock Block { get; init; }
    public string Setor { get; init; } = "Geral";
    public SeatStatus Status { get; set; }
    public decimal Price { get; init; }

    public string DisplayLabel => $"{RowLabel}{GlobalSeatNumber}";

    /// <summary>Número contínuo na fila (ex.: esquerda 1–6, direita 7–12).</summary>
    public int GlobalSeatNumber => GlobalNumber ?? (Block == SeatBlock.Left ? SeatNumber : SeatNumber + 6);

    public bool IsVip => Setor.Equals("VIP", StringComparison.OrdinalIgnoreCase);
}
