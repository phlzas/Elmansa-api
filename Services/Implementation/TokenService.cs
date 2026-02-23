using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Elmansa_api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Elmansa_api.Services.Implementation
{
    /// <summary>
    /// Implementation of JWT and Refresh token generation
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IConfiguration configuration,
            UserManager<ApplicationUser> userManager,
            ILogger<TokenService> logger)
        {
            _configuration = configuration;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT token with user claims including roles and full name
        /// </summary>
        public async Task<string> GenerateJwtTokenAsync(ApplicationUser user)
        {
            try
            {
                var jwtKey = _configuration["Jwt:Key"] 
                    ?? throw new InvalidOperationException("Jwt:Key not configured");
                var jwtIssuer = _configuration["Jwt:Issuer"] 
                    ?? "Elmansa";
                var jwtAudience = _configuration["Jwt:Audience"] 
                    ?? "ElmansaUsers";
                var jwtExpirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                // Build claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Name, user.FullName ?? user.UserName ?? ""),
                    new Claim("FullName", user.FullName ?? "")
                };

                // Add role claims
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(jwtExpirationMinutes),
                    signingCredentials: creds
                );

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating JWT token for user {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Generates a secure refresh token (random 64-byte string, base64 encoded)
        /// </summary>
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
    }
}
