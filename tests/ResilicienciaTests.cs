using System.Text.Json;
using Api.Middlewares;
using Application.DTOs;
using Application.Exceptions;
using Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace SoldOutTickets.Tests;

/// <summary>
/// Spec 150: Testes de resiliência — sanitização de erros, validação de input, Authorize, Trim.
/// </summary>
public class ResilicienciaTests
{
    // =========================================================================
    // Unit Tests: GlobalExceptionHandlerMiddleware (FR-001, FR-005, FR-006)
    // =========================================================================

    /// <summary>
    /// T1: DomainException deve retornar HTTP 400 com mensagem fixa,
    /// NÃO o ex.Message original.
    /// </summary>
    [Fact]
    public async Task DomainException_Retorna400_ComMensagemFixa()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new DomainException("mensagem interna que não deve vazar"),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Equal("Dados inválidos. Verifique as informações enviadas.", message);
        Assert.DoesNotContain("mensagem interna", message);
    }

    /// <summary>
    /// T2: Exception genérica deve retornar HTTP 500 com mensagem genérica,
    /// NUNCA expor detalhes internos.
    /// </summary>
    [Fact]
    public async Task ExceptionGenerica_Retorna500_ComMensagemGenerica()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new Exception("detalhes internos confidenciais"),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Equal("Erro interno do servidor.", message);
        Assert.DoesNotContain("detalhes", message);
    }

    /// <summary>
    /// T3: UnauthorizedAccessException deve retornar HTTP 401 com mensagem fixa,
    /// NÃO o ex.Message original.
    /// </summary>
    [Fact]
    public async Task UnauthorizedAccessException_Retorna401_Fixo()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new UnauthorizedAccessException("mensagem interna do sistema"),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Equal("Acesso não autorizado.", message);
        Assert.DoesNotContain("mensagem interna", message);
    }

    /// <summary>
    /// T4: LoginErro deve retornar HTTP 401 com a mensagem padronizada de login.
    /// (Teste de regressão — já implementado na Spec 120)
    /// </summary>
    [Fact]
    public async Task LoginErro_Retorna401_MensagemLogin()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new LoginErro(),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Equal("Email ou senha inválidos.", message);
    }

    /// <summary>
    /// T5: Conflitos (409) — CnpjJaCadastrado retorna mensagem fixa.
    /// </summary>
    [Fact]
    public async Task CnpjJaCadastrado_Retorna409_ComMensagemFixa()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ => throw new CnpjJaCadastrado(),
            loggerMock.Object);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        var message = json.RootElement.GetProperty("message").GetString();

        Assert.Equal("CNPJ já cadastrado.", message);
    }

    /// <summary>
    /// T6: Requisição bem-sucedida NÃO deve ser interceptada pelo middleware.
    /// </summary>
    [Fact]
    public async Task RequisicaoBemSucedida_PassaDireto()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<GlobalExceptionHandlerMiddleware>>();
        var foiChamado = false;
        var middleware = new GlobalExceptionHandlerMiddleware(
            _ =>
            {
                foiChamado = true;
                return Task.CompletedTask;
            },
            loggerMock.Object);

        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(foiChamado);
        Assert.Equal(200, context.Response.StatusCode);
    }

    // =========================================================================
    // DTO Sanitization Tests: Trim (FR-004)
    // =========================================================================

    /// <summary>
    /// T7: CadastrarUsuarioDTO.Nome com espaços deve ser trimmed.
    /// </summary>
    [Fact]
    public void DTO_Trim_NomeComEspacos()
    {
        // Arrange
        var nomeComEspacos = "  João  ";

        // Act
        var dto = new CadastrarUsuarioDTO
        {
            Nome = nomeComEspacos,
            Email = "teste@email.com",
            Cpf = "52998224725",
            Senha = "Teste@123"
        };

        // Assert
        Assert.Equal("João", dto.Nome);
    }

    /// <summary>
    /// T8: LoginDTO.Email com espaços e maiúsculas deve ser trimmed e lowered.
    /// </summary>
    [Fact]
    public void DTO_Trim_EmailComEspacos()
    {
        // Arrange
        var emailComEspacos = "  TESTE@EMAIL.COM  ";

        // Act
        var dto = new LoginDTO
        {
            Email = emailComEspacos,
            Senha = "Teste@123"
        };

        // Assert
        Assert.Equal("teste@email.com", dto.Email);
    }
}
