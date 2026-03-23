using Domain.Entities;
using Domain.Exceptions;
using System.Net.Mail;

public class Cupom
{
    public Guid Id {get; private set; }
    public Guid? IdEvento { get; private set; }
    public string Codigo { get; private set; }
    public int PorcentagemDesconto { get; private set; }
    public decimal ValorMinimo { get; private set; }
    public DateTime? DataExpiracao { get; private set; }
    public bool Ativo { get; private set; }

    // Construtor para o Dapper não reclamar da "materialization"
    protected Cupom() { }

    private Cupom(string codigo, int percentDesc, decimal valorMin, DateTime? expiracao, Guid? idEvento = null)
    {
        Id = Guid.NewGuid();
        IdEvento = idEvento;
        Codigo = codigo.ToUpper().Trim();
        PorcentagemDesconto = percentDesc;
        ValorMinimo = valorMin;
        DataExpiracao = expiracao;
        Ativo = true;
    }


    public static Cupom Criar(string codigo, int percentDesc, decimal valorMin, DateTime? expiracao, Guid? idEvento = null)
    {
        if (expiracao < DateTime.Now) throw new DataExpiracaoInvalida(); 
        if (valorMin <= 0) throw new ValorMinimoInvalido();

        Cupom cupom = new Cupom(codigo, percentDesc, valorMin, expiracao, idEvento);

        cupom.ValidarCodigo(codigo);
        cupom.ValidarDesconto(percentDesc);
        
        return cupom;
    }


    public bool EstaAtivo() 
    {
        return Ativo;
    }


    public void CupomExpirou(DateTime expiracao)
    {
        if (expiracao < DateTime.Now) throw new CupomExpirado();
    }


    public void ValidarCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
                throw new CodigoVazio();

        if (codigo.Length >= 50 || codigo.Length < 6)
                throw new TamanhoCodigoInvalido();

        if (codigo.Any(char.IsLetter) &&
            codigo.Any(char.IsDigit) &&
            !codigo.Any(c => !char.IsLetterOrDigit(c)))
        {
            int quantDigitos = 0;

            foreach (char c in codigo)
            {
                if (char.IsDigit(c))
                    quantDigitos++;
                if (quantDigitos > 3)
                    throw new FormatoCodigoInvalido();
            }
        }
        else
        {
            throw new FormatoCodigoInvalido();
        }
    }


    public void ValidarDesconto(int percentDesc)
    {
        if (percentDesc <= 0 || percentDesc > 100)
            throw new DescontoInvalido();
    }

}
