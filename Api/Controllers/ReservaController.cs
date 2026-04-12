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

    [HttpPost("FazerReserva")]
    [Authorize(Roles = "Comprador")]
    public async Task<IActionResult> FazerReserva([FromBody] ReservarDTO dto, CancellationToken ct)
    {
        var cpfUsuarioLogado = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(cpfUsuarioLogado))
        {
            return Unauthorized(new { message = "Não foi possível identificar o CPF no token do usuário logado." });
        }

        await _service.FazerReserva(cpfUsuarioLogado, dto, ct);
        
        return Ok(new { message = "Reserva realizada com sucesso!" });
    }

    [HttpGet("ListarPorCpf")]
    [Authorize(Roles = "Comprador")]
    public async Task<IActionResult> ListarMinhasReservas(CancellationToken ct)
    {
        var cpfUsuarioLogado = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(cpfUsuarioLogado))
        {
            return Unauthorized(new { message = "Não foi possível identificar o usuário." });
        }

        var reservas = await _service.ListarMinhasReservas(cpfUsuarioLogado, ct);
        
        return Ok(reservas);
    }

    [HttpGet("Admin/Todas")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ListarTodasAdmin(CancellationToken ct)
    {
        var reservas = await _repository.ListarTodasDetalhadasAdmin(ct);
        return Ok(reservas);
    }
}