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
        // Arrange
        // (constantes já definidas)

        // Act
        var usuario = Usuario.Criar(CpfValido, NomeValido, EmailValido, PerfilId, SenhaValida);

        // Assert
        Assert.NotNull(usuario);
        Assert.Equal(CpfValido, usuario.Cpf);
        Assert.Equal(NomeValido, usuario.Nome);
    }

    [Fact]
    public void Criar_ComCpfVazio_DeveLancarCpfVazio()
    {
        // Arrange
        var cpf = "";

        // Act
        Action act = () => Usuario.Criar(cpf, NomeValido, EmailValido, PerfilId, SenhaValida);

        // Assert
        Assert.Throws<CpfVazio>(act);
    }

    [Theory]
    [InlineData("12345678900")] // dígitos inválidos
    [InlineData("11111111111")] // todos iguais
    [InlineData("1234567")]     // tamanho errado
    public void Criar_ComCpfInvalido_DeveLancarCpfInvalido(string cpfInvalido)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => Usuario.Criar(cpfInvalido, NomeValido, EmailValido, PerfilId, SenhaValida);

        // Assert
        Assert.Throws<CpfInvalido>(act);
    }

    [Fact]
    public void Criar_ComNomeVazio_DeveLancarNomeVazio()
    {
        // Arrange
        var nome = "";

        // Act
        Action act = () => Usuario.Criar(CpfValido, nome, EmailValido, PerfilId, SenhaValida);

        // Assert
        Assert.Throws<NomeVazio>(act);
    }

    [Theory]
    [InlineData("AB")]          // muito curto
    [InlineData("João123")]     // contém número
    [InlineData("aaaaaaaaaa")]  // todos iguais
    public void Criar_ComNomeInvalido_DeveLancarNomeInvalido(string nomeInvalido)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => Usuario.Criar(CpfValido, nomeInvalido, EmailValido, PerfilId, SenhaValida);

        // Assert
        Assert.Throws<NomeInvalido>(act);
    }

    [Fact]
    public void Criar_ComEmailVazio_DeveLancarEmailVazio()
    {
        // Arrange
        var email = "";

        // Act
        Action act = () => Usuario.Criar(CpfValido, NomeValido, email, PerfilId, SenhaValida);

        // Assert
        Assert.Throws<EmailVazio>(act);
    }

    [Theory]
    [InlineData("emailsemarroba")]
    [InlineData("email@")]
    [InlineData("@dominio.com")]
    public void Criar_ComEmailInvalido_DeveLancarEmailInvalido(string emailInvalido)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => Usuario.Criar(CpfValido, NomeValido, emailInvalido, PerfilId, SenhaValida);

        // Assert
        Assert.Throws<EmailInvalido>(act);
    }

    [Fact]
    public void Criar_ComSenhaVazia_DeveLancarSenhaVazia()
    {
        // Arrange
        var senha = "";

        // Act
        Action act = () => Usuario.Criar(CpfValido, NomeValido, EmailValido, PerfilId, senha);

        // Assert
        Assert.Throws<SenhaVazia>(act);
    }

    [Theory]
    [InlineData("abc123")]       // menos de 8 caracteres
    [InlineData("abcdefgh")]     // sem número e sem especial
    [InlineData("12345678")]     // sem letra e sem especial
    public void Criar_ComSenhaInvalida_DeveLancarExcecao(string senhaInvalida)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => Usuario.Criar(CpfValido, NomeValido, EmailValido, PerfilId, senhaInvalida);

        // Assert
        Assert.ThrowsAny<Exception>(act);
    }

    [Fact]
    public void Criar_ComCpfFormatado_DeveRemoverMascaraECriar()
    {
        // Arrange
        var cpfFormatado = "529.982.247-25";

        // Act
        var usuario = Usuario.Criar(cpfFormatado, NomeValido, EmailValido, PerfilId, SenhaValida);

        // Assert
        Assert.Equal("52998224725", usuario.Cpf);
    }
}
