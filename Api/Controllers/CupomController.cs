using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

    private Guid GetAdminIdFromJwt()
    {
        var perfilIdStr = User.Claims.FirstOrDefault(c => c.Type == "perfilId")?.Value;
        if (string.IsNullOrEmpty(perfilIdStr) || !Guid.TryParse(perfilIdStr, out var perfilId))
            throw new UnauthorizedAccessException("Token inválido: perfil não identificado.");
        return perfilId;
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("CadastrarCupom")]

    public async Task<IActionResult> CadastrarCupom([FromBody] CadastrarCupomDTO dto, CancellationToken ct)
    {
       var adminId = GetAdminIdFromJwt();
       await _service.CadastrarCupom(dto, ct, adminId);

       return Ok(new {message ="Cupom Cadastrado com sucesso"});
    }


    [Authorize(Roles = "Admin")]
    [HttpDelete("DeletarCupom/{codigo}")]

    public async Task<IActionResult> DeletarCupom(
        [FromRoute] string codigo,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromJwt();
        await _service.DeletarCupom(adminId, codigo, ct);

        return Ok(new { message = $"Cupom '{codigo.ToUpper().Trim()}' excluído com sucesso." });
    }


    [Authorize(Roles = "Admin")]
    [HttpPatch("{codigo}/ValorMinimo")]

    public async Task<IActionResult> AlterarValorMinimo(
        [FromRoute] string codigo,
        [FromBody] AlterarValorMinimoDTO dto,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromJwt();
        await _service.AlterarValorMinimo(adminId, codigo, dto.NovoValor, ct);

        return Ok(new { message = "Valor mínimo do cupom alterado com sucesso!" });
    }


    [Authorize(Roles = "Admin")]
    [HttpPatch("{codigo}/DataExpiracao")]

    public async Task<IActionResult> AlterarDataExpiracao(
        [FromRoute] string codigo,
        [FromBody] AlterarDataExpiracaoDTO dto,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromJwt();
        await _service.AlterarDataVencimento(adminId, codigo, dto.novaData, ct);

        return Ok(new { message = "Data de expiração do cupom alterado com sucesso!" });
    }


    [Authorize(Roles = "Admin")]
    [HttpPatch("{codigo}/AlternarStatus")]
    public async Task<IActionResult> AlternarStatus(
        [FromRoute] string codigo,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromJwt();
        await _service.AlternarStatusCupom(adminId, codigo, ct);

        return Ok(new { message = "Status do cupom alterado com sucesso!" });
    }


    [Authorize(Roles = "Admin")]
    [HttpPatch("{codigo}/AlterarDesconto")]
    public async Task<IActionResult> AlterarDesconto(
        [FromRoute] string codigo,
        [FromBody] AlterarDescontoDTO dto,
        CancellationToken ct)
    {
        var adminId = GetAdminIdFromJwt();
        await _service.AlterarDesconto(adminId, codigo, dto.novoDesconto, ct);

        return Ok(new { message = "Porcentagem de desconto do cupom alterado com sucesso!" });
    }


    [Authorize(Roles = "Admin")] 
    [HttpGet("ListarTodosCupons")]
    public async Task<IActionResult> ListarTodosCupons(CancellationToken ct)
    {
        var adminId = GetAdminIdFromJwt();
        var cupons = await _service.ListarTodosCupons(adminId, ct);
        return Ok(cupons);
    }


    [HttpGet("ListarCuponsValidos")]
    public async Task<IActionResult> ListarCuponsValidos(CancellationToken ct)
    {
        var cuponsValidos = await _service.ListarCuponsValidos(ct);
        
        return Ok(cuponsValidos);
    }

    [HttpGet("DebugClaims")]
    public IActionResult DebugClaims()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }

}
