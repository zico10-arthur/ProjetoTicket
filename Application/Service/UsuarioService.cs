using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Application.Exceptions;

namespace Application.Service;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;
    public UsuarioService(IUsuarioRepository repository)
    {
        _repository = repository;
    }

    public async Task CadastrarUsuario(CadastrarUsuarioDTO dto, CancellationToken ct)
    {
        Usuario? cpfbuscado = await  _repository.BuscarCpfOuEmail(dto.Cpf, dto.Email, ct);
        if (cpfbuscado != null) throw new UsuarioCadastrado();

        Guid idComprador = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");
        Usuario? novousuario = new Usuario(dto.Cpf, dto.Nome, dto.Email, idComprador);
        _repository.CadastrarUsuario(novousuario);
    }
}
