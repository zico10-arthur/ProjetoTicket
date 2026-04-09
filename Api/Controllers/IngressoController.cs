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

    public IngressoController(IIngressoService service)
    {
        _service = service;
    }

    [HttpGet("eventos/{eventoId}/ingressos")]
    public async Task<IActionResult> ListarIngressos([FromRoute]Guid eventoId, CancellationToken ct)
    {
        var ingressos = await _service.ListarIngressosDoEventoAsync(eventoId, ct);

        return Ok(ingressos);
    }
}