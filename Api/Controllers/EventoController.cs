using Application.DTOs;
using Application.Interfaces;
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
        catch (UnauthorizedAccessException erro) { return Forbid(); }
        catch (Exception erro) { return BadRequest($"Erro ao atualizar o evento | {erro.Message}"); }
    }

    /// <summary>
    /// Spec 200: Extrai userId (Guid) do JWT.
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Vendedor,Admin")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> DeleteAsync(Guid id)
    {
        try
        {
            var userIdStr = User.Claims.FirstOrDefault(c => c.Type == "userId")?.Value
                            ?? User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

            // Admin pode não ter userId (usa cpf antigo)
            Guid userId = Guid.Empty;
            if (!string.IsNullOrEmpty(userIdStr))
                Guid.TryParse(userIdStr, out userId);

            var isAdmin = User.IsInRole("Admin");
            await _eventoService.DeleteAsync(id, userId, isAdmin);
            return Ok();
        }
        catch (KeyNotFoundException erro) { return NotFound($"Evento não encontrado | {erro.Message}"); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception erro) { return BadRequest($"Erro ao deletar o evento | {erro.Message}"); }
    }
}