using Web.Components.Features.SeatMap;
using Web.Models;

namespace Web.Mock;

/// <summary>
/// Dados fictícios para desenvolvimento e demonstração UI (sem API / banco).
/// </summary>
public static class PlateiaMockData
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
            Tipo = 0,
            Local = "Auditório Campus Quinta — UNIFESO",
        },
        new()
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Nome = "Workshop de UX — Palestra aberta",
            DataEvento = new DateTime(2026, 8, 22, 14, 0, 0),
            CapacidadeTotal = 80,
            PrecoPadrao = 0m,
            Tipo = 1,
            Gratuito = true,
            Local = "Sala 12 — Centro",
        },
        new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Nome = "Espetáculo Teatral — Noite de Estreia",
            DataEvento = new DateTime(2026, 9, 10, 20, 0, 0),
            CapacidadeTotal = 96,
            PrecoPadrao = 60m,
            Tipo = 0,
            Local = "Teatro Municipal",
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
