using Elmansa_api.Configuration;
using Elmansa_api.Data;
using Elmansa_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Elmansa_api.Services.Implementation
{
    /// <summary>
    /// Implementation of rate limiting and quota management
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<RateLimitService> _logger;
        private readonly RateLimitOptions _options;
        private readonly Dictionary<string, DateTime> _minuteResetCache = new();

        public RateLimitService(
            AppDbContext dbContext,
            ILogger<RateLimitService> logger,
            IOptions<RateLimitOptions> options)
        {
            _dbContext = dbContext;
            _logger = logger;
            _options = options.Value;
        }

        /// <summary>
        /// Check if user is within rate limits and quotas
        /// </summary>
        public async Task<RateLimitCheckResult> CheckRateLimitAsync(
            string userId,
            int estimatedTokens,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var quota = await _dbContext.UsageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

                if (quota == null)
                {
                    quota = await CreateDefaultQuotaAsync(userId, cancellationToken);
                }

                // Check if user is suspended
                if (quota.IsSuspended)
                {
                    if (quota.SuspensionUntil.HasValue && quota.SuspensionUntil > DateTime.UtcNow)
                    {
                        var remainingSeconds = (long)(quota.SuspensionUntil.Value - DateTime.UtcNow).TotalSeconds;
                        return new RateLimitCheckResult
                        {
                            IsAllowed = false,
                            DenyReason = quota.SuspensionReason ?? "Your account is temporarily suspended",
                            RetryAfterSeconds = remainingSeconds,
                            QuotaStatus = await MapQuotaStatus(quota)
                        };
                    }
                    else
                    {
                        // Lift permanent suspension if suspension date has passed
                        quota.IsSuspended = false;
                        quota.SuspensionReason = null;
                        quota.SuspensionUntil = null;
                        await _dbContext.SaveChangesAsync(cancellationToken);
                    }
                }

                // Reset quotas if needed
                await ResetQuotasIfNeededAsync(quota, cancellationToken);

                // Check daily limit
                if (quota.DailyTokensUsed + estimatedTokens > quota.DailyTokenLimit)
                {
                    _logger.LogWarning(
                        "Daily quota exceeded for user {UserId}. Used: {Used}, Limit: {Limit}, Requested: {Requested}",
                        userId, quota.DailyTokensUsed, quota.DailyTokenLimit, estimatedTokens);

                    return new RateLimitCheckResult
                    {
                        IsAllowed = false,
                        DenyReason = "Daily token limit exceeded",
                        QuotaStatus = await MapQuotaStatus(quota)
                    };
                }

                // Check monthly limit
                if (quota.MonthlyTokensUsed + estimatedTokens > quota.MonthlyTokenLimit)
                {
                    _logger.LogWarning(
                        "Monthly quota exceeded for user {UserId}. Used: {Used}, Limit: {Limit}, Requested: {Requested}",
                        userId, quota.MonthlyTokensUsed, quota.MonthlyTokenLimit, estimatedTokens);

                    return new RateLimitCheckResult
                    {
                        IsAllowed = false,
                        DenyReason = "Monthly token limit exceeded",
                        QuotaStatus = await MapQuotaStatus(quota)
                    };
                }

                // Check per-minute request limit
                if (quota.CurrentMinuteRequests >= quota.RequestsPerMinuteLimit)
                {
                    var minuteResetKey = $"{userId}_minute";
                    var secondsUntilReset = GetSecondsUntilMinuteReset(minuteResetKey);

                    _logger.LogWarning(
                        "Per-minute request limit exceeded for user {UserId}. Requests: {Requests}, Limit: {Limit}",
                        userId, quota.CurrentMinuteRequests, quota.RequestsPerMinuteLimit);

                    return new RateLimitCheckResult
                    {
                        IsAllowed = false,
                        DenyReason = "Request rate limit exceeded. Please wait before making another request.",
                        RetryAfterSeconds = secondsUntilReset,
                        QuotaStatus = await MapQuotaStatus(quota)
                    };
                }

                // Update minute request counter
                quota.CurrentMinuteRequests++;
                quota.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                return new RateLimitCheckResult
                {
                    IsAllowed = true,
                    QuotaStatus = await MapQuotaStatus(quota)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking rate limit for user {UserId}", userId);
                // Fail open - allow request if rate limit check fails
                return new RateLimitCheckResult { IsAllowed = true };
            }
        }

        /// <summary>
        /// Record token usage after successful AI request
        /// </summary>
        public async Task RecordUsageAsync(
            string userId,
            int tokensUsed,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var quota = await _dbContext.UsageQuotas
                    .FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

                if (quota == null)
                {
                    quota = await CreateDefaultQuotaAsync(userId, cancellationToken);
                }

                await ResetQuotasIfNeededAsync(quota, cancellationToken);

                quota.DailyTokensUsed += tokensUsed;
                quota.MonthlyTokensUsed += tokensUsed;
                quota.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Recorded {TokensUsed} tokens for user {UserId}. Daily: {Daily}/{DailyLimit}, Monthly: {Monthly}/{MonthlyLimit}",
                    tokensUsed, userId, quota.DailyTokensUsed, quota.DailyTokenLimit,
                    quota.MonthlyTokensUsed, quota.MonthlyTokenLimit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording usage for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Get current quota status for a user
        /// </summary>
        public async Task<QuotaStatus?> GetQuotaStatusAsync(string userId, CancellationToken cancellationToken = default)
        {
            var quota = await _dbContext.UsageQuotas
                .FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

            if (quota == null)
            {
                return null;
            }

            await ResetQuotasIfNeededAsync(quota, cancellationToken);
            return await MapQuotaStatus(quota);
        }

        /// <summary>
        /// Reset daily quota for a user
        /// </summary>
        public async Task ResetDailyQuotaAsync(string userId, CancellationToken cancellationToken = default)
        {
            var quota = await _dbContext.UsageQuotas
                .FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

            if (quota != null)
            {
                quota.DailyTokensUsed = 0;
                quota.CurrentMinuteRequests = 0;
                quota.DailyResetAt = DateTime.UtcNow;
                quota.MinuteResetAt = DateTime.UtcNow;
                quota.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Reset daily quota for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Reset monthly quota for a user
        /// </summary>
        public async Task ResetMonthlyQuotaAsync(string userId, CancellationToken cancellationToken = default)
        {
            var quota = await _dbContext.UsageQuotas
                .FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

            if (quota != null)
            {
                quota.MonthlyTokensUsed = 0;
                quota.MonthlyResetAt = DateTime.UtcNow;
                quota.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Reset monthly quota for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Suspend a user due to abuse
        /// </summary>
        public async Task SuspendUserAsync(
            string userId,
            string reason,
            TimeSpan? duration = null,
            CancellationToken cancellationToken = default)
        {
            var quota = await _dbContext.UsageQuotas
                .FirstOrDefaultAsync(q => q.UserId == userId, cancellationToken);

            if (quota != null)
            {
                quota.IsSuspended = true;
                quota.SuspensionReason = reason;
                quota.SuspensionUntil = duration.HasValue ? DateTime.UtcNow.Add(duration.Value) : null;
                quota.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Suspended user {UserId} for reason: {Reason}. Duration: {Duration}",
                    userId, reason, duration?.TotalMinutes ?? 0);
            }
        }

        private async Task<UsageQuota> CreateDefaultQuotaAsync(
            string userId,
            CancellationToken cancellationToken = default)
        {
            var quota = new UsageQuota
            {
                UserId = userId,
                DailyTokenLimit = _options.DefaultDailyTokenLimit,
                MonthlyTokenLimit = _options.DefaultMonthlyTokenLimit,
                RequestsPerMinuteLimit = _options.DefaultRequestsPerMinute
            };

            _dbContext.UsageQuotas.Add(quota);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return quota;
        }

        private async Task ResetQuotasIfNeededAsync(UsageQuota quota, CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            // Reset daily quota
            if (now.Date > quota.DailyResetAt.Date)
            {
                quota.DailyTokensUsed = 0;
                quota.CurrentMinuteRequests = 0;
                quota.DailyResetAt = now;
                quota.MinuteResetAt = now;
            }

            // Reset minute counter
            if (now.Minute != quota.MinuteResetAt.Minute)
            {
                quota.CurrentMinuteRequests = 0;
                quota.MinuteResetAt = now;
            }

            // Reset monthly quota
            if (now.Month != quota.MonthlyResetAt.Month || now.Year != quota.MonthlyResetAt.Year)
            {
                quota.MonthlyTokensUsed = 0;
                quota.MonthlyResetAt = now;
            }

            if (quota.UpdatedAt != now)
            {
                quota.UpdatedAt = now;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private int GetSecondsUntilMinuteReset(string key)
        {
            if (!_minuteResetCache.TryGetValue(key, out var resetTime))
            {
                resetTime = DateTime.UtcNow.AddMinutes(1);
                _minuteResetCache[key] = resetTime;
            }

            var secondsRemaining = (int)(resetTime - DateTime.UtcNow).TotalSeconds;
            return Math.Max(1, secondsRemaining);
        }

        private async Task<QuotaStatus> MapQuotaStatus(UsageQuota quota)
        {
            return new QuotaStatus
            {
                DailyTokensRemaining = Math.Max(0, quota.DailyTokenLimit - quota.DailyTokensUsed),
                MonthlyTokensRemaining = Math.Max(0, quota.MonthlyTokenLimit - quota.MonthlyTokensUsed),
                RequestsRemainingThisMinute = Math.Max(0, quota.RequestsPerMinuteLimit - quota.CurrentMinuteRequests),
                IsSuspended = quota.IsSuspended,
                SuspensionReason = quota.SuspensionReason,
                SuspensionUntil = quota.SuspensionUntil,
                DailyResetAt = quota.DailyResetAt.AddDays(1),
                MonthlyResetAt = quota.MonthlyResetAt.AddMonths(1)
            };
        }
    }
}
