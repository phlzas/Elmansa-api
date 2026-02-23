using Elmansa_api.Configuration;
using Elmansa_api.Models;
using Elmansa_api.Services;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Elmansa_api.Services.Implementation
{
    /// <summary>
    /// Implementation of AI service using Google Gemini API
    /// </summary>
    public class GeminiAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiAIService> _logger;
        private readonly AIOptions _options;

        public GeminiAIService(
            HttpClient httpClient,
            ILogger<GeminiAIService> logger,
            IOptions<AIOptions> options)
        {
            _httpClient = httpClient;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Send prompt to Gemini API
        /// </summary>
        public async Task<AIProviderResponse> SendPromptAsync(
            string prompt,
            string model = "gemini-2.5-flash-light",
            float? temperature = null,
            int? maxTokens = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    _logger.LogError("AI API key is not configured");
                    return new AIProviderResponse
                    {
                        IsSuccess = false,
                        ErrorMessage = "AI service is not properly configured"
                    };
                }

                var request = BuildGeminiRequest(prompt, temperature, maxTokens);
                var url = $"{_options.EndpointUrl}{model}:generateContent?key={_options.ApiKey}";

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(_options.RequestTimeoutSeconds));

                var response = await _httpClient.PostAsJsonAsync(url, request, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    };
                    var content = System.Text.Json.JsonSerializer.Deserialize<GeminiResponse>(jsonString, options);
                    var textContent = content?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                    if (!string.IsNullOrEmpty(textContent))
                    {
                        // Estimate tokens from response
                        int inputTokens = EstimateTokenCount(prompt);
                        int outputTokens = EstimateTokenCount(textContent);

                        return new AIProviderResponse
                        {
                            Content = textContent,
                            InputTokens = inputTokens,
                            OutputTokens = outputTokens,
                            IsSuccess = true,
                            HttpStatusCode = (int)response.StatusCode
                        };
                    }
                }

                // Read the error response body for better diagnostics
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("AI API returned status {StatusCode}. Response: {ErrorBody}", 
                    response.StatusCode, errorBody);

                return new AIProviderResponse
                {
                    IsSuccess = false,
                    ErrorMessage = $"AI API returned status {response.StatusCode}. Details: {errorBody}",
                    HttpStatusCode = (int)response.StatusCode
                };
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error calling AI API: {Message}", ex.Message);
                return new AIProviderResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "Failed to connect to AI service"
                };
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "AI API request timeout");
                return new AIProviderResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "AI service request timed out"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling AI API: {Message}", ex.Message);
                return new AIProviderResponse
                {
                    IsSuccess = false,
                    ErrorMessage = "An unexpected error occurred while processing your request"
                };
            }
        }

        /// <summary>
        /// Estimate token count for a text (rough approximation)
        /// </summary>
        public async Task<int> EstimateTokensAsync(string prompt, CancellationToken cancellationToken = default)
        {
            return EstimateTokenCount(prompt);
        }

        /// <summary>
        /// Calculate cost based on token usage
        /// </summary>
        public decimal CalculateCost(int inputTokens, int outputTokens)
        {
            var inputCost = (inputTokens / 1_000_000m) * _options.InputTokenPrice;
            var outputCost = (outputTokens / 1_000_000m) * _options.OutputTokenPrice;
            return inputCost + outputCost;
        }

        private GeminiRequest BuildGeminiRequest(string prompt, float? temperature, int? maxTokens)
        {
            return new GeminiRequest
            {
                Contents = new[]
                {
                    new Content
                    {
                        Parts = new[]
                        {
                            new Part { Text = prompt }
                        }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = temperature ?? 0.7f,
                    MaxOutputTokens = maxTokens ?? _options.MaxTokensPerRequest
                },
                SystemInstruction = string.IsNullOrEmpty(_options.SystemPrompt)
                    ? null
                    : new Content
                    {
                        Parts = new[]
                        {
                            new Part { Text = _options.SystemPrompt }
                        }
                    }
            };
        }

        private static int EstimateTokenCount(string text)
        {
            // Rough estimation: ~4 characters per token on average
            // This is a simplified approach; Gemini API provides exact counts
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        #region Gemini API Models

        private class GeminiRequest
        {
            [JsonPropertyName("contents")]
            public Content[] Contents { get; set; } = Array.Empty<Content>();

            [JsonPropertyName("generationConfig")]
            public GenerationConfig GenerationConfig { get; set; } = new();

            [JsonPropertyName("systemInstruction")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Content? SystemInstruction { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public Part[] Parts { get; set; } = Array.Empty<Part>();
        }

        private class Part
        {
            [JsonPropertyName("text")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? Text { get; set; }
        }

        private class GenerationConfig
        {
            [JsonPropertyName("temperature")]
            public float Temperature { get; set; } = 0.7f;

            [JsonPropertyName("maxOutputTokens")]
            public int MaxOutputTokens { get; set; } = 2000;
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public Candidate[]? Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        #endregion
    }
}
