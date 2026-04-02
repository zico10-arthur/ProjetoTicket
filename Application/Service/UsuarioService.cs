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
    public UsuarioService(IUsuarioRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
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

    public async Task<Usuario> Login(LoginDTO dto, CancellationToken ct)
    {
        Usuario? logado = await _repository.BuscarEmail(dto.Email, ct);
        if(logado == null) throw new LoginErro();

        if (logado.Senha != dto.Senha) throw new LoginErro();
        return logado;
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

        _repository.RemoverUsuario(usuario, ct);
    }

    public async Task AlterarSenha( AlterarSenhaDTO dto, string cpf, CancellationToken ct)
    {
         Usuario? usuario = await _repository.BuscarCpf(cpf, ct);
        if (usuario == null) throw new UsuarioNotFound();

        usuario.AlterarSenha(dto.NovaSenha);
        await _repository.AtualizarSenha(cpf, dto.NovaSenha, ct);
    }

    public async Task AlterarEmailAsync(string cpfBruto, AlterarEmailDTO dto, CancellationToken ct)
    {
        var cpfLimpo = cpfBruto.Replace(".","").Replace("-","").Trim();

        var usuario = await _repository.BuscarCpf(cpfLimpo,ct);
        if (usuario == null) throw new UsuarioNotFound();

        usuario.AlterarEmail(dto.NovoEmail);

        await _repository.AtualizarEmailAsync(usuario, ct);
    }
    
    public async Task AlterarNomeAsync(string cpf, AlterarNomeDTO dto, CancellationToken ct)
    {
        var cpfLimpo = cpf.Replace(".","").Replace("-","").Trim();

        var usuario = await _repository.BuscarCpf(cpfLimpo,ct);

        if(usuario == null)
            throw new UsuarioNotFound();

        usuario.AlterarNome(dto.NovoNome);

        await _repository.AtualizarNomeAsync(usuario,ct);
    }

    public async Task<IEnumerable<UsuarioResponseDTO>> ListarUsuariosAsync(CancellationToken ct)
    {
        var usuarios = await _repository.ListarTodosAsync(ct);

        return usuarios.Select(u => new UsuarioResponseDTO
        {
            Cpf = u.Cpf,
            Nome = u.Nome,
            Email = u.Email,
            Perfil = u.Perfil != null ? u.Perfil.Nome: "Sem Perfil"
        });
    }
}
