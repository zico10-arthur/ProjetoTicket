using System.Linq;

namespace Web.Components.Features.SeatMap;

/// <summary>
/// Mapa hardcoded — Auditório Campus Quinta (UNIFESO).
/// Corredor central entre blocos esquerdo e direito.
/// </summary>
public static class QuintaAuditorioSeatData
{
    private static readonly string[] Rows = ["A", "B", "C", "D", "E", "F", "G", "H"];
    private const int SeatsPerBlock = 6;
    private const decimal DefaultPrice = 45m;
    private const decimal FrontRowPrice = 55m;
    private const decimal VipPrice = 75m;

    public static List<SeatModel> CreateDemoLayout() =>
        BuildLayout(new HashSet<int> { 3, 7, 14, 22, 31, 48, 52, 61, 72, 85 });

    public static List<SeatModel> CreateAlmostFullLayout()
    {
        var livres = new HashSet<int> { 10, 25, 40, 55, 70, 88 };
        var ocupados = Enumerable.Range(1, 96).Where(id => !livres.Contains(id)).ToHashSet();
        return BuildLayout(ocupados);
    }

    public static List<SeatModel> CreateSparseLayout() =>
        BuildLayout(new HashSet<int> { 5, 18, 44 });

    private static List<SeatModel> BuildLayout(HashSet<int> occupiedIds)
    {
        var seats = new List<SeatModel>();
        var id = 1;

        for (var rowIndex = 0; rowIndex < Rows.Length; rowIndex++)
        {
            var row = Rows[rowIndex];
            var isVipRow = rowIndex < 2;
            var setor = isVipRow ? "VIP" : "Geral";
            var price = isVipRow ? VipPrice : rowIndex < 4 ? FrontRowPrice : DefaultPrice;

            for (var block = 0; block < 2; block++)
            {
                var seatBlock = block == 0 ? SeatBlock.Left : SeatBlock.Right;

                for (var seatNum = 1; seatNum <= SeatsPerBlock; seatNum++)
                {
                    seats.Add(new SeatModel
                    {
                        Id = id,
                        IngressoId = Guid.Parse($"aaaaaaaa-aaaa-aaaa-aaaa-{id:D12}"),
                        RowLabel = row,
                        SeatNumber = seatNum,
                        Block = seatBlock,
                        Setor = setor,
                        Price = price,
                        Status = occupiedIds.Contains(id) ? SeatStatus.Occupied : SeatStatus.Available,
                    });
                    id++;
                }
            }
        }

        return seats;
    }
}
