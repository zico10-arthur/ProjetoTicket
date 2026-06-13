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
        var ex = Record.Exception(() => CnpjValidator.Validar(CnpjValido));
        Assert.Null(ex);
    }

    [Fact]
    public void CnpjValidator_ComCnpjValidoFormatado_NaoDeveLancarExcecao()
    {
        var ex = Record.Exception(() => CnpjValidator.Validar("11.222.333/0001-81"));
        Assert.Null(ex);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CnpjValidator_ComCnpjVazio_DeveLancarCnpjInvalido(string cnpj)
    {
        Assert.Throws<CnpjInvalido>(() => CnpjValidator.Validar(cnpj));
    }

    [Theory]
    [InlineData("12345678000100")] // dígitos verificadores errados
    [InlineData("11111111111111")] // todos dígitos iguais
    [InlineData("12345")]          // tamanho errado
    [InlineData("abcdefghijklmn")] // letras
    public void CnpjValidator_ComCnpjInvalido_DeveLancarCnpjInvalido(string cnpj)
    {
        Assert.Throws<CnpjInvalido>(() => CnpjValidator.Validar(cnpj));
    }

    // ==================== CriarVendedor Factory ====================

    [Fact]
    public void CriarVendedor_ComDadosValidos_DeveRetornarUsuario()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

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
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        var vendedor = Usuario.CriarVendedor(
            "11.222.333/0001-81", RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        Assert.Equal("11222333000181", vendedor.Cpf);
        Assert.Equal("11222333000181", vendedor.Cnpj);
    }

    [Fact]
    public void CriarVendedor_ComCnpjInvalido_DeveLancarCnpjInvalido()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        Assert.Throws<CnpjInvalido>(() =>
            Usuario.CriarVendedor(
                "12345678000100", RazaoSocialValida, NomeFantasiaValido,
                EmailValido, senhaHash, TelefoneValido));
    }

    [Fact]
    public void CriarVendedor_ComRazaoSocialVazia_DeveLancarExcecao()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        Assert.Throws<RazaoSocialObrigatoria>(() =>
            Usuario.CriarVendedor(
                CnpjValido, "", NomeFantasiaValido,
                EmailValido, senhaHash, TelefoneValido));
    }

    [Fact]
    public void CriarVendedor_ComNomeFantasiaVazio_DeveLancarExcecao()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        Assert.Throws<NomeFantasiaObrigatorio>(() =>
            Usuario.CriarVendedor(
                CnpjValido, RazaoSocialValida, "",
                EmailValido, senhaHash, TelefoneValido));
    }

    [Fact]
    public void CriarVendedor_ComEmailInvalido_DeveLancarExcecao()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        Assert.Throws<EmailInvalido>(() =>
            Usuario.CriarVendedor(
                CnpjValido, RazaoSocialValida, NomeFantasiaValido,
                "emailinvalido", senhaHash, TelefoneValido));
    }

    [Fact]
    public void CriarVendedor_ComSenhaHashVazia_DeveLancarExcecao()
    {
        Assert.Throws<SenhaVazia>(() =>
            Usuario.CriarVendedor(
                CnpjValido, RazaoSocialValida, NomeFantasiaValido,
                EmailValido, "", TelefoneValido));
    }

    [Theory]
    [InlineData("abc")]           // muito curto
    [InlineData("1234-5678")]    // formato errado (sem DDD)
    public void CriarVendedor_ComTelefoneInvalido_DeveLancarExcecao(string telefone)
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        Assert.Throws<TelefoneInvalido>(() =>
            Usuario.CriarVendedor(
                CnpjValido, RazaoSocialValida, NomeFantasiaValido,
                EmailValido, senhaHash, telefone));
    }

    [Fact]
    public void CriarVendedor_SemTelefone_DeveCriarNormalmente()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, "");

        Assert.Equal("", vendedor.Telefone);
    }

    // ==================== BCrypt Hash ====================

    [Fact]
    public void BCrypt_HashPassword_DeveSerDiferenteDaSenhaOriginal()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);
        Assert.NotEqual(SenhaValida, hash);
    }

    [Fact]
    public void BCrypt_Verify_DeveValidarSenhaCorreta()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);
        Assert.True(BCrypt.Net.BCrypt.Verify(SenhaValida, hash));
    }

    [Fact]
    public void BCrypt_Verify_DeveRejeitarSenhaErrada()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);
        Assert.False(BCrypt.Net.BCrypt.Verify("SenhaErrada1@", hash));
    }

    // ==================== PerfilId do Vendedor ====================

    [Fact]
    public void CriarVendedor_DeveTerPerfilIdDeVendedor()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        var perfilVendedor = Guid.Parse("B2B2B2B2-B2B2-B2B2-B2B2-B2B2B2B2B2B2");
        Assert.Equal(perfilVendedor, vendedor.PerfilId);
    }

    [Fact]
    public void CriarVendedor_DeveTerPlanoGratuito()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        Assert.Equal(0, vendedor.Plano);
    }

    [Fact]
    public void CriarVendedor_DeveTerAtivoTrue()
    {
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaValida);

        var vendedor = Usuario.CriarVendedor(
            CnpjValido, RazaoSocialValida, NomeFantasiaValido,
            EmailValido, senhaHash, TelefoneValido);

        Assert.True(vendedor.Ativo);
    }
}
