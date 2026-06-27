using Web.Components.Features.SeatMap;
using Web.Models;

namespace Web.Mock;

/// <summary>
/// Dados fictícios para desenvolvimento e demonstração UI (sem API / banco).
/// </summary>
public static class TicketPrimeMockData
{
    public static readonly Guid EventoUnifesoId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string EventoUnifesoNome = "Formatura UNIFESO 2026 — Campus Quinta";

    public static List<EventoViewModel> GetEventos() =>
    [
        new()
        {
            Id = EventoUnifesoId,
            Nome = EventoUnifesoNome,
            DataEvento = new DateTime(2026, 12, 15, 19, 0, 0),
            CapacidadeTotal = 96,
            PrecoPadrao = 45m,
        },
        new()
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Nome = "Seminário de Tecnologia — Auditório Quinta",
            DataEvento = new DateTime(2026, 8, 22, 14, 0, 0),
            CapacidadeTotal = 96,
            PrecoPadrao = 35m,
        },
    ];

    public static List<SeatModel> GetAssentos(MockCenarioAssentos cenario = MockCenarioAssentos.Padrao) =>
        cenario switch
        {
            MockCenarioAssentos.QuaseLotado => QuintaAuditorioSeatData.CreateAlmostFullLayout(),
            MockCenarioAssentos.MuitosLivres => QuintaAuditorioSeatData.CreateSparseLayout(),
            _ => QuintaAuditorioSeatData.CreateDemoLayout(),
        };
}

public enum MockCenarioAssentos
{
    Padrao,
    QuaseLotado,
    MuitosLivres,
}
