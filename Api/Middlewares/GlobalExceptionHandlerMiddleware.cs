using Domain.Exceptions;

namespace Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro capturado no ProjetoTicket {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var (statusCode, message) = ex switch
        {
            // Spec 120: LoginErro retorna 401 com mensagem padronizada
            Application.Exceptions.LoginErro
                => (StatusCodes.Status401Unauthorized, "Email ou senha inválidos."),

            // Spec 150: Conflitos (409) — mensagens específicas do negócio
            Application.Exceptions.CnpjJaCadastrado
                => (StatusCodes.Status409Conflict, "CNPJ já cadastrado."),
            Application.Exceptions.EmailJaCadastrado
                => (StatusCodes.Status409Conflict, "E-mail já cadastrado."),
            Application.Exceptions.UsuarioCadastrado
                => (StatusCodes.Status409Conflict, "Usuário já cadastrado."),

            // Spec 150: DomainException (400) — mensagem genérica, não expõe detalhes
            DomainException => (StatusCodes.Status400BadRequest, 
                "Dados inválidos. Verifique as informações enviadas."),

            // Spec 150: Não encontrado (404)
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),

            // Spec 150: Não autorizado (401) — mensagem fixa
            UnauthorizedAccessException
                => (StatusCodes.Status401Unauthorized, "Acesso não autorizado."),

            // Spec 150: Fallback genérico (500) — nunca expõe detalhes
            _ => (StatusCodes.Status500InternalServerError, "Erro interno do servidor.")
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
