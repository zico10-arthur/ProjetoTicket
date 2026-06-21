namespace Application.DTOs;

public record PagamentoAdminDTO(
    Guid PagamentoId,
    Guid ReservaId,
    string UsuarioCpf,
    string EventoNome,
    decimal ValorPago,
    string Status,
    string Metodo,
    DateTime DataPagamento
);
