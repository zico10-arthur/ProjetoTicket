using Domain.Entities;
using Domain.Exceptions;
public class Usuario
{
    public string Cpf {get; private set;} = string.Empty;

    public string Nome{get; private set;} = string.Empty;

    public string Email{get; private set;} = string.Empty;

    public Perfil Perfil {get;private set;}

    public Guid PerfilId {get;private set;}

    public Usuario(string cpf, string nome, string email)
    {
        ValidarCpf(cpf);
        if (!DigitosSaoValidos(cpf)) throw new CpfInvalido();
        
        Cpf = cpf;
        Nome = nome;
        Email = email;
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
         public string Formatado =>
            $"{Cpf[..3]}.{Cpf.Substring(3, 3)}.{Cpf.Substring(6, 3)}-{Cpf.Substring(9, 2)}";

        public override string ToString() => Formatado;

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
 
    
}