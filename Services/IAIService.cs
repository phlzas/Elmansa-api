using Elmansa_api.Models;
using Elmansa_api.RequestResponse;

namespace Elmansa_api.Services
{
    /// <summary>
    /// Represents the response from the AI provider
    /// </summary>
    public class AIProviderResponse
    {
        public string? Content { get; set; }
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public int? HttpStatusCode { get; set; }
    }

    /// <summary>
    /// Service contract for AI operations
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Send a prompt to the AI model and get a response
        /// </summary>
        Task<AIProviderResponse> SendPromptAsync(
            string prompt,
            string model = "gemini-2.5-flash-light",
            float? temperature = null,
            int? maxTokens = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Estimate tokens for a prompt before sending
        /// </summary>
        Task<int> EstimateTokensAsync(string prompt, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculate cost for token usage
        /// </summary>
        decimal CalculateCost(int inputTokens, int outputTokens);
    }
}
