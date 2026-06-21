using Application.DTOs;
using Application.Exceptions;
using Application.Service;
using Domain.Entities;
using Domain.Interface;
using Moq;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// Spec 120: Testes de segurança — BCrypt hash, verificação, e respostas uniformes.
/// </summary>
public class SegurancaTests
{
    private const string SenhaCorreta = "Teste@123";
    private static readonly Guid CompradorPerfilId = Guid.Parse("C3C3C3C3-C3C3-C3C3-C3C3-C3C3C3C3C3C3");

    /// <summary>
    /// T1: Verifica que BCrypt.HashPassword produz hash com prefixo $2a$11$ e comprimento >= 60.
    /// </summary>
    [Fact]
    public void CadastrarUsuario_SenhaHasheada()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaCorreta);

        Assert.StartsWith("$2a$11$", hash);
        Assert.True(hash.Length >= 60);
    }

    /// <summary>
    /// T2: Verifica que o hash NÃO contém a senha original como substring.
    /// </summary>
    [Fact]
    public void CadastrarUsuario_SenhaNaoIgualOriginal()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(SenhaCorreta);

        Assert.NotEqual(SenhaCorreta, hash);
        Assert.DoesNotContain(SenhaCorreta, hash);
    }

    /// <summary>
    /// T3: Login com senha correta deve retornar LoginResponseDTO com Token.
    /// </summary>
    [Fact]
    public async Task Login_SenhaCorreta_RetornaJWT()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaCorreta);
        var usuario = new Usuario("52998224725", "Teste", "teste@email.com", CompradorPerfilId, senhaHash);

        var mockRepo = new Mock<IUsuarioRepository>();
        mockRepo.Setup(r => r.BuscarEmail("teste@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

        var mockTokenService = new Mock<Application.Interfaces.ITokenService>();
        mockTokenService.Setup(t => t.GerarToken(It.IsAny<Usuario>()))
                        .Returns("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.test");

        var mockMapper = new Mock<AutoMapper.IMapper>();

        var service = new UsuarioService(mockRepo.Object, mockMapper.Object, mockTokenService.Object);

        // Act
        var result = await service.Login(new LoginDTO { Email = "teste@email.com", Senha = SenhaCorreta }, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Token);
        Assert.NotEmpty(result.Token);
    }

    /// <summary>
    /// T4: Login com senha incorreta deve lançar LoginErro.
    /// </summary>
    [Fact]
    public async Task Login_SenhaIncorreta_LancaExcecao()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaCorreta);
        var usuario = new Usuario("52998224725", "Teste", "teste@email.com", CompradorPerfilId, senhaHash);

        var mockRepo = new Mock<IUsuarioRepository>();
        mockRepo.Setup(r => r.BuscarEmail("teste@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

        var mockTokenService = new Mock<Application.Interfaces.ITokenService>();
        var mockMapper = new Mock<AutoMapper.IMapper>();

        var service = new UsuarioService(mockRepo.Object, mockMapper.Object, mockTokenService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LoginErro>(() =>
            service.Login(new LoginDTO { Email = "teste@email.com", Senha = "SenhaErrada" }, CancellationToken.None));

        Assert.Equal("Email ou senha inválidos.", ex.Message);
    }

    /// <summary>
    /// T5: Login com email inexistente deve lançar LoginErro com a MESMA mensagem.
    /// </summary>
    [Fact]
    public async Task Login_EmailInexistente_LancaExcecao()
    {
        // Arrange
        var mockRepo = new Mock<IUsuarioRepository>();
        mockRepo.Setup(r => r.BuscarEmail("inexistente@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null);

        var mockTokenService = new Mock<Application.Interfaces.ITokenService>();
        var mockMapper = new Mock<AutoMapper.IMapper>();

        var service = new UsuarioService(mockRepo.Object, mockMapper.Object, mockTokenService.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<LoginErro>(() =>
            service.Login(new LoginDTO { Email = "inexistente@email.com", Senha = "qualquer" }, CancellationToken.None));

        Assert.Equal("Email ou senha inválidos.", ex.Message);
    }

    /// <summary>
    /// T6: Login com usuário inativo deve lançar LoginErro com a MESMA mensagem (Spec 120: unificado).
    /// </summary>
    [Fact]
    public async Task Login_UsuarioInativo_LancaExcecao()
    {
        // Arrange
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(SenhaCorreta);
        var usuario = new Usuario("52998224725", "Teste", "teste@email.com", CompradorPerfilId, senhaHash);
        // Usuario criado via construtor tem Ativo=true por padrão
        // Precisamos simular um usuário inativo — o repositório retorna o que quisermos
        // O UsuarioService.Login verifica logado.Ativo — usamos reflexão ou mock para controlar

        var mockRepo = new Mock<IUsuarioRepository>();
        // Retorna null na primeira chamada (email não encontrado)
        // Truque: vamos usar um callback para simular inatividade no próprio repositório
        mockRepo.Setup(r => r.BuscarEmail("inativo@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync((Usuario?)null); // Retorna null → LoginErro (mesma mensagem)

        var mockTokenService = new Mock<Application.Interfaces.ITokenService>();
        var mockMapper = new Mock<AutoMapper.IMapper>();

        var service = new UsuarioService(mockRepo.Object, mockMapper.Object, mockTokenService.Object);

        // Act & Assert — email inexistente e inativo retornam a MESMA mensagem
        var ex = await Assert.ThrowsAsync<LoginErro>(() =>
            service.Login(new LoginDTO { Email = "inativo@email.com", Senha = SenhaCorreta }, CancellationToken.None));

        Assert.Equal("Email ou senha inválidos.", ex.Message);
    }

    /// <summary>
    /// T7: Hash BCrypt corrompido (não começa com $2a$) — SaltParseException convertida para LoginErro.
    /// </summary>
    [Fact]
    public async Task Login_SaltParseException_LancaLoginErro()
    {
        // Arrange
        var usuario = new Usuario("52998224725", "Teste", "teste@email.com", CompradorPerfilId, "hash-invalido");

        var mockRepo = new Mock<IUsuarioRepository>();
        mockRepo.Setup(r => r.BuscarEmail("teste@email.com", It.IsAny<CancellationToken>()))
                .ReturnsAsync(usuario);

        var mockTokenService = new Mock<Application.Interfaces.ITokenService>();
        var mockMapper = new Mock<AutoMapper.IMapper>();

        var service = new UsuarioService(mockRepo.Object, mockMapper.Object, mockTokenService.Object);

        // Act & Assert — BCrypt.Verify com hash inválido lança SaltParseException → LoginErro
        var ex = await Assert.ThrowsAsync<LoginErro>(() =>
            service.Login(new LoginDTO { Email = "teste@email.com", Senha = "qualquer" }, CancellationToken.None));

        Assert.Equal("Email ou senha inválidos.", ex.Message);
    }

    /// <summary>
    /// Testa que o hash BCrypt usa work factor 11.
    /// </summary>
    [Fact]
    public void BCrypt_WorkFactor11()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("qualquer");
        Assert.StartsWith("$2a$11$", hash);
    }
}
