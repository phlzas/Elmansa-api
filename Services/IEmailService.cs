namespace Elmansa_api.Services
{
    /// <summary>
    /// Service for sending emails (confirmation, password reset, etc.)
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends an email confirmation link to the user
        /// </summary>
        Task SendConfirmationEmailAsync(string email, string confirmationLink);

        /// <summary>
        /// Sends a password reset link to the user
        /// </summary>
        Task SendPasswordResetEmailAsync(string email, string resetLink);

        /// <summary>
        /// Sends a generic email
        /// </summary>
        Task SendEmailAsync(string email, string subject, string message);
    }
}
