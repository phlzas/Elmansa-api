namespace Elmansa_api.Configuration
{
    /// <summary>
    /// Configuration options for rate limiting and quotas
    /// </summary>
    public class RateLimitOptions
    {
        public const string SectionName = "RateLimitSettings";

        /// <summary>
        /// Default daily token limit per user
        /// </summary>
        public int DefaultDailyTokenLimit { get; set; } = 10000;

        /// <summary>
        /// Default monthly token limit per user
        /// </summary>
        public int DefaultMonthlyTokenLimit { get; set; } = 100000;

        /// <summary>
        /// Default requests per minute limit
        /// </summary>
        public int DefaultRequestsPerMinute { get; set; } = 10;

        /// <summary>
        /// Enable per-IP rate limiting (in addition to per-user)
        /// </summary>
        public bool EnableIpRateLimiting { get; set; } = true;

        /// <summary>
        /// Requests per minute per IP address
        /// </summary>
        public int IpRequestsPerMinute { get; set; } = 30;

        /// <summary>
        /// Number of failed requests before temporary suspension (0 = disabled)
        /// </summary>
        public int FailedRequestThreshold { get; set; } = 10;

        /// <summary>
        /// Duration of temporary suspension in minutes
        /// </summary>
        public int SuspensionDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Automatically reset quotas at this time daily (24-hour format, UTC)
        /// </summary>
        public string DailyResetTime { get; set; } = "00:00";

        /// <summary>
        /// Day of month for monthly quota reset (1-31)
        /// </summary>
        public int MonthlyResetDay { get; set; } = 1;
    }
}
