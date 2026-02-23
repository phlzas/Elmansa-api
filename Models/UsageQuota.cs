namespace Elmansa_api.Models
{
    /// <summary>
    /// Tracks daily and monthly usage limits per user.
    /// Automatically resets based on configurable periods.
    /// </summary>
    public class UsageQuota
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to ApplicationUser
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Navigation property to the user
        /// </summary>
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// Daily token limit for this user
        /// </summary>
        public int DailyTokenLimit { get; set; } = 10000;

        /// <summary>
        /// Monthly token limit for this user
        /// </summary>
        public int MonthlyTokenLimit { get; set; } = 100000;

        /// <summary>
        /// Tokens consumed today
        /// </summary>
        public int DailyTokensUsed { get; set; } = 0;

        /// <summary>
        /// Tokens consumed this month
        /// </summary>
        public int MonthlyTokensUsed { get; set; } = 0;

        /// <summary>
        /// Maximum requests per minute (rate limiting)
        /// </summary>
        public int RequestsPerMinuteLimit { get; set; } = 10;

        /// <summary>
        /// Number of requests made in the current minute
        /// </summary>
        public int CurrentMinuteRequests { get; set; } = 0;

        /// <summary>
        /// Last time daily quota was reset
        /// </summary>
        public DateTime DailyResetAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time monthly quota was reset
        /// </summary>
        public DateTime MonthlyResetAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time minute-level quota was reset
        /// </summary>
        public DateTime MinuteResetAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether this user is temporarily banned due to abuse
        /// </summary>
        public bool IsSuspended { get; set; } = false;

        /// <summary>
        /// Reason for suspension if applicable
        /// </summary>
        public string? SuspensionReason { get; set; }

        /// <summary>
        /// When the suspension will be lifted (null for indefinite)
        /// </summary>
        public DateTime? SuspensionUntil { get; set; }

        /// <summary>
        /// When this quota record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this quota record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
