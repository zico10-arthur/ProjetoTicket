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
    private readonly IIngressoRepository _ingressoRepository;

    public ReservaController(IReservaService service, IReservaRepository repository, IIngressoRepository ingressoRepository)
    {
        _service = service;
        _repository = repository;
        _ingressoRepository = ingressoRepository;
    }

    [HttpPost("criar")]
    [Authorize]
    public async Task<IActionResult> CriarReserva([FromBody] ReservarDTO dto, CancellationToken ct)
    {
        var cpfUsuarioLogado = User.Claims.FirstOrDefault(c => c.Type == "cpf")?.Value;

        if (string.IsNullOrEmpty(cpfUsuarioLogado))
        {
            return Unauthorized(new { message = "Não foi possível identificar o CPF no token do usuário logado." });
        }

        try
        {
            var id = await _service.FazerReserva(cpfUsuarioLogado, dto, ct);
            return Ok(new { reservaId = id, message = "Reserva realizada com sucesso!" });
        }
        catch (Domain.Exceptions.DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("minhas")]
    [Authorize]
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

    [HttpPost("ConfirmarPagamento/{ingressoId}")]
    [Authorize]
    public async Task<IActionResult> ConfirmarPagamento([FromRoute] Guid ingressoId, CancellationToken ct)
    {
        await _ingressoRepository.VenderIngresso(ingressoId, ct);
        return Ok(new { message = "Pagamento confirmado!" });
    }
}
