public class Usuario
{
    public string Cpf {get; private set;}

    public string Nome{get; private set;}

    public string Email{get; private set;}

    public Usuario(string cpf, string nome, string email)
    {
        Cpf = cpf;
        Nome = nome;
        Email = email;
    }
}