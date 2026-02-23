namespace Elmansa_api.RequestResponse
{
    /// <summary>
    /// DTO for incoming AI request from client
    /// </summary>
    public class AIRequestDto
    {
        /// <summary>
        /// The question or prompt to send to the AI model
        /// </summary>
        public string Prompt { get; set; } = null!;

        /// <summary>
        /// Optional model identifier (default: gemini-2.5-flash-light)
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// Optional temperature for response creativity (0-1)
        /// </summary>
        public float? Temperature { get; set; }

        /// <summary>
        /// Optional maximum tokens to generate
        /// </summary>
        public int? MaxTokens { get; set; }
    }

    /// <summary>
    /// DTO for AI response to client
    /// </summary>
    public class AIResponseDto
    {
        /// <summary>
        /// Unique identifier for this request
        /// </summary>
        public int RequestLogId { get; set; }

        /// <summary>
        /// The AI-generated response
        /// </summary>
        public string Response { get; set; } = null!;

        /// <summary>
        /// Tokens used in this request
        /// </summary>
        public TokenUsageDto TokenUsage { get; set; } = null!;

        /// <summary>
        /// Estimated cost of this request
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Current user quota status
        /// </summary>
        public QuotaStatusDto QuotaStatus { get; set; } = null!;

        /// <summary>
        /// When the request was processed
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// How long the request took in milliseconds
        /// </summary>
        public long DurationMs { get; set; }
    }

    /// <summary>
    /// Token usage breakdown
    /// </summary>
    public class TokenUsageDto
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens { get; set; }
    }

    /// <summary>
    /// Current quota status for the user
    /// </summary>
    public class QuotaStatusDto
    {
        /// <summary>
        /// Daily tokens remaining
        /// </summary>
        public int DailyTokensRemaining { get; set; }

        /// <summary>
        /// Monthly tokens remaining
        /// </summary>
        public int MonthlyTokensRemaining { get; set; }

        /// <summary>
        /// Requests remaining this minute
        /// </summary>
        public int RequestsRemainingThisMinute { get; set; }

        /// <summary>
        /// Whether user is currently suspended
        /// </summary>
        public bool IsSuspended { get; set; }

        /// <summary>
        /// Suspension reason if applicable
        /// </summary>
        public string? SuspensionReason { get; set; }
    }

    /// <summary>
    /// Error response DTO
    /// </summary>
    public class ErrorResponseDto
    {
        public string Message { get; set; } = null!;
        public string? Details { get; set; }
        public int ErrorCode { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Rate limit exceeded response
    /// </summary>
    public class RateLimitExceededDto : ErrorResponseDto
    {
        /// <summary>
        /// When the user can retry (Unix timestamp)
        /// </summary>
        public long RetryAfterSeconds { get; set; }
    }

    /// <summary>
    /// Quota exceeded response
    /// </summary>
    public class QuotaExceededDto : ErrorResponseDto
    {
        /// <summary>
        /// When daily quota resets
        /// </summary>
        public DateTime DailyResetAt { get; set; }

        /// <summary>
        /// When monthly quota resets
        /// </summary>
        public DateTime MonthlyResetAt { get; set; }

        /// <summary>
        /// Current daily tokens used
        /// </summary>
        public int DailyTokensUsed { get; set; }

        /// <summary>
        /// Current monthly tokens used
        /// </summary>
        public int MonthlyTokensUsed { get; set; }
    }
}
