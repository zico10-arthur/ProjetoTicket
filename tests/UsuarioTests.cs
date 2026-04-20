using Domain.Entities;
using Domain.Exceptions;
using Xunit;

namespace SoldOutTickets.Tests;

public class UsuarioTests
{
    private const string CpfValido = "52998224725";
    private const string NomeValido = "João Silva";
    private const string EmailValido = "joao@email.com";
    private const string SenhaValida = "Senha@123";
    private static readonly Guid PerfilId = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");

    [Fact]
    public void Criar_ComDadosValidos_DeveRetornarUsuario()
    {
        var usuario = Usuario.Criar(CpfValido, NomeValido, EmailValido, PerfilId, SenhaValida);

        Assert.NotNull(usuario);
        Assert.Equal(CpfValido, usuario.Cpf);
        Assert.Equal(NomeValido, usuario.Nome);
    }

    [Fact]
    public void Criar_ComCpfVazio_DeveLancarCpfVazio()
    {
        Assert.Throws<CpfVazio>(() =>
            Usuario.Criar("", NomeValido, EmailValido, PerfilId, SenhaValida));
    }

    [Theory]
    [InlineData("12345678900")] // dígitos inválidos
    [InlineData("11111111111")] // todos iguais
    [InlineData("1234567")]     // tamanho errado
    public void Criar_ComCpfInvalido_DeveLancarCpfInvalido(string cpfInvalido)
    {
        Assert.Throws<CpfInvalido>(() =>
            Usuario.Criar(cpfInvalido, NomeValido, EmailValido, PerfilId, SenhaValida));
    }

    [Fact]
    public void Criar_ComNomeVazio_DeveLancarNomeVazio()
    {
        Assert.Throws<NomeVazio>(() =>
            Usuario.Criar(CpfValido, "", EmailValido, PerfilId, SenhaValida));
    }

    [Theory]
    [InlineData("AB")]          // muito curto
    [InlineData("João123")]     // contém número
    [InlineData("aaaaaaaaaa")]  // todos iguais
    public void Criar_ComNomeInvalido_DeveLancarNomeInvalido(string nomeInvalido)
    {
        Assert.Throws<NomeInvalido>(() =>
            Usuario.Criar(CpfValido, nomeInvalido, EmailValido, PerfilId, SenhaValida));
    }

    [Fact]
    public void Criar_ComEmailVazio_DeveLancarEmailVazio()
    {
        Assert.Throws<EmailVazio>(() =>
            Usuario.Criar(CpfValido, NomeValido, "", PerfilId, SenhaValida));
    }

    [Theory]
    [InlineData("emailsemarroba")]
    [InlineData("email@")]
    [InlineData("@dominio.com")]
    public void Criar_ComEmailInvalido_DeveLancarEmailInvalido(string emailInvalido)
    {
        Assert.Throws<EmailInvalido>(() =>
            Usuario.Criar(CpfValido, NomeValido, emailInvalido, PerfilId, SenhaValida));
    }

    [Fact]
    public void Criar_ComSenhaVazia_DeveLancarSenhaVazia()
    {
        Assert.Throws<SenhaVazia>(() =>
            Usuario.Criar(CpfValido, NomeValido, EmailValido, PerfilId, ""));
    }

    [Theory]
    [InlineData("abc123")]       // menos de 8 caracteres
    [InlineData("abcdefgh")]     // sem número e sem especial
    [InlineData("12345678")]     // sem letra e sem especial
    public void Criar_ComSenhaInvalida_DeveLancarExcecao(string senhaInvalida)
    {
        Assert.ThrowsAny<Exception>(() =>
            Usuario.Criar(CpfValido, NomeValido, EmailValido, PerfilId, senhaInvalida));
    }

    [Fact]
    public void Criar_ComCpfFormatado_DeveRemoverMascaraECriar()
    {
        var usuario = Usuario.Criar("529.982.247-25", NomeValido, EmailValido, PerfilId, SenhaValida);

        Assert.Equal("52998224725", usuario.Cpf);
    }
}
