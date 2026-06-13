using System.Collections.Concurrent;

namespace Api.Middlewares;

/// <summary>
/// ST-01: Rate limiting para o endpoint de login (5 requisições/min por IP).
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, RateLimitEntry> _clients = new();

    private const int MaxRequests = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Aplica apenas ao endpoint de login
        if (!context.Request.Path.Value?.EndsWith("/login", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            await _next(context);
            return;
        }

        var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;

        var entry = _clients.GetOrAdd(clientIp, _ => new RateLimitEntry { Count = 0, WindowStart = now });

        bool limitExceeded;
        lock (entry)
        {
            if (now - entry.WindowStart > Window)
            {
                entry.Count = 0;
                entry.WindowStart = now;
            }

            entry.Count++;
            limitExceeded = entry.Count > MaxRequests;
        }

        if (limitExceeded)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                "{\"statusCode\":429,\"message\":\"Muitas requisições. Tente novamente em 1 minuto.\"}");
            return;
        }

        await _next(context);
    }

    private class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }
}
