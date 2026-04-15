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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Vendedor,Admin")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> DeleteAsync(Guid id)
    {
        try
        {
            var cpf = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;
            if (string.IsNullOrEmpty(cpf)) return Unauthorized();

            var isAdmin = User.IsInRole("Admin");
            await _eventoService.DeleteAsync(id, cpf, isAdmin);
            return Ok();
        }
        catch (KeyNotFoundException erro) { return NotFound($"Evento não encontrado | {erro.Message}"); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception erro) { return BadRequest($"Erro ao deletar o evento | {erro.Message}"); }
    }
}
