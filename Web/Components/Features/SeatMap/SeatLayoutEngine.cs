namespace Web.Components.Features.SeatMap;

/// <summary>
/// Layout estilo Ticket360: palco curvo no topo, blocos esquerdo/direito e corredor central.
/// </summary>
internal static class SeatLayoutEngine
{
    internal const double SeatSize = 22;
    internal const double SeatGap = 5;
    internal const double RowGap = 7;
    internal const double RowLabelWidth = 22;
    internal const double AisleWidth = 28;
    internal const double PaddingX = 28;
    internal const double PaddingTop = 12;
    internal const double PaddingBottom = 24;
    internal const double StageDepth = 36;
    internal const double StageGap = 28;
    internal const double ColHeaderHeight = 16;

    internal sealed record RenderItem(SeatModel Seat, double X, double Y);

    internal sealed record TextLabel(string Text, double X, double Y, string CssClass, string Anchor = "middle");

    internal sealed record StageArc(double Left, double Right, double BaseY, double PeakY, double CenterX);

    internal sealed record Layout(
        double Width,
        double Height,
        StageArc Stage,
        double GridLeft,
        double GridTop,
        double GridWidth,
        IReadOnlyList<RenderItem> Seats,
        IReadOnlyList<TextLabel> Labels);

    internal static Layout Build(IEnumerable<SeatModel> seats, string sectorFilter)
    {
        var allSeats = seats.ToList();
        var rows = allSeats
            .Select(s => s.RowLabel)
            .Distinct()
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();

        if (rows.Count == 0)
            rows = ["A"];

        var maxLeft = Math.Max(1, rows.Max(row =>
            allSeats.Count(s => s.RowLabel == row && s.Block == SeatBlock.Left && MatchesFilter(s, sectorFilter))));

        var maxRight = Math.Max(1, rows.Max(row =>
            allSeats.Count(s => s.RowLabel == row && s.Block == SeatBlock.Right && MatchesFilter(s, sectorFilter))));

        var leftBlockWidth = maxLeft * SeatSize + (maxLeft - 1) * SeatGap;
        var rightBlockWidth = maxRight * SeatSize + (maxRight - 1) * SeatGap;
        var gridWidth = RowLabelWidth + leftBlockWidth + AisleWidth + rightBlockWidth + RowLabelWidth;
        var gridHeight = ColHeaderHeight + rows.Count * (SeatSize + RowGap) - RowGap;

        var width = gridWidth + PaddingX * 2;
        var height = PaddingTop + StageDepth + StageGap + gridHeight + PaddingBottom;

        var gridLeft = PaddingX;
        var gridTop = PaddingTop + StageDepth + StageGap;
        var stageLeft = gridLeft + gridWidth * 0.12;
        var stageRight = gridLeft + gridWidth * 0.88;
        var stageBaseY = PaddingTop + StageDepth;
        var stagePeakY = PaddingTop + 4;
        var stageCenterX = gridLeft + gridWidth / 2;

        var leftX = gridLeft + RowLabelWidth;
        var rightX = leftX + leftBlockWidth + AisleWidth;

        var items = new List<RenderItem>();
        var labels = new List<TextLabel>();

        labels.Add(new TextLabel("PALCO", stageCenterX, PaddingTop + StageDepth - 10, "t360-stage-text"));

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            var rowY = gridTop + ColHeaderHeight + rowIndex * (SeatSize + RowGap);
            var rowCenterY = rowY + SeatSize / 2 + 4;

            // Leve curva: fileiras de trás um pouco mais largas (efeito auditório)
            var curveOffset = rowIndex * 1.2;

            labels.Add(new TextLabel(row, gridLeft + 10, rowCenterY, "t360-row-label", "middle"));

            PlaceBlock(allSeats, row, SeatBlock.Left, leftX + curveOffset, rowY, rowIndex, gridTop, items, labels, sectorFilter);
            PlaceBlock(allSeats, row, SeatBlock.Right, rightX - curveOffset, rowY, rowIndex, gridTop, items, labels, sectorFilter);
        }

        var stage = new StageArc(stageLeft, stageRight, stageBaseY, stagePeakY, stageCenterX);

        return new Layout(width, height, stage, gridLeft, gridTop, gridWidth, items, labels);
    }

    private static void PlaceBlock(
        List<SeatModel> allSeats,
        string row,
        SeatBlock block,
        double startX,
        double rowY,
        int rowIndex,
        double gridTop,
        List<RenderItem> items,
        List<TextLabel> labels,
        string sectorFilter)
    {
        var blockSeats = allSeats
            .Where(s => s.RowLabel == row && s.Block == block)
            .OrderBy(s => s.SeatNumber)
            .ToList();

        for (var i = 0; i < blockSeats.Count; i++)
        {
            var seat = blockSeats[i];
            if (!MatchesFilter(seat, sectorFilter)) continue;

            var x = startX + i * (SeatSize + SeatGap);
            items.Add(new RenderItem(seat, x, rowY));

            if (rowIndex == 0)
            {
                labels.Add(new TextLabel(
                    seat.GlobalSeatNumber.ToString(),
                    x + SeatSize / 2,
                    gridTop + 10,
                    "t360-col-label"));
            }
        }
    }

    internal static string StagePath(StageArc stage)
    {
        var l = F(stage.Left);
        var r = F(stage.Right);
        var b = F(stage.BaseY);
        var p = F(stage.PeakY);
        var c = F(stage.CenterX);
        return $"M {l} {b} Q {c} {p} {r} {b} L {r} {b} L {l} {b} Z";
    }

    internal static string StageFrontLine(StageArc stage)
    {
        var l = F(stage.Left);
        var r = F(stage.Right);
        var b = F(stage.BaseY);
        var p = F(stage.PeakY);
        var c = F(stage.CenterX);
        return $"M {l} {b} Q {c} {p} {r} {b}";
    }

    private static string F(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    private static bool MatchesFilter(SeatModel seat, string sectorFilter) =>
        sectorFilter == "Todos" || seat.Setor.Equals(sectorFilter, StringComparison.OrdinalIgnoreCase);
}
