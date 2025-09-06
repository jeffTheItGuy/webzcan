using Microsoft.AspNetCore.Mvc;
using WebZcan.Services;
using System.Net;

namespace WebZcan.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly RateLimitService _rateLimitService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(RateLimitService rateLimitService, ILogger<AdminController> logger)
        {
            _rateLimitService = rateLimitService;
            _logger = logger;
        }

        /// <summary>
        /// Get rate limit statistics for monitoring purposes
        /// In production, this should be protected by authentication
        /// </summary>
        [HttpGet("ratelimit/stats")]
        public ActionResult GetRateLimitStats()
        {
            // Get client IP for context
            var clientIp = GetClientIpAddress();
            var currentIpInfo = _rateLimitService.GetRateLimitInfo(clientIp);

            return Ok(new
            {
                currentIp = clientIp,
                currentIpUsage = new
                {
                    used = currentIpInfo.RequestsUsed,
                    limit = currentIpInfo.RequestsAllowed,
                    remaining = currentIpInfo.RequestsAllowed - currentIpInfo.RequestsUsed,
                    resetTime = currentIpInfo.ResetTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    windowMinutes = currentIpInfo.WindowSizeMinutes
                },
                rateLimitConfig = new
                {
                    maxRequestsPerWindow = currentIpInfo.RequestsAllowed,
                    windowSizeMinutes = currentIpInfo.WindowSizeMinutes
                },
                serverTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        /// <summary>
        /// Health check specifically for rate limiting functionality
        /// </summary>
        [HttpGet("health")]
        public ActionResult GetAdminHealth()
        {
            var clientIp = GetClientIpAddress();
            var canMakeRequest = _rateLimitService.IsAllowed("test-ip-check");
            var rateLimitInfo = _rateLimitService.GetRateLimitInfo(clientIp);

            return Ok(new
            {
                status = "ok",
                rateLimitService = "operational",
                clientIp = clientIp,
                testAllowed = canMakeRequest,
                currentUsage = new
                {
                    used = rateLimitInfo.RequestsUsed,
                    remaining = rateLimitInfo.RequestsAllowed - rateLimitInfo.RequestsUsed,
                    limit = rateLimitInfo.RequestsAllowed
                },
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            });
        }

        private string GetClientIpAddress()
        {
            // Check for forwarded IP headers (common in load balancers/proxies)
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                var firstIp = forwardedFor.Split(',')[0].Trim();
                if (IPAddress.TryParse(firstIp, out _))
                {
                    return firstIp;
                }
            }

            var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp) && IPAddress.TryParse(realIp, out _))
            {
                return realIp;
            }

            // Fallback to connection remote IP
            var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString();
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
    }
}