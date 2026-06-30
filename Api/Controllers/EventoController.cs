using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EventoController : ControllerBase
{
    private readonly IEventoService _eventoService;

    public EventoController(IEventoService eventoService)
    {
        _eventoService = eventoService;
    }

    [HttpGet]
    public async Task<ActionResult<EventoResponseDTO>> GetAllAsync()
    {
        try
        {
            var eventos = await _eventoService.GetAllAsync();
            return Ok(eventos);
        }
        catch (KeyNotFoundException erro) { return NotFound(erro.Message); }
        catch (Exception erro) { return StatusCode(500, $"Evento não encontrado | {erro.Message}"); }
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpGet("meus")]
    [Authorize(Roles = "Vendedor")]
    public async Task<ActionResult<EventoResponseDTO>> GetMeusEventosAsync()
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var eventos = await _eventoService.GetAllByVendedorAsync(userId);
        return Ok(eventos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<EventoResponseDTO>> GetByIdAsync(Guid id)
    {
        try
        {
            var evento = await _eventoService.GetByIdAsync(id);
            return Ok(evento);
        }
        catch (KeyNotFoundException erro) { return NotFound(erro.Message); }
        catch (Exception erro) { return StatusCode(500, $"Evento não encontrado | {erro.Message}"); }
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Vendedor")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> CreateAsync(EventoRequestDTO evento)
    {
        try
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                            ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var id = await _eventoService.CriarEventoAsync(evento, userId);
            return Ok(new { id });
        }
        catch (Exception erro) { return BadRequest($"Erro ao criar novo evento | {erro.Message}"); }
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Vendedor")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> UpdateAsync(Guid id, EventoRequestDTO evento)
    {
        try
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                            ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return Unauthorized();

            await _eventoService.UpdateAsync(id, evento, userId);
            return Ok();
        }
        catch (KeyNotFoundException erro) { return NotFound($"Evento não encontrado | {erro.Message}"); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception erro) { return BadRequest($"Erro ao atualizar o evento | {erro.Message}"); }
    }

    /// <summary>
    /// Spec 50: Consulta status de cancelamento do evento.
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpGet("{id:guid}/status-cancelamento")]
    [Authorize(Roles = "Vendedor,Admin")]
    public async Task<IActionResult> StatusCancelamento(Guid id, CancellationToken ct)
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Não foi possível identificar o usuário." });

        try
        {
            var isAdmin = User.IsInRole("Admin");
            var status = await _eventoService.ObterStatusCancelamento(id, userId, isAdmin, ct);
            return Ok(status);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return StatusCode(403, new { message = e.Message });
        }
    }

    /// <summary>
    /// Spec 50: Cancela evento com reembolso. Substitui DeleteAsync.
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Vendedor,Admin")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                        ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized(new { message = "Não foi possível identificar o usuário." });

        try
        {
            var isAdmin = User.IsInRole("Admin");
            await _eventoService.CancelarEvento(id, userId, isAdmin, ct);
            return Ok(new { message = "Evento cancelado com sucesso." });
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(new { message = e.Message });
        }
        catch (UnauthorizedAccessException e)
        {
            return StatusCode(403, new { message = e.Message });
        }
        catch (DomainException e)
        {
            return Conflict(new { message = e.Message });
        }
    }
}