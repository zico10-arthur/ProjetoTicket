using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Application.Exceptions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

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

    public async Task<string> Login(LoginDTO dto, CancellationToken ct)
    {
        Usuario? logado = await _repository.BuscarEmail(dto.Email, ct);
        if(logado == null) throw new LoginErro();

        if (logado.Senha != dto.Senha) throw new LoginErro();
        
        // 2. Em vez de retornar o usuário, nós fabricamos o Crachá (Token) dele!
        return GerarTokenJwt(logado);
    }

    // --- NOVA FUNÇÃO PRIVADA PARA FABRICAR O TOKEN ---
    private string GerarTokenJwt(Usuario usuario)
    {
        // 1. Descobrimos quem ele é pelo Guid do Banco de Dados
        string role = usuario.PerfilId.ToString().ToUpper() switch
        {
            "A1A1A1A1-A1A1-A1A1-A1A1-A1A1A1A1A1A1" => "Admin",
            "B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2" => "Vendedor",
            "C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3" => "Comprador",
            _ => "Comprador" // Padrão caso não ache
        };

        // 2. Colocamos as informações dele dentro do Token
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Cpf.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nome),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, role),
            new Claim("PerfilId", usuario.PerfilId.ToString())
        };

        // 3. Assinamos o Token com uma chave de segurança (Em produção, isso fica no appsettings.json)
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("SuperChaveSecretaDoProjetoTicket2026!!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "ProjetoTicketAPI",
            audience: "ProjetoTicketWeb",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8), // Token vale por 8 horas
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    
    
}
