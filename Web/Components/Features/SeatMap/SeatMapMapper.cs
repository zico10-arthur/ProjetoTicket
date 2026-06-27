using Application.DTOs;

namespace Web.Components.Features.SeatMap;

/// <summary>
/// Converte ingressos da API para o modelo do mapa. Ajuste o parsing conforme o contrato final da API.
/// </summary>
public static class SeatMapMapper
{
    public static List<SeatModel> FromIngressos(IEnumerable<IngressoResponseDTO> ingressos)
    {
        var list = new List<SeatModel>();
        var id = 1;

        foreach (var ingresso in ingressos.OrderBy(i => i.Posicao))
        {
            var (row, seatNum) = ParsePosicao(ingresso.Posicao);
            var block = seatNum <= 6 ? SeatBlock.Left : SeatBlock.Right;
            var seatInBlock = seatNum <= 6 ? seatNum : seatNum - 6;

            list.Add(new SeatModel
            {
                Id = id++,
                RowLabel = row,
                SeatNumber = seatInBlock,
                Block = block,
                Price = ingresso.Preco,
                Status = MapStatus(ingresso.Status),
            });
        }

        return list;
    }

    private static SeatStatus MapStatus(string status) =>
        status.Equals("Disponivel", StringComparison.OrdinalIgnoreCase)
        || status == "0"
            ? SeatStatus.Available
            : SeatStatus.Occupied;

    private static (string Row, int SeatNumber) ParsePosicao(string posicao)
    {
        var parts = posicao.Split('|', StringSplitOptions.TrimEntries);
        var rowPart = parts.Length > 0 ? parts[0] : "A";
        var seatPart = parts.Length > 1 ? parts[1] : "1";

        var row = rowPart.Replace("Fila", "", StringComparison.OrdinalIgnoreCase).Trim();
        if (string.IsNullOrEmpty(row)) row = "A";

        var seatDigits = new string(seatPart.Where(char.IsDigit).ToArray());
        _ = int.TryParse(seatDigits, out var seatNum);
        if (seatNum <= 0) seatNum = 1;

        return (row, seatNum);
    }
}
