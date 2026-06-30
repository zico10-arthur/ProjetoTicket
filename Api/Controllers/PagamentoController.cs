using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PagamentoController : ControllerBase
{
    private readonly IPagamentoService _service;

    public PagamentoController(IPagamentoService service)
    {
        _service = service;
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT em vez de cpf.
    /// </summary>
    [HttpPost("checkout/{reservaId:guid}")]
    [Authorize]
    public async Task<IActionResult> Checkout(Guid reservaId, [FromBody] CheckoutRequestDTO dto, CancellationToken ct)
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Usuário não identificado no token." });

        try
        {
            var result = await _service.ConfirmarCheckout(reservaId, userId, dto.Metodo, ct);
            return Ok(result);
        }
        catch (DomainException ex)
        {
            return ex.Message switch
            {
                "Reserva não encontrada." => NotFound(new { message = ex.Message }),
                "Evento não encontrado." => NotFound(new { message = ex.Message }),
                _ => BadRequest(new { message = ex.Message })
            };
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    [HttpGet("admin/todos")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarTodosAdmin(CancellationToken ct)
    {
        var pagamentos = await _service.ListarTodosAdmin(ct);
        return Ok(pagamentos);
    }
}