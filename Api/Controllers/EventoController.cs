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

    [HttpGet("meus")]
    [Authorize(Roles = "Vendedor")]
    public async Task<ActionResult<EventoResponseDTO>> GetMeusEventosAsync()
    {
        var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
        if (string.IsNullOrEmpty(cpf)) return Unauthorized();

        var eventos = await _eventoService.GetAllByVendedorAsync(cpf);
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

    [HttpPost]
    [Authorize(Roles = "Vendedor")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> CreateAsync(EventoRequestDTO evento)
    {
        try
        {
            var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
            if (string.IsNullOrEmpty(cpf)) return Unauthorized();

            var id = await _eventoService.CriarEventoAsync(evento, cpf);
            return Ok(new { id });
        }
        catch (Exception erro) { return BadRequest($"Erro ao criar novo evento | {erro.Message}"); }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Vendedor")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> UpdateAsync(Guid id, EventoRequestDTO evento)
    {
        try
        {
            var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
            if (string.IsNullOrEmpty(cpf)) return Unauthorized();

            await _eventoService.UpdateAsync(id, evento, cpf);
            return Ok();
        }
        catch (KeyNotFoundException erro) { return NotFound($"Evento não encontrado | {erro.Message}"); }
        catch (UnauthorizedAccessException erro) { return Forbid(); }
        catch (Exception erro) { return BadRequest($"Erro ao atualizar o evento | {erro.Message}"); }
    }

    [HttpGet("{id:guid}/status-cancelamento")]
    [Authorize(Roles = "Vendedor,Admin")]
    public async Task<IActionResult> StatusCancelamento(Guid id, CancellationToken ct)
    {
        var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
        if (string.IsNullOrEmpty(cpf))
            return Unauthorized(new { message = "Token inválido: claim 'cpf' não encontrada." });

        try
        {
            var isAdmin = User.IsInRole("Admin");
            var status = await _eventoService.ObterStatusCancelamento(id, cpf, isAdmin, ct);
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Vendedor,Admin")]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken ct)
    {
        var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
        if (string.IsNullOrEmpty(cpf))
            return Unauthorized(new { message = "Token inválido: claim 'cpf' não encontrada." });

        try
        {
            var isAdmin = User.IsInRole("Admin");
            await _eventoService.CancelarEvento(id, cpf, isAdmin, ct);
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
