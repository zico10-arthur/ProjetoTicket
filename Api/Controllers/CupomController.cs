using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class CupomController : ControllerBase
{
    private readonly ICupomService _service;

    public CupomController(ICupomService service)
    {
        _service = service;
    }

    [HttpPost("CadastrarCupom")]

    public async Task<IActionResult> CadastrarCupom(CadastrarCupomDTO dto, CancellationToken ct)
    {
       await _service.CadastrarCupom(dto,ct);

       return Ok(new {message ="Cupom Cadastrado com sucesso"});
    }
}
