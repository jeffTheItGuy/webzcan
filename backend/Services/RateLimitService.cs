using System.Collections.Concurrent;

namespace WebZcan.Services
{
    public class RateLimitService
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _requestHistory = new();
        private readonly int _maxRequests;
        private readonly TimeSpan _timeWindow;
        private readonly ILogger<RateLimitService> _logger;

        public RateLimitService(ILogger<RateLimitService> logger, int maxRequests = 3, TimeSpan? timeWindow = null)
        {
            _logger = logger;
            _maxRequests = maxRequests;
            _timeWindow = timeWindow ?? TimeSpan.FromHours(1);
            
            // Start cleanup task to prevent memory leaks
            StartCleanupTask();
        }

        public bool IsAllowed(string ipAddress)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _timeWindow;

            // Get or create request history for this IP
            var requests = _requestHistory.GetOrAdd(ipAddress, _ => new List<DateTime>());

            lock (requests)
            {
                // Remove old requests outside the time window
                requests.RemoveAll(time => time < windowStart);

                // Check if within limit
                if (requests.Count >= _maxRequests)
                {
                    _logger.LogWarning("Rate limit exceeded for IP: {IPAddress}. {Count}/{Max} requests in last {TimeWindow}", 
                        ipAddress, requests.Count, _maxRequests, _timeWindow);
                    return false;
                }

                // Add current request
                requests.Add(now);
                _logger.LogInformation("Request allowed for IP: {IPAddress}. {Count}/{Max} requests in last {TimeWindow}", 
                    ipAddress, requests.Count, _maxRequests, _timeWindow);
                return true;
            }
        }

        public RateLimitInfo GetRateLimitInfo(string ipAddress)
        {
            var now = DateTime.UtcNow;
            var windowStart = now - _timeWindow;

            if (!_requestHistory.TryGetValue(ipAddress, out var requests))
            {
                return new RateLimitInfo
                {
                    RequestsUsed = 0,
                    RequestsAllowed = _maxRequests,
                    WindowSizeMinutes = (int)_timeWindow.TotalMinutes,
                    ResetTime = now.Add(_timeWindow)
                };
            }

            lock (requests)
            {
                // Remove old requests
                requests.RemoveAll(time => time < windowStart);

                var oldestRequest = requests.FirstOrDefault();
                var resetTime = oldestRequest != default ? oldestRequest.Add(_timeWindow) : now.Add(_timeWindow);

                return new RateLimitInfo
                {
                    RequestsUsed = requests.Count,
                    RequestsAllowed = _maxRequests,
                    WindowSizeMinutes = (int)_timeWindow.TotalMinutes,
                    ResetTime = resetTime
                };
            }
        }

        private void StartCleanupTask()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(30)); // Cleanup every 30 minutes
                    CleanupOldEntries();
                }
            });
        }

        private void CleanupOldEntries()
        {
            var cutoff = DateTime.UtcNow - _timeWindow - TimeSpan.FromHours(1); // Extra buffer
            var keysToRemove = new List<string>();

            foreach (var kvp in _requestHistory)
            {
                lock (kvp.Value)
                {
                    kvp.Value.RemoveAll(time => time < cutoff);
                    
                    // If no recent requests, mark for removal
                    if (!kvp.Value.Any())
                    {
                        keysToRemove.Add(kvp.Key);
                    }
                }
            }

            // Remove empty entries
            foreach (var key in keysToRemove)
            {
                _requestHistory.TryRemove(key, out _);
            }

            _logger.LogInformation("Cleaned up {Count} empty rate limit entries", keysToRemove.Count);
        }
    }

    public class RateLimitInfo
    {
        public int RequestsUsed { get; set; }
        public int RequestsAllowed { get; set; }
        public int WindowSizeMinutes { get; set; }
        public DateTime ResetTime { get; set; }
    }
}