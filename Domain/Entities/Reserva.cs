using Domain.Exceptions;
using System;

namespace Domain.Entities;

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

    public static Reserva Criar(string usuarioCpf, Evento evento, Ingresso ingresso, Cupom? cupom = null)
    {
        decimal valorFinal = ingresso.Preco;
        string? codigoCupom = null;

        if (cupom != null)
        {
            ValidarUsoDoCupom(cupom, valorFinal);

            decimal desconto = valorFinal * (cupom.PorcentagemDesconto / 100m);
            valorFinal = valorFinal - desconto;
            
            codigoCupom = cupom.Codigo;
        }

        Reserva reserva = new Reserva(usuarioCpf, evento.id, ingresso.Id, codigoCupom, valorFinal);

        reserva.ValidarCpf(usuarioCpf);
        if (!DigitosSaoValidos(usuarioCpf)) throw new CpfInvalido();

        return reserva;
    }


    private void ValidarCpf(string cpf)
    {

        if (string.IsNullOrWhiteSpace(cpf))
                throw new CpfVazio();
        
        if (cpf.Length != 11)
                throw new CpfInvalido();

        if (cpf.Distinct().Count() == 1)
                throw new CpfInvalido();

    }

    private static bool DigitosSaoValidos(string cpf)
    {
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += (cpf[i] - '0') * (10 - i);

        int resto = soma % 11;
        int digito1 = resto < 2 ? 0 : 11 - resto;

        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += (cpf[i] - '0') * (11 - i);

        resto = soma % 11;
        int digito2 = resto < 2 ? 0 : 11 - resto;

        return cpf[9] - '0' == digito1 &&
                cpf[10] - '0' == digito2;
    }

    private static void ValidarUsoDoCupom(Cupom cupom, decimal precoBase)
    {
        if (!cupom.EstaValidoParaUso)
            throw new CupomInvalidoParaUso();

        if (precoBase < cupom.ValorMinimo)
            throw new ValorMinimoCupomExcedido(cupom.ValorMinimo);
    }
}