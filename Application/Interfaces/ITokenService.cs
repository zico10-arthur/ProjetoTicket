namespace Application.Interfaces;

public interface ITokenService
{
     string GerarToken(Domain.Entities.Usuario usuario);
}
