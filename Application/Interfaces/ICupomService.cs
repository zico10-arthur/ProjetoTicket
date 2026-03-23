using Application.DTOs;

namespace Application.Interfaces;

public interface ICupomService
{
    Task CadastrarCupom(CadastrarCupomDTO dto, CancellationToken ct);
}
