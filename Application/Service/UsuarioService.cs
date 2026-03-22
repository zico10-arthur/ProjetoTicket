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

    public async Task CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct)
    {
        Usuario? cpfbuscado = await  _repository.BuscarCpfOuEmail(dto.Cpf, dto.Email, ct);
        if (cpfbuscado != null) throw new UsuarioCadastrado();

        Guid idComprador = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");
        Usuario? novocomprador = Usuario.Criar(dto.Cpf, dto.Nome, dto.Email, idComprador, dto.Senha);
        _repository.CadastrarUsuario(novocomprador);
    }

    public async Task CadastrarVendedor(CadastrarUsuarioDTO dto, CancellationToken ct, Guid AdminLogado)
    {
        Usuario? responsavel = await _repository.BuscarId(AdminLogado, ct);

        Guid PerfilAdmin = Guid.Parse("A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1");

        if (responsavel == null || responsavel.PerfilId != PerfilAdmin) throw new UsuarioNaoAutorizado();
        
        Usuario? cpfbuscado = await  _repository.BuscarCpfOuEmail(dto.Cpf, dto.Email, ct);
        if (cpfbuscado != null) throw new UsuarioCadastrado();

        Guid IdVendedor = Guid.Parse("B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2");
        Usuario? novovendedor =  Usuario.Criar(dto.Cpf, dto.Nome, dto.Email, IdVendedor, dto.Senha);
        _repository.CadastrarUsuario(novovendedor);
    }
}
