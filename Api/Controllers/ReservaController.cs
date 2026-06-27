using Application.DTOs;
using Application.Interfaces;
using Domain.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservaController : ControllerBase
{
    private readonly IReservaService _service;
    private readonly IReservaRepository _repository;

    public ReservaController(IReservaService service, IReservaRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT em vez de cpf.
    /// </summary>
    [HttpPost("criar")]
    [Authorize]
    public async Task<IActionResult> CriarReserva([FromBody] ReservarDTO dto, CancellationToken ct)
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Não foi possível identificar o usuário logado." });
        }

        try
        {
            var id = await _service.FazerReserva(userId, dto, ct);
            return Ok(new { reservaId = id, message = "Reserva realizada com sucesso!" });
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpGet("minhas")]
    [Authorize]
    public async Task<IActionResult> ListarMinhasReservas(CancellationToken ct)
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Não foi possível identificar o usuário." });
        }

        var reservas = await _service.ListarMinhasReservas(userId, ct);
        
        return Ok(reservas);
    }

    [HttpGet("Admin/Todas")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarTodasAdmin(CancellationToken ct)
    {
        var reservas = await _repository.ListarTodasDetalhadasAdmin(ct);
        return Ok(reservas);
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpGet("minhas-vendas")]
    [Authorize(Roles = "Vendedor")]
    public async Task<IActionResult> ListarMinhasVendas(CancellationToken ct)
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Não foi possível identificar o vendedor." });

        var vendas = await _service.ListarVendasDoVendedor(userId, ct);
        return Ok(vendas);
    }
}