using Domain.Entities;

public class Reserva
{
     public Guid Id { get; private set; } = Guid.NewGuid();
    public string UsuarioCpf { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid IngressoId { get; private set; }
    public string? CupomUtilizado { get; private set; }
    public decimal ValorFinalPago { get; private set; }

    protected Reserva() { }

    private Reserva(string usuarioCpf, Guid eventoId, Guid ingressoId, string? cupomUtilizado, decimal valorFinalPago)
    {
        UsuarioCpf = usuarioCpf;
        EventoId = eventoId;
        IngressoId = ingressoId;
        CupomUtilizado = cupomUtilizado;
        ValorFinalPago = valorFinalPago;
    }
}