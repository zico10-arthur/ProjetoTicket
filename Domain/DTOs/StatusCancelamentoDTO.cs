namespace Domain.DTOs;

public record StatusCancelamentoDTO(
    Guid EventoId,
    string Nome,
    bool Gratuito,
    int TotalIngressosVendidos,
    int TotalReservasAtivas,
    bool ReembolsoNecessario,
    decimal ValorTotalReembolso,
    string Mensagem
);
