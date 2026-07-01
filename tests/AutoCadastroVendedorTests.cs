using Domain.Entities;
using Domain.Exceptions;
using Domain.Validators;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// ST-01: Testes de Auto Cadastro de Vendedor.
/// </summary>
public class AutoCadastroVendedorTests
{
    private const string CnpjValido = "11222333000181";
    private const string RazaoSocialValida = "Workshop de Tecnologia Ltda";
    private const string NomeFantasiaValido = "Tech Workshops";
    private const string EmailValido = "contato@techworkshops.com.br";
    private const string SenhaValida = "Senha@123";
    private const string TelefoneValido = "(11) 99999-0001";

    // ==================== CNPJ Validator ====================

    [Fact]
    public void CnpjValidator_ComCnpjValido_NaoDeveLancarExcecao()
    {
        // Arrange
        var cnpj = CnpjValido;

        // Act
        var ex = Record.Exception(() => CnpjValidator.Validar(cnpj));

        // Assert
        Assert.Null(ex);
    }

    [Fact]
    public void CnpjValidator_ComCnpjValidoFormatado_NaoDeveLancarExcecao()
    {
        // Arrange
        var cnpj = "11.222.333/0001-81";

        // Act
        var ex = Record.Exception(() => CnpjValidator.Validar(cnpj));

        // Assert
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CnpjValidator_ComCnpjVazio_DeveLancarCnpjInvalido(string cnpj)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => CnpjValidator.Validar(cnpj);

        // Assert
        Assert.Throws<CnpjInvalido>(act);
    }

    [Theory]
    [InlineData("12345678000100")] // dígitos verificadores errados
    [InlineData("11111111111111")] // todos dígitos iguais
    [InlineData("12345")]          // tamanho errado
    [InlineData("abcdefghijklmn")] // letras
    public void CnpjValidator_ComCnpjInvalido_DeveLancarCnpjInvalido(string cnpj)
    {
        // Arrange
        // (dados fornecidos pelo InlineData)

        // Act
        Action act = () => CnpjValidator.Validar(cnpj);

        // Assert
        Assert.Throws<CnpjInvalido>(act);
    }

    // ==================== CriarVendedor Factory ====================

    [Fact]
    public void CriarVendedor_ComDadosValidos_DeveRetornarUsuario()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.NotNull(vendedor);
        Assert.Equal("11222333000181", vendedor.Cpf);
        Assert.Equal(RazaoSocialValida, vendedor.Nome);
        Assert.Equal(NomeFantasiaValido, vendedor.NomeFantasia);
        Assert.Equal(EmailValido, vendedor.Email);
        Assert.Equal("11222333000181", vendedor.Cnpj);
        Assert.Equal(TelefoneValido, vendedor.Telefone);
        Assert.Equal(0, vendedor.Plano);
        Assert.True(vendedor.Ativo);

        var perfilEsperado = Guid.Parse("B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2");
        Assert.Equal(perfilEsperado, vendedor.PerfilId);
    }

    [Fact]
    public void CriarVendedor_ComCnpjFormatado_DeveRemoverMascara()
    {
        // Arrange
        var cnpjFormatado = "11.222.333/0001-81";
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var vendedor = Usuario.CriarVendedor(
            cnpjFormatado, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.Equal("11222333000181", vendedor.Cpf);
        Assert.Equal("11222333000181", vendedor.Cnpj);
    }

    [Fact]
    public void CriarVendedor_ComCnpjInvalido_DeveLancarCnpjInvalido()
    {
        // Arrange
        var cnpjInvalido = "12345678000100";
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        Action act = () => Usuario.CriarVendedor(
            cnpjInvalido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.Throws<CnpjInvalido>(act);
    }

    [Fact]
    public void CriarVendedor_ComRazaoSocialVazia_DeveLancarExcecao()
    {
        // Arrange
        var razaoSocial = "";
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        Action act = () => Usuario.CriarVendedor(
            CnpjValido, razaoSocial, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.Throws<RazaoSocialObrigatoria>(act);
    }

    [Fact]
    public void CriarVendedor_ComNomeFantasiaVazio_DeveLancarExcecao()
    {
        // Arrange
        var nomeFantasia = "";
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        Action act = () => Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, nomeFantasia,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.Throws<NomeFantasiaObrigatorio>(act);
    }

    [Fact]
    public void CriarVendedor_ComEmailInvalido_DeveLancarExcecao()
    {
        // Arrange
        var emailInvalido = "emailinvalido";
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        Action act = () => Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            emailInvalido, senhaHash, TelefoneValido);

        // Assert
        Assert.Throws<EmailInvalido>(act);
    }

    [Fact]
    public void CriarVendedor_ComSenhaHashVazia_DeveLancarExcecao()
    {
        // Arrange
        var senhaHash = "";

        // Act
        Action act = () => Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.Throws<SenhaVazia>(act);
    }

    [Theory]
    [InlineData("abc")]           // muito curto
    [InlineData("1234-5678")]    // formato errado (sem DDD)
    public void CriarVendedor_ComTelefoneInvalido_DeveLancarExcecao(string telefone)
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        Action act = () => Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, telefone);

        // Assert
        Assert.Throws<TelefoneInvalido>(act);
    }

    [Fact]
    public void CriarVendedor_SemTelefone_DeveCriarNormalmente()
    {
        // Arrange
        var telefoneVazio = "";
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, telefoneVazio);

        // Assert
        Assert.Equal("", vendedor.Telefone);
    }

    // ==================== BCrypt Hash ====================

    [Fact]
    public void BCrypt_HashPassword_DeveSerDiferenteDaSenhaOriginal()
    {
        // Arrange
        // (constante SenhaValida)

        // Act
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Assert
        Assert.NotEqual(SenhaValida, hash);
    }

    [Fact]
    public void BCrypt_Verify_DeveValidarSenhaCorreta()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var resultado = BCrypt.Net.BCrypt.Verify(SenhaValida, hash);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public void BCrypt_Verify_DeveRejeitarSenhaErrada()
    {
        // Arrange
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var resultado = BCrypt.Net.BCrypt.Verify("SenhaErrada1@", hash);

        // Assert
        Assert.False(resultado);
    }

    // ==================== PerfilId do Vendedor ====================

    [Fact]
    public void CriarVendedor_ComDadosValidos_DeveTerPerfilIdDeVendedor()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        var perfilVendedor = Guid.Parse("B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2");
        Assert.Equal(perfilVendedor, vendedor.PerfilId);
    }

    [Fact]
    public void CriarVendedor_ComDadosValidos_DeveTerPlanoGratuito()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.Equal(0, vendedor.Plano);
    }

    [Fact]
    public void CriarVendedor_ComDadosValidos_DeveTerAtivoTrue()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        // Act
        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        // Assert
        Assert.True(vendedor.Ativo);
    }
}
