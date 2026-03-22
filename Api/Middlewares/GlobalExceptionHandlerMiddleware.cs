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
        context.Response.ContentType = "application/json";

        var (statusCode, message) = ex switch
        {
            DomainException => (StatusCodes.Status400BadRequest, ex.Message),
            KeyNotFoundException => (StatusCodes.Status404NotFound, "Recurso não encontrado."),
            _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro interno no servidor.")
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            StatusCode = statusCode,
            Message = message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}
