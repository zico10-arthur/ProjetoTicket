namespace Web.Services;

public sealed class PurchaseStateService
{
    public const int MaxSeats = 4;

    public Guid EventoId { get; private set; }
    public List<SelectedSeatState> SelectedSeats { get; private set; } = [];

    public bool HasSelection => SelectedSeats.Count > 0;

    public void SetSelection(Guid eventoId, IEnumerable<SelectedSeatState> seats)
    {
        EventoId = eventoId;
        SelectedSeats = seats.ToList();
    }

    public void Clear()
    {
        EventoId = Guid.Empty;
        SelectedSeats = [];
    }

    public bool MatchesEvent(Guid eventoId) =>
        EventoId == eventoId && SelectedSeats.Count > 0;
}

public sealed record SelectedSeatState(
    Guid IngressoId,
    string Posicao,
    string Setor,
    decimal Preco,
    string DisplayLabel);
