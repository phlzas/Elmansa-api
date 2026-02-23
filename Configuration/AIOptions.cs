namespace Elmansa_api.Configuration
{
    /// <summary>
    /// Configuration options for AI service integration
    /// </summary>
    public class AIOptions
    {
        public const string SectionName = "AISettings";

        /// <summary>
        /// API key for the AI provider (stored in user secrets in production)
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// Base endpoint URL for the AI API
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Default model to use
        /// </summary>
        public string DefaultModel { get; set; } = "gemini-3-flash-preview";

        /// <summary>
        /// Request timeout in seconds
        /// </summary>
        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Maximum tokens per request
        /// </summary>
        public int MaxTokensPerRequest { get; set; } = 2000;

        /// <summary>
        /// System prompt/instructions for the AI
        /// </summary>
        public string? SystemPrompt { get; set; }

        /// <summary>
        /// Input token price per 1M tokens
        /// </summary>
        public decimal InputTokenPrice { get; set; } = 0.075m;

        /// <summary>
        /// Output token price per 1M tokens
        /// </summary>
        public decimal OutputTokenPrice { get; set; } = 0.3m;

        /// <summary>
        /// Retry policy: number of retries on failure
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Initial delay for exponential backoff (milliseconds)
        /// </summary>
        public int RetryDelayMs { get; set; } = 1000;
    }
}
