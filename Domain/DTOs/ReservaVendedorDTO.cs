namespace Domain.DTOs;

/// <summary>
/// Spec 200: CompradorId (Guid) em vez de CpfComprador.
/// </summary>
public class ReservaVendedorDTO
{
    public Guid Id { get; set; }
    public string NomeEvento { get; set; } = string.Empty;
    public DateTime DataEvento { get; set; }
    public decimal ValorFinalPago { get; set; }
    public bool Pago { get; set; }
    public bool Reembolsada { get; set; }
    public string NomeComprador { get; set; } = string.Empty;
    public Guid CompradorId { get; set; }
}