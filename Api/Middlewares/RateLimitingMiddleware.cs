using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http.Extensions;

namespace Api.Middlewares;

/// <summary>
/// Spec 120: Rate-limiting middleware for the login endpoint.
/// Limits POST /api/usuario/login to 5 requests per minute per IP address.
/// Uses sliding window algorithm with in-memory storage.
/// Respects X-Forwarded-For header for clients behind proxies.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly ConcurrentDictionary<string, SlidingWindow> _clients = new();
    private const int Limit = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    public RateLimitingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldRateLimit(context))
        {
            var ip = GetClientIp(context);
            var window = _clients.GetOrAdd(ip, _ => new SlidingWindow());

            if (!window.TryIncrement(Limit, Interval))
            {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync(
                    "{\"message\":\"Muitas tentativas. Aguarde 1 minuto.\"}");
                return;
            }
        }

        await _next(context);
    }

    private static bool ShouldRateLimit(HttpContext context)
    {
        return context.Request.Path.StartsWithSegments("/api/usuario/login", StringComparison.OrdinalIgnoreCase)
            && HttpMethods.IsPost(context.Request.Method);
    }

    private static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
        {
            return forwarded.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
