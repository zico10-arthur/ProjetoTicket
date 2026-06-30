namespace Application.DTOs;

/// <summary>
/// Spec 200: string UsuarioCpf → string UsuarioId (Guid como string para exibição).
/// </summary>
public record PagamentoAdminDTO(
    Guid PagamentoId,
    Guid ReservaId,
    string UsuarioId,
    string EventoNome,
    decimal ValorPago,
    string Status,
    string Metodo,
    DateTime DataPagamento
);