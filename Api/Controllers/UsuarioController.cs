using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _service;

    public UsuarioController(IUsuarioService service)
    {
        _service = service;
    }

    [HttpPost("CadastrarComprador")]

    public async Task<IActionResult> CadastrarComprador(CadastrarUsuarioDTO dto, CancellationToken ct)
    {
       await _service.CadastrarComprador(dto,ct);

       return Ok(new {message ="Usuário Cadastrado com sucesso"});
    }

    [Authorize(Roles = "Admin")]

    [HttpPost("CadastrarVendedor/{Id}")]

    public async Task<IActionResult> CadastrarVendedor([FromBody] CadastrarUsuarioDTO dto,[FromRoute] Guid Id, CancellationToken ct)
    {
        await _service.CadastrarVendedor(dto, ct, Id);

        return Ok(new {message ="Usuário Cadastrado com sucesso"});
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto, CancellationToken ct)
    {
        var token = await _service.Login(dto,ct);

        return Ok(new{token = token});
    }

    
    [Authorize(Roles = "Admin")]

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

    
}