namespace Application.DTOs;

public record CheckoutResponseDTO(
    Guid PagamentoId,
    Guid ReservaId,
    decimal ValorPago,
    string Status,
    DateTime DataPagamento,
    string Message
);
