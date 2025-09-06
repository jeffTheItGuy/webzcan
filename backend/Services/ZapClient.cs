using System.Text.Json;
using Microsoft.Extensions.Logging;
using WebZcan.Models;

namespace WebZcan.Services
{
    public class ZapClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly ILogger<ZapClient> _logger; // Now used properly

        // Known legitimate testing targets - these are safe and legal to scan
        private static readonly HashSet<string> LegitimateTargets = new()
        {
            "demo.testfire.net",        // IBM Security AppScan Demo
            "testphp.vulnweb.com",      // Acunetix Test Site
            "httpbin.org",              // HTTPBin Test Service
            "juice-shop.herokuapp.com", // OWASP Juice Shop
            "github.com",               // GitHub - well-secured public site
            "developer.mozilla.org"     // Mozilla MDN - security-focused documentation
        };

        public ZapClient(string baseUrl, string apiKey, ILogger<ZapClient> logger)
        {
            _baseUrl = baseUrl;
            _apiKey = apiKey;
            _httpClient = new HttpClient();
            _logger = logger; // Injected and used
        }

        private async Task<string> MakeRequestAsync(string endpoint, Dictionary<string, string>? parameters = null)
        {
            var url = $"{_baseUrl}{endpoint}";
            var queryParams = new List<string>();

            if (!string.IsNullOrEmpty(_apiKey))
            {
                queryParams.Add($"zapapikey={_apiKey}");
            }

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    queryParams.Add($"{param.Key}={Uri.EscapeDataString(param.Value)}");
                }
            }

            if (queryParams.Any())
            {
                url += "?" + string.Join("&", queryParams);
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ZAP API returned non-success status: {StatusCode} for URL: {Url}", response.StatusCode, url);
                throw new HttpRequestException($"ZAP API returned status {response.StatusCode}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetVersionAsync()
        {
            try
            {
                var response = await MakeRequestAsync("/JSON/core/view/version/");
                var json = JsonDocument.Parse(response);
                return json.RootElement.GetProperty("version").GetString() ?? "";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve ZAP version");
                return "";
            }
        }

        public async Task ClearSessionAsync()
        {
            _logger.LogInformation("Clearing ZAP session...");
            await MakeRequestAsync("/JSON/core/action/newSession/", new Dictionary<string, string>
            {
                ["name"] = $"scan_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}"
            });
        }

        public async Task<TimeSpan> SpiderAsync(string targetUrl)
        {
            _logger.LogInformation("Starting spider scan for target: {TargetUrl}", targetUrl);
            var startTime = DateTime.Now;

            // Start spider
            await MakeRequestAsync("/JSON/spider/action/scan/", new Dictionary<string, string>
            {
                ["url"] = targetUrl,
                ["maxChildren"] = "10",
                ["recurse"] = "true",
                ["contextName"] = "",
                ["subtreeOnly"] = "false"
            });

            // Wait for completion with progress updates
            while (true)
            {
                var statusResponse = await MakeRequestAsync("/JSON/spider/view/status/");
                var statusJson = JsonDocument.Parse(statusResponse);
                var statusValue = statusJson.RootElement.GetProperty("status").GetString();

                if (int.TryParse(statusValue, out var statusInt) && statusInt >= 100)
                {
                    break;
                }

                _logger.LogInformation("Spider progress: {Progress}%", statusValue);
                await Task.Delay(2000);
            }

            var duration = DateTime.Now - startTime;
            _logger.LogInformation("Spider scan completed in {Duration}", duration);
            return duration;
        }

        public async Task<TimeSpan> ActiveScanAsync(string targetUrl)
        {
            _logger.LogInformation("Starting active scan for target: {TargetUrl}", targetUrl);
            var startTime = DateTime.Now;

            // Start active scan
            await MakeRequestAsync("/JSON/ascan/action/scan/", new Dictionary<string, string>
            {
                ["url"] = targetUrl,
                ["recurse"] = "true",
                ["inScopeOnly"] = "false",
                ["scanPolicyName"] = "",
                ["method"] = "",
                ["postData"] = ""
            });

            // Wait for completion with progress updates
            while (true)
            {
                var statusResponse = await MakeRequestAsync("/JSON/ascan/view/status/");
                var statusJson = JsonDocument.Parse(statusResponse);
                var statusValue = statusJson.RootElement.GetProperty("status").GetString();

                if (int.TryParse(statusValue, out var statusInt) && statusInt >= 100)
                {
                    break;
                }

                _logger.LogInformation("Active scan progress: {Progress}%", statusValue);
                await Task.Delay(3000);
            }

            var duration = DateTime.Now - startTime;
            _logger.LogInformation("Active scan completed in {Duration}", duration);
            return duration;
        }

        public async Task<(int urlsFound, int totalRequests)> GetScanStatsAsync()
        {
            try
            {
                var urlsResponse = await MakeRequestAsync("/JSON/spider/view/results/");
                var urlsJson = JsonDocument.Parse(urlsResponse);

                var urlsFound = 0;
                if (urlsJson.RootElement.TryGetProperty("results", out var results) &&
                    results.ValueKind == JsonValueKind.Array)
                {
                    urlsFound = results.GetArrayLength();
                }

                var totalRequests = urlsFound * 5; // Rough estimate
                _logger.LogInformation("Scan stats - URLs found: {UrlsFound}, Estimated requests: {TotalRequests}", urlsFound, totalRequests);
                return (urlsFound, totalRequests);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get scan stats");
                return (0, 0);
            }
        }

        public async Task<List<Alert>> GetAlertsAsync()
        {
            _logger.LogInformation("Retrieving passive alerts from ZAP spider scan");

            var alertsResponse = await MakeRequestAsync("/JSON/core/view/alerts/", new Dictionary<string, string>
            {
                ["baseurl"] = "",
                ["start"] = "",
                ["count"] = "",
                ["riskId"] = ""
            });

            var alertsJson = JsonDocument.Parse(alertsResponse);
            var alerts = new List<Alert>();

            if (alertsJson.RootElement.TryGetProperty("alerts", out var alertsArray) &&
                alertsArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var alertElement in alertsArray.EnumerateArray())
                {
                    var alert = new Alert
                    {
                        Name = GetStringValue(alertElement, "name"),
                        Risk = GetStringValue(alertElement, "risk"),
                        Confidence = GetStringValue(alertElement, "confidence"),
                        Description = GetStringValue(alertElement, "description"),
                        Url = GetStringValue(alertElement, "url"),
                        Param = GetStringValue(alertElement, "param"),
                        Solution = GetStringValue(alertElement, "solution"),
                        Reference = GetStringValue(alertElement, "reference"),
                        Evidence = GetStringValue(alertElement, "evidence"),
                        Cwe = GetStringValue(alertElement, "cweid"),
                        Wasc = GetStringValue(alertElement, "wascid")
                    };
                    alerts.Add(alert);
                }
            }

            _logger.LogInformation("Retrieved {AlertCount} alerts", alerts.Count);
            return alerts;
        }

        private static string GetStringValue(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out var property) &&
                   property.ValueKind == JsonValueKind.String
                ? property.GetString() ?? ""
                : "";
        }

        public static AlertSummary CalculateSummary(List<Alert> alerts)
        {
            var summary = new AlertSummary { Total = alerts.Count };

            foreach (var alert in alerts)
            {
                switch (alert.Risk.ToLowerInvariant())
                {
                    case "high":
                        summary.High++;
                        break;
                    case "medium":
                        summary.Medium++;
                        break;
                    case "low":
                        summary.Low++;
                        break;
                    case "informational":
                        summary.Info++;
                        break;
                }
            }

            return summary;
        }

        public string ResolveAndValidateTargetUrl(string target)
        {
            // All targets should already be legitimate - no local resolution needed
            var resolved = target;

            // Validate the target URL for safety
            if (!IsLegitimateTarget(resolved))
            {
                _logger.LogWarning("Unauthorized target URL attempted: {Target}", target);
                throw new ArgumentException($"Target URL is not in the list of authorized testing sites: {target}");
            }

            _logger.LogInformation("Validated target URL: {ResolvedUrl}", resolved);
            return resolved;
        }

        private bool IsLegitimateTarget(string targetUrl)
        {
            try
            {
                var uri = new Uri(targetUrl);
                var host = uri.Host.ToLowerInvariant();

                // Check if it's a known legitimate testing target
                if (LegitimateTargets.Any(legitimate => host == legitimate))
                {
                    _logger.LogInformation("Scanning authorized target: {Host}", host);
                    return true;
                }

                _logger.LogWarning("Target URL not in authorized list: {Host}. Only legitimate testing sites are allowed.", host);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid target URL format: {TargetUrl}", targetUrl);
                return false;
            }
        }
    }
}