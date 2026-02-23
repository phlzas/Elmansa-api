using Elmansa_api.Data;
using Elmansa_api.Models;
using Elmansa_api.RequestResponse;
using Elmansa_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace Elmansa_api.Controllers
{
    /// <summary>
    /// Controller for AI operations and educational assistance
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly IRateLimitService _rateLimitService;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<AIController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public AIController(
            IAIService aiService,
            IRateLimitService rateLimitService,
            AppDbContext dbContext,
            ILogger<AIController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _aiService = aiService;
            _rateLimitService = rateLimitService;
            _dbContext = dbContext;
            _logger = logger;
            _userManager = userManager;
        }

        /// <summary>
        /// Send a prompt to the AI assistant
        /// </summary>
        /// <param name="request">The AI request containing the prompt</param>
        /// <returns>AI response with usage details</returns>
        [HttpPost("ask")]
        [ProducesResponseType(typeof(AIResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(RateLimitExceededDto), StatusCodes.Status429TooManyRequests)]
        [ProducesResponseType(typeof(QuotaExceededDto), StatusCodes.Status402PaymentRequired)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AskAssistant([FromBody] AIRequestDto request)
        {
            try
            {
                // Validate request
                if (string.IsNullOrWhiteSpace(request.Prompt))
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Prompt cannot be empty",
                        ErrorCode = 400
                    });
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Unable to extract user ID from claims");
                    return Unauthorized();
                }

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var sw = Stopwatch.StartNew();

                // Estimate tokens
                var estimatedTokens = await _aiService.EstimateTokensAsync(request.Prompt);

                // Check rate limits
                var rateLimitCheck = await _rateLimitService.CheckRateLimitAsync(
                    userId, estimatedTokens, ipAddress);

                if (!rateLimitCheck.IsAllowed)
                {
                    var quotaInfo = rateLimitCheck.QuotaStatus;

                    if (rateLimitCheck.DenyReason?.Contains("quota", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogWarning(
                            "Quota exceeded for user {UserId}: {Reason}",
                            userId, rateLimitCheck.DenyReason);

                        return StatusCode(
                            StatusCodes.Status402PaymentRequired,
                            new QuotaExceededDto
                            {
                                Message = rateLimitCheck.DenyReason,
                                ErrorCode = 402,
                                DailyResetAt = quotaInfo?.DailyResetAt ?? DateTime.UtcNow.AddDays(1),
                                MonthlyResetAt = quotaInfo?.MonthlyResetAt ?? DateTime.UtcNow.AddMonths(1),
                                DailyTokensUsed = (quotaInfo?.DailyResetAt.Month == DateTime.UtcNow.Month ? quotaInfo?.DailyTokensRemaining ?? 0 : 0),
                                MonthlyTokensUsed = quotaInfo?.MonthlyTokensRemaining ?? 0
                            });
                    }

                    return StatusCode(
                        StatusCodes.Status429TooManyRequests,
                        new RateLimitExceededDto
                        {
                            Message = rateLimitCheck.DenyReason ?? "Rate limit exceeded",
                            ErrorCode = 429,
                            RetryAfterSeconds = rateLimitCheck.RetryAfterSeconds
                        });
                }

                // Call AI service
                var aiResponse = await _aiService.SendPromptAsync(
                    request.Prompt,
                    request.Model ?? "gemini-2.5-flash-light",
                    request.Temperature,
                    request.MaxTokens);

                if (!aiResponse.IsSuccess)
                {
                    sw.Stop();

                    // Log failed request
                    await LogAIRequestAsync(
                        userId,
                        request.Prompt,
                        null,
                        0,
                        0,
                        0,
                        RequestStatus.Failed,
                        aiResponse.ErrorMessage,
                        aiResponse.HttpStatusCode,
                        ipAddress,
                        sw.ElapsedMilliseconds);

                    return StatusCode(
                        aiResponse.HttpStatusCode ?? 500,
                        new ErrorResponseDto
                        {
                            Message = aiResponse.ErrorMessage ?? "Failed to process your request",
                            ErrorCode = aiResponse.HttpStatusCode ?? 500
                        });
                }

                sw.Stop();

                // Calculate cost
                var cost = _aiService.CalculateCost(
                    aiResponse.InputTokens,
                    aiResponse.OutputTokens);

                // Record usage
                await _rateLimitService.RecordUsageAsync(userId, aiResponse.InputTokens + aiResponse.OutputTokens);

                // Log successful request
                var logEntry = await LogAIRequestAsync(
                    userId,
                    request.Prompt,
                    aiResponse.Content,
                    aiResponse.InputTokens,
                    aiResponse.OutputTokens,
                    (int)(cost * 100), // Store as cents
                    RequestStatus.Success,
                    null,
                    200,
                    ipAddress,
                    sw.ElapsedMilliseconds);

                // Get updated quota status
                var quotaStatus = await _rateLimitService.GetQuotaStatusAsync(userId);

                _logger.LogInformation(
                    "Successfully processed AI request for user {UserId}. Tokens: {InputTokens}+{OutputTokens}, Cost: {Cost}",
                    userId, aiResponse.InputTokens, aiResponse.OutputTokens, cost);

                return Ok(new AIResponseDto
                {
                    RequestLogId = logEntry.Id,
                    Response = aiResponse.Content!,
                    TokenUsage = new TokenUsageDto
                    {
                        InputTokens = aiResponse.InputTokens,
                        OutputTokens = aiResponse.OutputTokens,
                        TotalTokens = aiResponse.InputTokens + aiResponse.OutputTokens
                    },
                    Cost = cost,
                    QuotaStatus = new RequestResponse.QuotaStatusDto
                    {
                        DailyTokensRemaining = quotaStatus?.DailyTokensRemaining ?? 0,
                        MonthlyTokensRemaining = quotaStatus?.MonthlyTokensRemaining ?? 0,
                        RequestsRemainingThisMinute = quotaStatus?.RequestsRemainingThisMinute ?? 0,
                        IsSuspended = quotaStatus?.IsSuspended ?? false,
                        SuspensionReason = quotaStatus?.SuspensionReason
                    },
                    ProcessedAt = DateTime.UtcNow,
                    DurationMs = sw.ElapsedMilliseconds
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in AskAssistant: {Message}", ex.Message);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new ErrorResponseDto
                    {
                        Message = "An unexpected error occurred",
                        Details = ex.Message,
                        ErrorCode = 500
                    });
            }
        }

        /// <summary>
        /// Get current usage quota status
        /// </summary>
        [HttpGet("quota-status")]
        [ProducesResponseType(typeof(RequestResponse.QuotaStatusDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetQuotaStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var quotaStatus = await _rateLimitService.GetQuotaStatusAsync(userId);
            if (quotaStatus == null)
            {
                return NotFound();
            }

            return Ok(new RequestResponse.QuotaStatusDto
            {
                DailyTokensRemaining = quotaStatus.DailyTokensRemaining,
                MonthlyTokensRemaining = quotaStatus.MonthlyTokensRemaining,
                RequestsRemainingThisMinute = quotaStatus.RequestsRemainingThisMinute,
                IsSuspended = quotaStatus.IsSuspended,
                SuspensionReason = quotaStatus.SuspensionReason
            });
        }

        /// <summary>
        /// Get AI request history (for current user)
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(IEnumerable<AIRequestHistoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistory([FromQuery] int pageSize = 10, [FromQuery] int pageNumber = 1)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var history = _dbContext.AIRequestLogs
                .Where(log => log.UserId == userId)
                .OrderByDescending(log => log.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new AIRequestHistoryDto
                {
                    Id = log.Id,
                    Prompt = log.Prompt,
                    Status = log.Status.ToString(),
                    TotalTokens = log.TotalTokens ?? 0,
                    Cost = log.Cost ?? 0,
                    CreatedAt = log.CreatedAt,
                    DurationMs = log.DurationMs ?? 0
                })
                .ToList();

            return Ok(history);
        }

        private async Task<AIRequestLog> LogAIRequestAsync(
            string userId,
            string prompt,
            string? response,
            int inputTokens,
            int outputTokens,
            int costInCents,
            RequestStatus status,
            string? errorMessage,
            int? httpStatusCode,
            string? ipAddress,
            long durationMs)
        {
            var logEntry = new AIRequestLog
            {
                UserId = userId,
                Prompt = prompt,
                Response = response,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                TotalTokens = inputTokens + outputTokens,
                Cost = costInCents / 100m,
                Status = status,
                ErrorMessage = errorMessage,
                HttpStatusCode = httpStatusCode,
                IpAddress = ipAddress,
                DurationMs = durationMs,
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            };

            _dbContext.AIRequestLogs.Add(logEntry);
            await _dbContext.SaveChangesAsync();

            return logEntry;
        }
    }

    /// <summary>
    /// DTO for AI request history
    /// </summary>
    public class AIRequestHistoryDto
    {
        public int Id { get; set; }
        public string Prompt { get; set; } = null!;
        public string Status { get; set; } = null!;
        public int TotalTokens { get; set; }
        public decimal Cost { get; set; }
        public DateTime CreatedAt { get; set; }
        public long DurationMs { get; set; }
    }
}
