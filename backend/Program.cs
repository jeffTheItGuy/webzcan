using WebZcan.Services;
using WebZcan.Middleware;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Get CORS origins from environment variables
var allowedOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")?.Split(',', StringSplitOptions.RemoveEmptyEntries)
    ?? new[] { "http://127.0.0.1:5173" }; // Default fallback

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .WithExposedHeaders("X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset", "X-RateLimit-Window");
    });
});

// Get ZAP configuration from environment variables
var zapUrl = Environment.GetEnvironmentVariable("ZAP_URL") ?? "http://webzcan-zap-svc:8090";
var zapApiKey = Environment.GetEnvironmentVariable("ZAP_API_KEY") ?? "";

// ✅ FIXED: Register ZAP client with logger injection
builder.Services.AddSingleton<ZapClient>(sp =>
    new ZapClient(
        zapUrl,
        zapApiKey,
        sp.GetRequiredService<ILogger<ZapClient>>()
    ));

// Register rate limiting service
var rateLimitMax = int.TryParse(Environment.GetEnvironmentVariable("RATE_LIMIT_MAX"), out var max) ? max : 3;
var rateLimitWindowMinutes = int.TryParse(Environment.GetEnvironmentVariable("RATE_LIMIT_WINDOW_MINUTES"), out var minutes) ? minutes : 60;

builder.Services.AddSingleton<RateLimitService>(provider =>
    new RateLimitService(
        provider.GetRequiredService<ILogger<RateLimitService>>(),
        rateLimitMax,
        TimeSpan.FromMinutes(rateLimitWindowMinutes)
    )
);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseCors("AllowFrontend");

// Add rate limiting middleware BEFORE mapping controllers
app.UseMiddleware<RateLimitMiddleware>();

app.MapControllers();

// Health check endpoint - enhanced with rate limit info
app.MapGet("/api/health", (RateLimitService rateLimitService, HttpContext context) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var rateLimitInfo = rateLimitService.GetRateLimitInfo(clientIp);

    return new
    {
        status = "ok",
        timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        zapUrl = zapUrl,
        allowedOrigins = allowedOrigins,
        rateLimit = new
        {
            maxRequests = rateLimitInfo.RequestsAllowed,
            windowMinutes = rateLimitInfo.WindowSizeMinutes,
            currentUsage = rateLimitInfo.RequestsUsed,
            remaining = rateLimitInfo.RequestsAllowed - rateLimitInfo.RequestsUsed,
            resetTime = rateLimitInfo.ResetTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        }
    };
});

// Rate limit status endpoint
app.MapGet("/api/ratelimit", (RateLimitService rateLimitService, HttpContext context) =>
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var rateLimitInfo = rateLimitService.GetRateLimitInfo(clientIp);

    return new
    {
        limit = rateLimitInfo.RequestsAllowed,
        used = rateLimitInfo.RequestsUsed,
        remaining = rateLimitInfo.RequestsAllowed - rateLimitInfo.RequestsUsed,
        windowMinutes = rateLimitInfo.WindowSizeMinutes,
        resetTime = rateLimitInfo.ResetTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
        resetInSeconds = (int)(rateLimitInfo.ResetTime - DateTime.UtcNow).TotalSeconds
    };
});

// Wait for ZAP to be ready
var zapClient = app.Services.GetRequiredService<ZapClient>();
await WaitForZapAsync(zapClient);

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Web Zcan backend starting on port 8000");
logger.LogInformation("ZAP daemon URL: {ZapUrl}", zapUrl);
logger.LogInformation("Rate limiting: {RateLimitMax} requests per {RateLimitWindowMinutes} minutes", rateLimitMax, rateLimitWindowMinutes);
logger.LogInformation("Accepting requests from: {AllowedOrigins}", string.Join(", ", allowedOrigins));

app.Run("http://0.0.0.0:8000");

// Helper method to wait for ZAP
static async Task WaitForZapAsync(ZapClient zapClient)
{
    var timeout = TimeSpan.FromSeconds(30);
    var deadline = DateTime.Now.Add(timeout);

    while (DateTime.Now < deadline)
    {
        try
        {
            var version = await zapClient.GetVersionAsync();
            if (!string.IsNullOrEmpty(version))
            {
                var logger = zapClient.GetType()
                    .GetProperty("_logger", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .GetValue(zapClient) as ILogger;
                logger?.LogInformation("ZAP is ready. Version: {Version}", version);
                return;
            }
        }
        catch
        {
            // ZAP not ready yet
        }

        await Task.Delay(2000);
    }

    throw new TimeoutException($"ZAP did not become ready within {timeout}");
}