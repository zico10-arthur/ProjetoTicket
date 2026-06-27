namespace Domain.DTOs;

/// <summary>
/// Spec 200: UsuarioId (Guid) em vez de CpfUsuario.
/// </summary>
public class ReservaAdminDTO : ReservaDetalhadaDTO
{
    public string NomeUsuario { get; set; } = "";
    public Guid UsuarioId { get; set; }
}