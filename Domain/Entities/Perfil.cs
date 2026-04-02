namespace Domain.Entities;

public class Perfil
{
    public Guid Id {get;private set;} =  Guid.NewGuid();

    public string Nome {get;private set;}

    public List<Usuario> Usuarios {get;private set;} = new();

    private Perfil(){}

    public Perfil(string nome)
    {
        Nome = nome;
    }
}
