using Elmansa_api.Models;

namespace Elmansa_api.Services
{
    /// <summary>
    /// Service for generating JWT and Refresh tokens
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT token for the given user
        /// </summary>
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);

        /// <summary>
        /// Generates a refresh token
        /// </summary>
        string GenerateRefreshToken();
    }
}
