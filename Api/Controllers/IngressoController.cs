using Application.DTOs;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Domain.Interface;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class IngressoController : ControllerBase
{
    private readonly IIngressoService _service;
    private readonly IIngressoRepository _repository;

    public IngressoController(IIngressoService service, IIngressoRepository repository)
    {
        _service = service;
        _repository = repository;
    }

    [HttpGet("eventos/{eventoId}/ingressos")]
    public async Task<IActionResult> ListarIngressos([FromRoute]Guid eventoId, CancellationToken ct)
    {
        var ingressos = await _service.ListarIngressosDoEventoAsync(eventoId, ct);

        return Ok(ingressos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId([FromRoute] Guid id, CancellationToken ct)
    {
        // Chama o método que já criaste no Repositório!
        var ingresso = await _repository.BuscarIngressoId(id, ct);

        if (ingresso == null)
            return NotFound(new { message = "Ingresso não encontrado." });

        return Ok(ingresso);
    }

}