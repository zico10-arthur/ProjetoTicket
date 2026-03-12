using Domain.Entities;

public class Usuario
{
    public string Cpf {get; private set;}

    public string Nome{get; private set;}

    public string Email{get; private set;}

    public Perfil Perfil {get;private set;}

    public Guid PerfilId {get;private set;}

    public Usuario(string cpf, string nome, string email, Guid perfilId)
    {
        Cpf = cpf;
        Nome = nome;
        Email = email;
        PerfilId = perfilId;
    }
}