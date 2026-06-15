using Domain.Exceptions;

namespace Application.Exceptions;

public class UsuarioInexistente : DomainException
{
    public UsuarioInexistente() : base("Usuário não encontrado no sistema.") {}
}

public class EventoInexistente : DomainException
{
    public EventoInexistente() : base("Evento não encontrado no sistema.") {}
}

public class ReservaDuplicada : DomainException
{
    public ReservaDuplicada() 
        : base("Você já possui uma reserva ativa para este evento!") {}
}

public class IngressosEsgotados : DomainException
{
    public IngressosEsgotados() 
        : base("Desculpe, todos os ingressos para este evento já foram vendidos ou reservados.") {}
}

public class IngressoInexistente : DomainException
{
    public IngressoInexistente() 
        : base("Este ingresso não existe no sistema!") {}
}

public class IngressoIndisponivel : DomainException
{
    public IngressoIndisponivel() 
        : base("Desculpe, este ingresso ja esta reservado ou vendido!") {}
}

public class CupomNaoAplicavelEventoGratuito : DomainException
{
    public CupomNaoAplicavelEventoGratuito()
        : base("Cupom não aplicável em evento gratuito.") {}
}

public class IngressoNaoPertenceAoEvento : DomainException
{
    public IngressoNaoPertenceAoEvento()
        : base("Ingresso não pertence a este evento.") {}
}

public class CpfJaReservadoNoEvento : DomainException
{
    public CpfJaReservadoNoEvento(string cpf)
        : base($"CPF {cpf} já possui reserva neste evento.") {}
}

public class AssentoNaoDisponivel : DomainException
{
    public AssentoNaoDisponivel()
        : base("Um ou mais assentos não estão mais disponíveis.") {}
}