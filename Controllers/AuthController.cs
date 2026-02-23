using Elmansa_api.Models;
using Elmansa_api.RequestResponse;
using Elmansa_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Elmansa_api.Controllers
{
    /// <summary>
    /// Authentication controller for user registration, login, email confirmation, and password reset.
    /// This controller provides custom responses that extend the default Identity API endpoints.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Register a new user with email confirmation flow
        /// </summary>
        /// <remarks>
        /// Creates a new user account with the provided email and password.
        /// A confirmation email is simulated and logged.
        /// User must confirm email before being able to login.
        /// </remarks>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponseDto<object>>> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Registration failed",
                    Errors = errors
                });
            }

            try
            {
                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User with this email already exists",
                        Errors = new List<string> { "Email is already registered" }
                    });
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    EducationLevel = model.EducationLevel,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    EmailConfirmed = false // User must confirm email
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User creation failed",
                        Errors = errors
                    });
                }

                // Generate email confirmation token
                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Build confirmation link - in production, send to frontend URL
                var confirmationLink = $"{_configuration["AppUrl"]}/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

                // Send confirmation email (simulated in development)
                await _emailService.SendConfirmationEmailAsync(user.Email!, confirmationLink);

                _logger.LogInformation("User {Email} registered successfully. Confirmation email sent.", user.Email);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Registration successful! Please check your email to confirm your account.",
                    Data = new { userId = user.Id, email = user.Email }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user registration for {Email}", model.Email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred during registration",
                        Errors = new List<string> { ex.Message }
                    });
            }
        }

        /// <summary>
        /// Login with email and password.
        /// Returns JWT token and refresh token.
        /// Email must be confirmed before login is allowed.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(new ApiResponseDto<AuthResponseDto>
                {
                    Success = false,
                    Message = "Login failed",
                    Errors = errors
                });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login attempt for non-existent user: {Email}", model.Email);
                    return Unauthorized(new ApiResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Check if email is confirmed
                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login attempt for unconfirmed email: {Email}", model.Email);
                    return Unauthorized(new ApiResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Please confirm your email address before logging in"
                    });
                }

                // Verify password
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Failed login attempt for user: {Email}", model.Email);
                    return Unauthorized(new ApiResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    });
                }

                // Generate tokens
                var jwtToken = await _tokenService.GenerateJwtTokenAsync(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Get user roles
                var roles = await _userManager.GetRolesAsync(user);

                var response = new AuthResponseDto
                {
                    Token = jwtToken,
                    RefreshToken = refreshToken,
                    UserId = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList(),
                    ExpiresIn = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60") * 60
                };

                _logger.LogInformation("User {Email} logged in successfully", user.Email);

                return Ok(new ApiResponseDto<AuthResponseDto>
                {
                    Success = true,
                    Message = "Login successful",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", model.Email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "An error occurred during login"
                    });
            }
        }

        /// <summary>
        /// Confirm user email address using token from confirmation email
        /// </summary>
        [HttpPost("confirm-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponseDto<object>>> ConfirmEmail([FromBody] ConfirmEmailDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid request"
                });
            }

            try
            {
                var user = await _userManager.FindByIdAsync(model.UserId);
                if (user == null)
                {
                    return NotFound(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var result = await _userManager.ConfirmEmailAsync(user, model.Token);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Email confirmation failed",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                _logger.LogInformation("Email confirmed for user {Email}", user.Email);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Email confirmed successfully. You can now login."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }

        /// <summary>
        /// Resend confirmation email to user
        /// </summary>
        [HttpPost("resend-confirmation-email")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponseDto<object>>> ResendConfirmationEmail([FromBody] ResendConfirmationEmailDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid email"
                });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal if user exists
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = true,
                        Message = "If the email exists, a confirmation link has been sent."
                    });
                }

                if (user.EmailConfirmed)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Email is already confirmed"
                    });
                }

                var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = $"{_configuration["AppUrl"]}/auth/confirm-email?userId={user.Id}&token={Uri.EscapeDataString(confirmationToken)}";

                await _emailService.SendConfirmationEmailAsync(user.Email!, confirmationLink);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Confirmation email resent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending confirmation email");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }

        /// <summary>
        /// Initiate password reset flow - sends reset token via email
        /// </summary>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponseDto<object>>> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid email"
                });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    // Don't reveal if user exists
                    return Ok(new ApiResponseDto<object>
                    {
                        Success = true,
                        Message = "If the email exists, a password reset link has been sent."
                    });
                }

                var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = $"{_configuration["AppUrl"]}/auth/reset-password?userId={user.Id}&token={Uri.EscapeDataString(resetToken)}";

                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink);

                _logger.LogInformation("Password reset email sent for user {Email}", user.Email);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Password reset email sent successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }

        /// <summary>
        /// Reset password using token from email
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ApiResponseDto<object>>> ResetPassword([FromBody] ResetPasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
                return BadRequest(new ApiResponseDto<object>
                {
                    Success = false,
                    Message = "Invalid request",
                    Errors = errors
                });
            }

            try
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Password reset failed",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                _logger.LogInformation("Password reset for user {Email}", user.Email);

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Password reset successfully. You can now login with your new password."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }

        /// <summary>
        /// Get current user's profile information (requires authentication)
        /// </summary>
        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ApiResponseDto<AuthResponseDto>>> GetCurrentUser()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound(new ApiResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var response = new AuthResponseDto
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FullName = user.FullName ?? "",
                    EmailConfirmed = user.EmailConfirmed,
                    Roles = roles.ToList()
                };

                return Ok(new ApiResponseDto<AuthResponseDto>
                {
                    Success = true,
                    Message = "User information retrieved successfully",
                    Data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current user");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<AuthResponseDto>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }

        /// <summary>
        /// Get user management information (requires authentication)
        /// </summary>
        [Authorize]
        [HttpGet("manage/info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetManageInfo()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    email = user.Email,
                    isEmailConfirmed = user.EmailConfirmed,
                    phoneNumber = user.PhoneNumber,
                    isTwoFactorEnabled = user.TwoFactorEnabled,
                    hasPassword = !string.IsNullOrEmpty(user.PasswordHash)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting manage info");
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Update user management information (requires authentication)
        /// </summary>
        [Authorize]
        [HttpPost("manage/info")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<object>>> UpdateManageInfo([FromBody] dynamic model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // Extract phone number if provided
                if (model?.phoneNumber != null)
                {
                    var phoneNumber = (string)model.phoneNumber;
                    var result = await _userManager.SetPhoneNumberAsync(user, phoneNumber);
                    if (!result.Succeeded)
                    {
                        return BadRequest(new ApiResponseDto<object>
                        {
                            Success = false,
                            Message = "Failed to update phone number",
                            Errors = result.Errors.Select(e => e.Description).ToList()
                        });
                    }
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = "Profile updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating manage info");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }

        /// <summary>
        /// Enable/Disable two-factor authentication (requires authentication)
        /// </summary>
        [Authorize]
        [HttpPost("manage/2fa")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiResponseDto<object>>> Manage2FA([FromBody] dynamic model)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return NotFound();
                }

                // This is a simplified implementation
                // In production, implement proper 2FA flow with authenticator apps or SMS
                var enable = (bool?)model?.enable ?? false;

                user.TwoFactorEnabled = enable;
                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return BadRequest(new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "Failed to update 2FA settings",
                        Errors = result.Errors.Select(e => e.Description).ToList()
                    });
                }

                return Ok(new ApiResponseDto<object>
                {
                    Success = true,
                    Message = $"Two-factor authentication has been {(enable ? "enabled" : "disabled")}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error managing 2FA");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ApiResponseDto<object>
                    {
                        Success = false,
                        Message = "An error occurred"
                    });
            }
        }
    }
}
