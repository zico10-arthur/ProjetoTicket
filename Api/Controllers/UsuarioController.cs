using Application.DTOs;
using Application.Interfaces;
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
       try
    {
        var resultado = await _service.CadastrarVendedor(dto, ct);
        return CreatedAtAction(nameof(CadastrarVendedor), resultado);
    }
    catch (Exception ex)
    {
        return BadRequest(ex.ToString());
    }
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

    /// <summary>
    /// Spec 200: Rota usa {id:guid} em vez de {cpf}.
    /// </summary>
    [HttpGet("ListarUsuarioEspecifico/{id:guid}")]
    public async Task<IActionResult> ListarUsuarioEspecifico([FromRoute] Guid id, CancellationToken ct)
    {
        UsuarioSaidaDTO dto = await _service.UsuarioEspecifico(id, ct);

        return Ok(dto);
    }

    /// <summary>
    /// Spec 200: Rota usa {id:guid} em vez de {cpf}.
    /// </summary>
    [HttpDelete("DeletarUsuario/{id:guid}")]

    public async Task<IActionResult> RemoverUusuario([FromRoute] Guid id, CancellationToken ct)
    {
        await _service.RemoverUsuario(id, ct);

        return Ok(new{message = "Usuario deletado com sucesso"});
    }

    /// <summary>
    /// Spec 200: Rota usa {id:guid} em vez de {cpf}.
    /// </summary>
    [HttpPut("alterarsenha/{id:guid}")]

    public async Task<IActionResult> AlterarSenha([FromRoute] Guid id, [FromBody] AlterarSenhaDTO dto, CancellationToken ct)
    {
        await _service.AlterarSenha(dto, id, ct);

        return Ok(new{message = "senha alterada com sucesso"});
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("Todos")]
    public async Task<IActionResult> ListarTodos(CancellationToken ct)
    {
        var usuarios = await _repository.ListarTodos(ct);
        return Ok(usuarios);
    }

    /// <summary>
    /// Spec 200: Rota usa {id:guid} em vez de {cpf}.
    /// </summary>
    [HttpPut("alterarnome/{id:guid}")]

    public async Task<IActionResult> AlterarNome([FromRoute] Guid id, [FromBody] AlterarNomeDTO dto, CancellationToken ct )
    {
        await _service.AlterarNomeAsync(id, dto, ct);

        return Ok(new{message = "Nome alterado com sucesso"});
    }

    /// <summary>
    /// Spec 200: Rota usa {id:guid} em vez de {cpf}.
    /// </summary>
    [HttpPut("alteraremail/{id:guid}")]

    public async Task<IActionResult> AlterarEmail([FromRoute] Guid id, [FromBody] AlterarEmailDTO dto, CancellationToken ct )
    {
        await _service.AlterarEmailAsync(id, dto, ct);

        return Ok(new{message = "Email alterado com sucesso"});
    }


    
}