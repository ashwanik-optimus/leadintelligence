namespace LeadPortal.Middleware;

/// <summary>
/// Adds baseline security response headers to every response. The CSP allows the
/// same-origin SPA, its inline (first-party) styles/scripts, and the SignalR client
/// loaded from cdnjs, plus WebSocket connections for the live dashboard.
/// </summary>
public static class SecurityHeadersMiddleware
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdnjs.cloudflare.com; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "connect-src 'self' https: wss:; " +
        "base-uri 'self'; " +
        "form-action 'self'; " +
        "frame-ancestors 'none'; " +
        "object-src 'none'";

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Content-Security-Policy"] = ContentSecurityPolicy;
            await next();
        });
}
