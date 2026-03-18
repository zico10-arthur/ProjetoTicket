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

       return Ok(new {message ="Sucesso!"});
    }

    [HttpPost("CadastrarVendedor/{Id}")]

    public async Task<IActionResult> CadastrarVendedor([FromBody] CadastrarUsuarioDTO dto,[FromRoute] Guid Id, CancellationToken ct)
    {
        await _service.CadastrarVendedor(dto, ct, Id);

        return Ok(new {message ="Sucesso!"});
    }
}