using Elmansa_api.Data;
using Elmansa_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Elmansa_api.Configuration;

namespace Elmansa_api.Services
{
    /// <summary>
    /// Service contract for rate limiting and quota management
    /// </summary>
    public interface IRateLimitService
    {
        /// <summary>
        /// Check if user is within rate limits and quotas
        /// </summary>
        Task<RateLimitCheckResult> CheckRateLimitAsync(
            string userId,
            int estimatedTokens,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Record token usage after successful AI request
        /// </summary>
        Task RecordUsageAsync(
            string userId,
            int tokensUsed,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get current quota status for a user
        /// </summary>
        Task<QuotaStatus?> GetQuotaStatusAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset daily quota for a user
        /// </summary>
        Task ResetDailyQuotaAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reset monthly quota for a user
        /// </summary>
        Task ResetMonthlyQuotaAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Suspend a user due to abuse
        /// </summary>
        Task SuspendUserAsync(
            string userId,
            string reason,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of rate limit check
    /// </summary>
    public class RateLimitCheckResult
    {
        public bool IsAllowed { get; set; }
        public string? DenyReason { get; set; }
        public long RetryAfterSeconds { get; set; }
        public QuotaStatus? QuotaStatus { get; set; }
    }

    /// <summary>
    /// Current quota status
    /// </summary>
    public class QuotaStatus
    {
        public int DailyTokensRemaining { get; set; }
        public int MonthlyTokensRemaining { get; set; }
        public int RequestsRemainingThisMinute { get; set; }
        public bool IsSuspended { get; set; }
        public string? SuspensionReason { get; set; }
        public DateTime? SuspensionUntil { get; set; }
        public DateTime DailyResetAt { get; set; }
        public DateTime MonthlyResetAt { get; set; }
    }
}
