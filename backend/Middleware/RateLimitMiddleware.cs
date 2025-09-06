using WebZcan.Services;
using System.Net;
using System.Text.Json;

namespace WebZcan.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RateLimitService _rateLimitService;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, RateLimitService rateLimitService, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Only apply rate limiting to scan endpoints
            if (!context.Request.Path.StartsWithSegments("/api/scan") || 
                context.Request.Method != "POST")
            {
                await _next(context);
                return;
            }

            var clientIp = GetClientIpAddress(context);
            
            if (!_rateLimitService.IsAllowed(clientIp))
            {
                await HandleRateLimitExceeded(context, clientIp);
                return;
            }

            // ✅ FIXED: Use indexer instead of .Add()
            var rateLimitInfo = _rateLimitService.GetRateLimitInfo(clientIp);
            context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.RequestsAllowed.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = (rateLimitInfo.RequestsAllowed - rateLimitInfo.RequestsUsed).ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();
            context.Response.Headers["X-RateLimit-Window"] = $"{rateLimitInfo.WindowSizeMinutes}m";

            await _next(context);
        }

        private string GetClientIpAddress(HttpContext context)
        {
            // Check for forwarded IP headers (common in load balancers/proxies)
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // X-Forwarded-For can contain multiple IPs, take the first one
                var firstIp = forwardedFor.Split(',')[0].Trim();
                if (IPAddress.TryParse(firstIp, out _))
                {
                    return firstIp;
                }
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            var remoteIp = context.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(remoteIp))
            {
                // Handle IPv6 loopback and IPv4-mapped IPv6
                if (remoteIp == "::1")
                    return "127.0.0.1";
                
                if (remoteIp.StartsWith("::ffff:"))
                    return remoteIp.Substring(7);
                
                return remoteIp;
            }

            return "127.0.0.1"; // Default fallback
        }

        private async Task HandleRateLimitExceeded(HttpContext context, string clientIp)
        {
            var rateLimitInfo = _rateLimitService.GetRateLimitInfo(clientIp);
            
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            
            // ✅ FIXED: Use indexer instead of .Add()
            context.Response.Headers["X-RateLimit-Limit"] = rateLimitInfo.RequestsAllowed.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "0";
            context.Response.Headers["X-RateLimit-Reset"] = ((DateTimeOffset)rateLimitInfo.ResetTime).ToUnixTimeSeconds().ToString();
            context.Response.Headers["X-RateLimit-Window"] = $"{rateLimitInfo.WindowSizeMinutes}m";
            context.Response.Headers["Retry-After"] = ((int)(rateLimitInfo.ResetTime - DateTime.UtcNow).TotalSeconds).ToString();

            var response = new
            {
                error = "Rate limit exceeded",
                message = $"Too many scan requests. Limit: {rateLimitInfo.RequestsAllowed} requests per {rateLimitInfo.WindowSizeMinutes} minutes.",
                details = new
                {
                    limit = rateLimitInfo.RequestsAllowed,
                    windowMinutes = rateLimitInfo.WindowSizeMinutes,
                    resetTime = rateLimitInfo.ResetTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    clientIp = clientIp
                }
            };

            _logger.LogWarning("Rate limit exceeded for IP {ClientIp}. Reset time: {ResetTime}", 
                clientIp, rateLimitInfo.ResetTime);

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            await context.Response.WriteAsync(json);
        }
    }
}