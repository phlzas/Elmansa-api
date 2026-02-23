using Microsoft.AspNetCore.Identity;

namespace Elmansa_api.Models
{
    /// <summary>
    /// Extended IdentityUser for the Elmansa educational platform.
    /// Includes additional properties for tracking user education level and preferences.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// User's full name
        /// </summary>
        public string? FullName { get; set; }

        /// <summary>
        /// Educational level (e.g., "High School", "University")
        /// </summary>
        public string? EducationLevel { get; set; }

        /// <summary>
        /// Date when user joined the platform
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last update timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation property: AI request logs for this user
        /// </summary>
        public ICollection<AIRequestLog> AIRequestLogs { get; set; } = new List<AIRequestLog>();

        /// <summary>
        /// Navigation property: Usage quota for this user
        /// </summary>
        public UsageQuota? UsageQuota { get; set; }
    }
}
