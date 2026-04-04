using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Application.Exceptions;
using AutoMapper;
namespace Application.Service;

public class UsuarioService : IUsuarioService
{
    private readonly IUsuarioRepository _repository;
    private readonly IMapper _mapper;

    private readonly ITokenService _tokenservice;
    public UsuarioService(IUsuarioRepository repository, IMapper mapper, ITokenService tokenservice)
    {
        _repository = repository;
        _mapper = mapper;
        _tokenservice = tokenservice;
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

    public async Task<string> Login(LoginDTO dto, CancellationToken ct)
    {
        Usuario? logado = await _repository.BuscarEmail(dto.Email, ct);
        if(logado == null) throw new LoginErro();

        if (logado.Senha != dto.Senha) throw new LoginErro();
        var token = _tokenservice.GerarToken(logado);
        return token;
    }

    
    public async Task<UsuarioSaidaDTO> UsuarioEspecifico(string cpf, CancellationToken ct)
    {
        Usuario? usuario = await _repository.BuscarCpf(cpf, ct);
        if (usuario == null) throw new UsuarioNotFound();

        UsuarioSaidaDTO dto = _mapper.Map<UsuarioSaidaDTO>(usuario);
        return dto;
    }

    public async Task RemoverUsuario(string cpf, CancellationToken ct)
    {
        Usuario? usuario = await _repository.BuscarCpf(cpf, ct);
        if (usuario == null) throw new UsuarioNotFound();

       await _repository.RemoverUsuario(usuario, ct);
    }

    public async Task AlterarSenha( AlterarSenhaDTO dto, string cpf, CancellationToken ct)
    {
         Usuario? usuario = await _repository.BuscarCpf(cpf, ct);
        if (usuario == null) throw new UsuarioNotFound();

        usuario.AlterarSenha(dto.NovaSenha);
        await _repository.AtualizarSenha(cpf, dto.NovaSenha, ct);
    }

    
    
}
