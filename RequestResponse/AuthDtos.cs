using System.ComponentModel.DataAnnotations;

namespace Elmansa_api.RequestResponse
{
    /// <summary>
    /// Request model for user registration
    /// </summary>
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
        public string FullName { get; set; } = null!;

        [StringLength(50, ErrorMessage = "Education level must not exceed 50 characters")]
        public string? EducationLevel { get; set; }
    }

    /// <summary>
    /// Request model for user login
    /// </summary>
    public class LoginDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = null!;
    }

    /// <summary>
    /// Response model after successful authentication
    /// Includes JWT token, refresh token, and user details
    /// </summary>
    public class AuthResponseDto
    {
        /// <summary>
        /// JWT access token for API requests
        /// </summary>
        public string? Token { get; set; }

        /// <summary>
        /// Refresh token for obtaining new access tokens
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>
        /// Unique user identifier
        /// </summary>
        public string UserId { get; set; } = null!;

        /// <summary>
        /// User's email address
        /// </summary>
        public string Email { get; set; } = null!;

        /// <summary>
        /// User's full name
        /// </summary>
        public string FullName { get; set; } = null!;

        /// <summary>
        /// User's assigned roles
        /// </summary>
        public List<string> Roles { get; set; } = new();

        /// <summary>
        /// Email confirmation status
        /// </summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// Token expiration time in seconds
        /// </summary>
        public int ExpiresIn { get; set; } = 3600; // 1 hour
    }

    /// <summary>
    /// Request model for confirming email address
    /// </summary>
    public class ConfirmEmailDto
    {
        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;
    }

    /// <summary>
    /// Request model for password reset
    /// </summary>
    public class ResetPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        public string Token { get; set; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string NewPassword { get; set; } = null!;
    }

    /// <summary>
    /// Request model for forgetting password (initiates reset flow)
    /// </summary>
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    /// <summary>
    /// Request model for resending confirmation email
    /// </summary>
    public class ResendConfirmationEmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }

    /// <summary>
    /// Response model for generic API success/error messages
    /// </summary>
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
