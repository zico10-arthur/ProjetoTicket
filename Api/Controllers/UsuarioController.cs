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



    
}