using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

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

    [HttpPost("CadastrarVendedor/{Id}")]

    public async Task<IActionResult> CadastrarVendedor([FromBody] CadastrarUsuarioDTO dto,[FromRoute] Guid Id, CancellationToken ct)
    {
        await _service.CadastrarVendedor(dto, ct, Id);

        return Ok(new {message ="Usuário Cadastrado com sucesso"});
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDTO dto, CancellationToken ct)
    {
        await _service.Login(dto,ct);

        return Ok(new{message="Bem vindo!"});
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

    [HttpPut("AlterarEmail/{cpf}")]
    public async Task<IActionResult> AlterarEmail([FromRoute]string cpf,[FromBody] AlterarEmailDTO dto, CancellationToken ct)
    {
        await _service.AlterarEmailAsync(cpf, dto, ct);
        return Ok(new{menssage = "Email alterado com sucesso."});
    }

    [HttpPut("AlterarNome/{cpf}")]
    public async Task<IActionResult> AlterarNome([FromRoute]string cpf,[FromBody] AlterarNomeDTO dto, CancellationToken ct)
    {
        await _service.AlterarNomeAsync(cpf, dto, ct);
        return Ok(new{message = "Nome alterado com sucesso."});
    }

    [HttpGet("ListarUsuarios")]
    public async Task<IActionResult> ListarUsuarios(CancellationToken ct)
    {
        var usuarios = await _service.ListarUsuariosAsync(ct);
        return Ok(usuarios);
    }
}