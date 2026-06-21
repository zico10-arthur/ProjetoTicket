using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Application.Exceptions;
using Domain.Entities;
using Domain.Validators;
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

        // ST-08: Validar senha bruta antes do hash
        Usuario.ValidarSenhaBruta(dto.Senha);

        // ST-08: Hash da senha com BCrypt
        string senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

        Usuario? novocomprador = Usuario.CriarComprador(dto.Cpf, dto.Nome, dto.Email, senhaHash);
        _repository.CadastrarUsuario(novocomprador);
    }

    /// <summary>
    /// ST-01: Auto cadastro público de vendedor.
    /// </summary>
    public async Task<VendedorCadastradoDTO> CadastrarVendedor(CadastrarVendedorDTO dto, CancellationToken ct)
    {
        // 1. Validar CNPJ
        CnpjValidator.Validar(dto.Cnpj);

        // 2. Verificar unicidade de CNPJ e Email
        Usuario? existente = await _repository.BuscarCnpjOuEmail(dto.Cnpj, dto.Email, ct);
        if (existente != null)
        {
            var cnpjLimpo = dto.Cnpj.Replace(".", "").Replace("-", "").Replace("/", "").Trim();
            if (existente.Cnpj == cnpjLimpo)
                throw new CnpjJaCadastrado();
            throw new EmailJaCadastrado();
        }

        // 3. Validar senha bruta (antes do hash)
        Usuario.ValidarSenhaBruta(dto.Senha);

        // 4. Hash da senha com BCrypt
        string senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.Senha);

        // 5. Criar entidade (factory method)
        Usuario vendedor = Usuario.CriarVendedor(
            dto.Cnpj, dto.RazaoSocial, dto.NomeFantasia,
            dto.Email, senhaHash, dto.Telefone);

        // 6. Persistir
        _repository.CadastrarVendedor(vendedor);

        // 7. Retornar resposta
        return new VendedorCadastradoDTO
        {
            Cpf = vendedor.Cpf,
            Nome = vendedor.Nome,
            NomeFantasia = vendedor.NomeFantasia,
            Email = vendedor.Email,
            Perfil = "Vendedor",
            Plano = "Gratuito"
        };
    }

    /// <summary>
    /// ST-08: Login unificado — BCrypt.Verify + verificação de Ativo + LoginResponseDTO.
    /// Spec 120: SaltParseException catch, mensagem uniforme, resposta 401 padronizada.
    /// </summary>
    public async Task<LoginResponseDTO> Login(LoginDTO dto, CancellationToken ct)
    {
        Usuario? logado = await _repository.BuscarEmail(dto.Email, ct);
        if (logado == null) throw new LoginErro();

        try
        {
            if (!BCrypt.Net.BCrypt.Verify(dto.Senha, logado.Senha))
                throw new LoginErro();
        }
        catch (BCrypt.Net.SaltParseException)
        {
            throw new LoginErro();
        }

        if (!logado.Ativo)
            throw new LoginErro();

        // 8.2.4 Gerar JWT
        var token = _tokenservice.GerarToken(logado);

        // 8.2.5 Obter nome do perfil
        var perfilId = logado.PerfilId.ToString().ToUpper();
        var perfil = perfilId switch
        {
            "A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1" => "Admin",
            "B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2" => "Vendedor",
            _ => "Comprador"
        };

        return new LoginResponseDTO
        {
            Token = token,
            Usuario = new UsuarioLoginDTO
            {
                Cpf = logado.Cpf,
                Nome = logado.Nome,
                Email = logado.Email,
                Perfil = perfil
            }
        };
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

        // ST-08: Validar e hashear a nova senha com BCrypt
        Usuario.ValidarSenhaBruta(dto.NovaSenha);
        string senhaHash = BCrypt.Net.BCrypt.HashPassword(dto.NovaSenha);

        usuario.AlterarSenha(senhaHash);
        await _repository.AtualizarSenha(cpf, senhaHash, ct);
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
