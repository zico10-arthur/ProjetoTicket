namespace Domain.Entities;

public class Reserva
{
    public Guid Id { get; private set; } = Guid.NewGuid();

<<<<<<< HEAD
    public string UsuarioCpf { get; private set; }
=======
    public Usuario Usuario { get; private set; }
>>>>>>> branch-dudu

    public Guid EventoId { get; private set; }

    public string CupomUtilizado { get; private set; }

    public decimal ValorFinalPago { get; private set; }

    public Reserva(string usuariocpf, Guid eventoid, string cupomutilizado, decimal valorfinalpago)
    {
        UsuarioCpf = usuariocpf;
        EventoId = eventoid;
        CupomUtilizado = cupomutilizado;
        ValorFinalPago = valorfinalpago;
    }
}
