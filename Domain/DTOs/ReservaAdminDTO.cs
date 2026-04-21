namespace Domain.DTOs;

public class ReservaAdminDTO : ReservaDetalhadaDTO
{
    public string NomeUsuario { get; set; } = "";
    public string CpfUsuario { get; set; } = "";
}