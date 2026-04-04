namespace Domain.Entities;

public class Perfil
{
    public Guid Id {get; set;} =  Guid.NewGuid();

    public string Nome {get; set;}

    public List<Usuario> Usuarios {get;private set;} = new();

    public Perfil() {}

    public Perfil(string nome)
    {
        Nome = nome;
    }
}
