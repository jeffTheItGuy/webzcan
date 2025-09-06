namespace WebZcan.Models
{
    public class ScanRequest
    {
        public string Target { get; set; } = string.Empty;
        public string TargetName { get; set; } = string.Empty;
    }

    public class ScanResponse
    {
        public string Target { get; set; } = string.Empty;
        public List<Alert> Alerts { get; set; } = new();
        public AlertSummary Summary { get; set; } = new();
        public string Duration { get; set; } = string.Empty;
        public string Timestamp { get; set; } = string.Empty;
        public ScanInfo ScanInfo { get; set; } = new();
    }

    public class Alert
    {
        public string Name { get; set; } = string.Empty;
        public string Risk { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Param { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Evidence { get; set; } = string.Empty;
        public string Cwe { get; set; } = string.Empty;
        public string Wasc { get; set; } = string.Empty;
    }

    public class AlertSummary
    {
        public int Total { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
        public int Info { get; set; }
    }

    public class ScanInfo
    {
        public string SpiderDuration { get; set; } = string.Empty;
        public string ActiveScanDuration { get; set; } = string.Empty;
        public int UrlsFound { get; set; }
        public int TotalRequests { get; set; }
    }
}