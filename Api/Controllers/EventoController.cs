using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

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
        catch (KeyNotFoundException erro)
        {
            
            return NotFound(erro.Message);

        }
        catch (Exception erro)
        {
            
            return StatusCode(500, $"Evento não encontrado | {erro.Message}");

        }

    }


    [HttpGet("{id}")]
    public async Task<ActionResult<EventoResponseDTO>> GetByIdAsync(Guid id)
    {
        
        try
        {
            
            var evento = await _eventoService.GetByIdAsync(id);

            return Ok(evento);

        }
        catch (KeyNotFoundException erro)
        {
            
            return NotFound(erro.Message);

        }
        catch (Exception erro)
        {
            
            return StatusCode(500, $"Evento não encontrado | {erro.Message}");

        }

    }


    [HttpPost]
    [Authorize(Roles = "Vendedor")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> CreateAsync(EventoRequestDTO evento)
    {
        try
        {
            var cpf = User.FindFirstValue("cpf") ?? string.Empty;

            var id = await _eventoService.CriarEventoAsync(evento, cpf);
            return Ok(new { id });
        }
        catch (Exception erro)
        {
            return BadRequest($"Erro ao criar novo evento | {erro.Message}");
        }
    }


    [HttpPut("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> UpdateAsync(Guid id, EventoRequestDTO evento)
    {

        try
        {

            await _eventoService.UpdateAsync(id, evento);

            return Ok();

        }
        catch (KeyNotFoundException erro)
        {

            return NotFound($"Evento não encontrado | {erro.Message}");

        }
        catch (Exception erro)
        {

            return BadRequest($"Erro ao atualizar o evento | {erro.Message}");

        }

    }


    [HttpDelete("{id}")]
    [Consumes("application/json")]
    public async Task<ActionResult<EventoResponseDTO>> DeleteAsync(Guid id)
    {

        try
        {

            await _eventoService.DeleteAsync(id);

            return Ok();

        }
        catch (KeyNotFoundException erro)
        {

            return NotFound($"Evento não encontrado | {erro.Message}");

        }
        catch (Exception erro)
        {

            return BadRequest($"Erro ao deletar o evento | {erro.Message}");

        }

    }
    
}
