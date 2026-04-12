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