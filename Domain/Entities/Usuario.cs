using Domain.Entities;
using Domain.Exceptions;
using System.Net.Mail;
public class Usuario
{
    public string Cpf {get; private set;} = string.Empty;

    public string Nome{get; private set;} = string.Empty;

    public string Email{get; private set;} = string.Empty;

    public Perfil Perfil {get;  set;}

    public Guid PerfilId {get;private set;}

    public string Senha {get; private set;} 

    public Usuario() {}

    public Usuario(string cpf, string nome, string email, Guid perfilid, string senha)
    {
        
        Cpf = cpf;
        Nome = nome;
        Email = email;
        PerfilId = perfilid;
        Senha = senha;
    }

    public static Usuario Criar(string cpf, string nome, string email, Guid perfilid, string senha)
{
    cpf = (cpf ?? string.Empty).Replace(".", "").Replace("-", "").Trim();

    Usuario usuario = new Usuario(cpf, nome, email, perfilid, senha);

    usuario.ValidarNome(nome);
    usuario.ValidarCpf(cpf);
    if (!DigitosSaoValidos(cpf)) throw new CpfInvalido();
    usuario.ValidarEmail(email);
    usuario.ValidarSenha(senha);

    return usuario;
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

    private void ValidarNome(string nome)
    {

         if (string.IsNullOrWhiteSpace(nome))
            {
                throw new NomeVazio();
            }
                
        nome = nome.Trim();

        
         if (nome.Length < 3)
                throw new NomeInvalido();

            if (!nome.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\''))
                throw new NomeInvalido();
            if (nome.All(c => c == nome[0]))
            {
                throw new NomeInvalido();
            }
    }

    private void ValidarEmail(string email)
    {

        if (string.IsNullOrWhiteSpace(email)) throw new EmailVazio();

        email = email.Trim();


        try 
    {
        var addr = new MailAddress(email);
        Email = addr.Address;
    }
    catch
    {
        throw new EmailInvalido();
    }
    }

    private void ValidarSenha(string senha)
    {
        if (string.IsNullOrWhiteSpace(senha)) throw new SenhaVazia();

        senha = senha.Trim();


        if (senha.Length < 8) throw new Senha8digitos();

        if (!senha.Any(char.IsLetter) ||
            !senha.Any(char.IsDigit) ||
            !senha.Any(c => !char.IsLetterOrDigit(c)))
            {
                throw new SenhaInvalida();
            }
    }
 
    
}