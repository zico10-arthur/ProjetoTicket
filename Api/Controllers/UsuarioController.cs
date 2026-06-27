using Application.DTOs;
using Application.Interfaces;
using Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain.Interface;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;
    private readonly IUsuarioRepository _repository;

    public UsuarioController(IUsuarioService service, IUsuarioRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpPost("CadastrarComprador")]

    public async Task<IActionResult> CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct)
    {
       await _service.CadastrarComprador(dto,ct);

       return Ok(new {message ="Usuário Cadastrado com sucesso"});
    }

    /// <summary>
    /// ST-01: Auto cadastro público de vendedor.
    /// </summary>
    [HttpPost("cadastrar-vendedor")]
    public async Task<IActionResult> CadastrarVendedor([FromBody] CadastrarVendedorDTO dto, CancellationToken ct)
    {
        var resultado = await _service.CadastrarVendedor(dto, ct);
        return CreatedAtAction(nameof(CadastrarVendedor), resultado);
    }

    /// <summary>
    /// ST-08: Login unificado — um endpoint para todos os perfis.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto, CancellationToken ct)
    {
        var resposta = await _service.Login(dto, ct);
        return Ok(resposta);
    }

    

    [HttpGet("ListarUsuarioEspecifico/{cpf}")]
    public async Task<IActionResult> ListarUsuarioEspecifico([FromRoute]string cpf, CancellationToken ct)
    {
        UsuarioSaidaDTO dto = await _service.UsuarioEspecifico(cpf, ct);

        return Ok(dto);
    }

    [HttpDelete("DeletarUsuario/{cpf}")]

    public async Task<IActionResult> RemoverUusuario([FromRoute]string cpf, CancellationToken ct)
    {
        await _service.RemoverUsuario(cpf, ct);

        return Ok(new{message = "Usuario deletado com sucesso"});
    }

    [HttpPut("alterarsenha/{cpf}")]

    public async Task<IActionResult> AlterarSenha([FromRoute]string cpf, [FromBody] AlterarSenhaDTO dto, CancellationToken ct)
    {
        await _service.AlterarSenha(dto, cpf, ct);

        return Ok(new{message = "senha alterada com sucesso"});
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("Todos")]
    public async Task<IActionResult> ListarTodos(CancellationToken ct)
    {
        var usuarios = await _repository.ListarTodos(ct);
        return Ok(usuarios);
    }

    [HttpPut("alterarnome/{cpf}")]

    public async Task<IActionResult> AlterarNome([FromRoute] string cpf, [FromBody] AlterarNomeDTO dto, CancellationToken ct )
    {
        await _service.AlterarNomeAsync(cpf, dto, ct);

        return Ok(new{message = "Nome alterado com sucesso"});
    }

    [HttpPut("alteraremail/{cpf}")]

    public async Task<IActionResult> AlterarEmail([FromRoute] string cpf, [FromBody] AlterarEmailDTO dto, CancellationToken ct )
    {
        await _service.AlterarEmailAsync(cpf, dto, ct);

        return Ok(new{message = "Email alterado com sucesso"});
    }

    /// <summary>
    /// Spec 180: Solicitar redefinição de senha. Sempre retorna 200 OK.
    /// </summary>
    [HttpPost("esqueci-senha")]
    public async Task<IActionResult> EsqueciSenha([FromBody] EsqueciSenhaDTO dto, CancellationToken ct)
    {
        await _service.SolicitarRedefinicaoSenha(dto.Email, ct);
        return Ok(new { message = "Se o e-mail estiver cadastrado, um link de redefinição será enviado." });
    }

    /// <summary>
    /// Spec 180: Redefinir senha com token JWT.
    /// </summary>
    [HttpPost("redefinir-senha")]
    public async Task<IActionResult> RedefinirSenha([FromBody] RedefinirSenhaDTO dto, CancellationToken ct)
    {
        try
        {
            await _service.RedefinirSenha(dto.Token, dto.NovaSenha, ct);
            return Ok(new { message = "Senha redefinida com sucesso." });
        }
        catch (TokenRedefinicaoInvalido ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Domain.Exceptions.SenhaInvalida ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Domain.Exceptions.Senha8digitos ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Domain.Exceptions.SenhaVazia ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}