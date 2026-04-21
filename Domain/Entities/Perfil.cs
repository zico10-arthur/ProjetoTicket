namespace Domain.Entities;

public class Perfil
{
    public Guid Id {get; set;} =  Guid.NewGuid();

    public string Nome {get; set;}

    public Perfil() {}

    private Perfil(){}

    public Perfil(string nome)
    {
        Nome = nome;
    }
}
