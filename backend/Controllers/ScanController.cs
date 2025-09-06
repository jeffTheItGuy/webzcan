using Microsoft.AspNetCore.Mvc;
using WebZcan.Models;
using WebZcan.Services;

namespace WebZcan.Controllers
{
    [ApiController]
    [Route("api")]
    public class ScanController : ControllerBase
    {
        private readonly ZapClient _zapClient;
        private readonly ILogger<ScanController> _logger;

        public ScanController(ZapClient zapClient, ILogger<ScanController> logger)
        {
            _zapClient = zapClient;
            _logger = logger;
        }

        [HttpPost("scan")]
        public async Task<ActionResult<ScanResponse>> Scan([FromBody] ScanRequest request)
        {
            if (string.IsNullOrEmpty(request.Target))
            {
                return BadRequest("Target is required");
            }

            // Trim and normalize input
            var target = request.Target.Trim();

            _logger.LogInformation("Starting spider scan for target: {Target} (Name: {TargetName})", 
                target, request.TargetName);

            var startTime = DateTime.Now;

            try
            {
                // Clear previous session
                try
                {
                    await _zapClient.ClearSessionAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Failed to clear ZAP session: {Error}", ex.Message);
                }

                // Validate and resolve target URL
                string targetUrl;
                try 
                {
                    targetUrl = _zapClient.ResolveAndValidateTargetUrl(target);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError("Invalid target URL: {Error}", ex.Message);
                    return BadRequest($"Invalid target: {ex.Message}");
                }

                _logger.LogInformation("Validated and normalized target URL: {TargetUrl}", targetUrl);

                // Check if target is reachable before scanning
                if (!await IsTargetReachable(targetUrl))
                {
                    return BadRequest($"Target is not reachable: {targetUrl}");
                }

                // Perform spider scan only (passive reconnaissance)
                var spiderDuration = await _zapClient.SpiderAsync(targetUrl);

                // Gather scan stats
                var (urlsFound, totalRequests) = await _zapClient.GetScanStatsAsync();

                // Retrieve alerts generated during spidering
                var alerts = await _zapClient.GetAlertsAsync();
                var summary = ZapClient.CalculateSummary(alerts);

                var totalDuration = DateTime.Now - startTime;

                var response = new ScanResponse
                {
                    Target = target, // Return original user input
                    Alerts = alerts,
                    Summary = summary,
                    Duration = FormatDuration(totalDuration),
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    ScanInfo = new ScanInfo
                    {
                        SpiderDuration = FormatDuration(spiderDuration),
                        ActiveScanDuration = "N/A", // Active scanning disabled
                        UrlsFound = urlsFound,
                        TotalRequests = totalRequests
                    }
                };

                _logger.LogInformation(
                    "Spider scan completed for {TargetUrl} in {Duration}. Found {UrlCount} URLs and {AlertCount} alerts (High:{High}, Medium:{Medium})",
                    targetUrl, totalDuration, urlsFound, alerts.Count,
                    summary.High, summary.Medium);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError("Target validation failed: {Error}", ex.Message);
                return BadRequest($"Invalid target: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during scan for target: {Target}", target);
                return StatusCode(500, $"Scan failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the target is reachable with a short timeout.
        /// Accepts 2xx, 401, 403 as "reachable" since they indicate the host responded.
        /// </summary>
        private async Task<bool> IsTargetReachable(string targetUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("WebZcan-Scanner");

                var response = await httpClient.GetAsync(targetUrl);
                
                _logger.LogInformation("Reachability check for {Url}: {StatusCode}", targetUrl, response.StatusCode);
                
                return response.IsSuccessStatusCode ||
                       response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
                       response.StatusCode == System.Net.HttpStatusCode.Forbidden;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Target not reachable: {Url} - {Exception}", targetUrl, ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Returns a list of known, legal, and vulnerable targets suitable for security testing.
        /// Removed httpbin.org and fixed URLs with trailing spaces.
        /// </summary>
        [HttpGet("targets")]
        public ActionResult<object> GetKnownTargets()
        {
            var targets = new
            {
                LegalTestingTargets = new[]
                {
                    new 
                    { 
                        name = "IBM Security AppScan Demo", 
                        url = "https://demo.testfire.net", 
                        description = "Vulnerable banking application with intentional security flaws." 
                    },
                    new 
                    { 
                        name = "Acunetix Test Site", 
                        url = "http://testphp.vulnweb.com", 
                        description = "Deliberately vulnerable PHP-based web application for testing." 
                    },
                    new 
                    { 
                        name = "OWASP Juice Shop", 
                        url = "https://juice-shop.herokuapp.com", 
                        description = "Modern, gamified vulnerable app with 100+ exploits (including OWASP Top 10)." 
                    },
                    new 
                    { 
                        name = "Google Gruyere", 
                        url = "https://google-gruyere.appspot.com", 
                        description = "Google's codelab on web exploits and defenses. May be intermittently available." 
                    }
                },
                Disclaimer = "Only scan applications you own or have explicit permission to test. Unauthorized scanning is illegal."
            };

            return Ok(targets);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            var totalSeconds = duration.TotalSeconds;
            if (totalSeconds >= 60)
            {
                var minutes = (int)(totalSeconds / 60);
                var seconds = totalSeconds % 60;
                return $"{minutes}m{seconds:F1}s";
            }
            return $"{totalSeconds:F1}s";
        }
    }
}