using Elmansa_api.Models;
using Microsoft.AspNetCore.Identity;

namespace Elmansa_api.Configuration
{
    /// <summary>
    /// Extension methods for configuring Identity with proper token providers,
    /// password policies, and email confirmation settings
    /// </summary>
    public static class IdentityConfiguration
    {
        /// <summary>
        /// Configures Identity with appropriate defaults for email confirmation,
        /// password reset, and two-factor authentication
        /// </summary>
        public static void ConfigureIdentityOptions(IdentityOptions options)
        {
            // Password Policy
            options.Password.RequiredLength = 6;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredUniqueChars = 0;

            // User Policy
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Lockout Policy (security)
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // Email confirmation
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.SignIn.RequireConfirmedAccount = true;
        }
    }
}
