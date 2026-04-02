using Domain.Entities;

public class Usuario
{
    public string Cpf {get; private set;}

    public string Nome{get; private set;}

    public string Email{get; private set;}

    public Perfil Perfil {get;private set;}

    public Guid PerfilId {get;private set;}

    public string Senha {get; private set;}

    protected Usuario() { }
    public Usuario(string cpf, string nome, string email, Guid perfilId, string senha)
    {
        Cpf = cpf;
        Nome = nome;
        Email = email;
        PerfilId = perfilId;
        Senha = senha;
    }
}