namespace Elmansa_api.Models
{
    /// <summary>
    /// Tracks AI API requests and responses for auditing, billing, and analytics.
    /// </summary>
    public class AIRequestLog
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to ApplicationUser
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// Navigation property to the user who made the request
        /// </summary>
        public ApplicationUser User { get; set; } = null!;

        /// <summary>
        /// The prompt/question sent to the AI model
        /// </summary>
        public string Prompt { get; set; } = null!;

        /// <summary>
        /// The AI model response
        /// </summary>
        public string? Response { get; set; }

        /// <summary>
        /// Number of tokens used in the request
        /// </summary>
        public int? InputTokens { get; set; }

        /// <summary>
        /// Number of tokens in the response
        /// </summary>
        public int? OutputTokens { get; set; }

        /// <summary>
        /// Total tokens consumed (input + output)
        /// </summary>
        public int? TotalTokens { get; set; }

        /// <summary>
        /// Estimated or actual cost of the request (in currency units)
        /// </summary>
        public decimal? Cost { get; set; }

        /// <summary>
        /// Request status: Pending, Success, Failed
        /// </summary>
        public RequestStatus Status { get; set; } = RequestStatus.Pending;

        /// <summary>
        /// Error message if request failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// HTTP status code from AI provider
        /// </summary>
        public int? HttpStatusCode { get; set; }

        /// <summary>
        /// Timestamp when the request was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the request was completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// IP address of the requester (for security audit)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// Request duration in milliseconds
        /// </summary>
        public long? DurationMs { get; set; }
    }

    /// <summary>
    /// Enum for request status tracking
    /// </summary>
    public enum RequestStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        RateLimited = 3,
        QuotaExceeded = 4
    }
}
