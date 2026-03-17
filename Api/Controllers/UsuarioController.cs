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

    public async Task<IActionResult> CadastrarUsuario(CadastrarUsuarioDTO dto, CancellationToken ct)
    {
       await _service.CadastrarUsuario(dto,ct);

       return Ok(new {message ="Sucesso!"});
    }

}