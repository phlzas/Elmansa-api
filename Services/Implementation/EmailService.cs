using System.Net;
using System.Net.Mail;
using Elmansa_api.Configuration;
using Microsoft.Extensions.Options;

namespace Elmansa_api.Services.Implementation
{
    /// <summary>
    /// Email service implementation using SMTP.
    /// Supports Gmail, SendGrid, and other SMTP providers.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(
            ILogger<EmailService> logger,
            IOptions<EmailSettings> emailSettings)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        /// <summary>
        /// Sends a confirmation email with the confirmation link.
        /// Uses SMTP configuration from appsettings.json
        /// </summary>
        public async Task SendConfirmationEmailAsync(string email, string confirmationLink)
        {
            try
            {
                // Build email body with HTML formatting
                var emailBody = BuildConfirmationEmailBody(confirmationLink);

                await SendEmailAsync(
                    toEmail: email,
                    subject: "Confirm Your Elmansa Account",
                    body: emailBody,
                    isHtml: true
                );

                _logger.LogInformation("? Confirmation email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error sending confirmation email to {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Sends a password reset email with the reset link.
        /// </summary>
        public async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var emailBody = BuildPasswordResetEmailBody(resetLink);

                await SendEmailAsync(
                    toEmail: email,
                    subject: "Reset Your Elmansa Password",
                    body: emailBody,
                    isHtml: true
                );

                _logger.LogInformation("? Password reset email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error sending password reset email to {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Sends a generic email.
        /// </summary>
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                await SendEmailAsync(
                    toEmail: email,
                    subject: subject,
                    body: message,
                    isHtml: false
                );

                _logger.LogInformation("? Email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "? Error sending email to {Email}", email);
                throw;
            }
        }

        /// <summary>
        /// Internal method to send email via SMTP
        /// </summary>
        private async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml)
        {
            // Validate settings
            if (string.IsNullOrEmpty(_emailSettings.SmtpServer) ||
                string.IsNullOrEmpty(_emailSettings.SenderEmail) ||
                string.IsNullOrEmpty(_emailSettings.Username) ||
                string.IsNullOrEmpty(_emailSettings.Password))
            {
                _logger.LogWarning(
                    "??  Email settings not configured. Please set EmailSettings in appsettings.json\n" +
                    "Current settings: SmtpServer={SmtpServer}, SenderEmail={SenderEmail}",
                    _emailSettings.SmtpServer, _emailSettings.SenderEmail);

                throw new InvalidOperationException(
                    "Email settings are not properly configured in appsettings.json");
            }

            using (var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort))
            {
                // Configure SMTP client
                client.EnableSsl = _emailSettings.UseTls;
                client.Credentials = new NetworkCredential(
                    _emailSettings.Username,
                    _emailSettings.Password
                );
                client.Timeout = 10000; // 10 seconds timeout

                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(
                        _emailSettings.SenderEmail,
                        _emailSettings.SenderName
                    );
                    mailMessage.To.Add(toEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = isHtml;

                    try
                    {
                        await client.SendMailAsync(mailMessage);
                    }
                    catch (SmtpException ex) when (ex.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst)
                    {
                        _logger.LogWarning(
                            "??  SMTP Server requires STARTTLS. Retrying with TLS...");
                        client.EnableSsl = true;
                        await client.SendMailAsync(mailMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Builds HTML email body for confirmation email
        /// </summary>
        private string BuildConfirmationEmailBody(string confirmationLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding-top: 20px; font-size: 12px; color: #666; }}
        .highlight {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Welcome to Elmansa!</h1>
        </div>
        <div class=""content"">
            <p>Hello,</p>
            
            <p>Thank you for registering with Elmansa. To complete your registration and activate your account, please confirm your email address.</p>
            
            <p style=""text-align: center;"">
                <a href=""{confirmationLink}"" class=""button"">Confirm Email Address</a>
            </p>
            
            <p>Or copy and paste this link in your browser:</p>
            <p style=""word-break: break-all; color: #0066cc; font-size: 12px;"">{confirmationLink}</p>
            
            <div class=""highlight"">
                <strong>? Note:</strong> This confirmation link will expire in 24 hours.
            </div>
            
            <p style=""margin-top: 20px;"">
                If you did not create this account, please ignore this email.
            </p>
            
            <p>Best regards,<br/>The Elmansa Team</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 Elmansa. All rights reserved.</p>
            <p>For support, visit <a href=""https://elmanssa.com"">elmanssa.com</a></p>
        </div>
    </div>
</body>
</html>";
        }

        /// <summary>
        /// Builds HTML email body for password reset email
        /// </summary>
        private string BuildPasswordResetEmailBody(string resetLink)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #FF6B6B; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #FF6B6B; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }}
        .footer {{ text-align: center; padding-top: 20px; font-size: 12px; color: #666; }}
        .warning {{ background-color: #fff3cd; padding: 15px; border-left: 4px solid #ffc107; margin-top: 15px; }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Reset Your Password</h1>
        </div>
        <div class=""content"">
            <p>Hello,</p>
            
            <p>We received a request to reset your Elmansa account password. Click the button below to create a new password.</p>
            
            <p style=""text-align: center;"">
                <a href=""{resetLink}"" class=""button"">Reset Password</a>
            </p>
            
            <p>Or copy and paste this link in your browser:</p>
            <p style=""word-break: break-all; color: #0066cc; font-size: 12px;"">{resetLink}</p>
            
            <div class=""warning"">
                <strong>?? Security Warning:</strong> This link will expire in 24 hours. If you did not request a password reset, please ignore this email. Your account will remain secure.
            </div>
            
            <p style=""margin-top: 20px; color: #666; font-size: 12px;"">
                If you're having trouble clicking the button, you can copy and paste the link above into your web browser.
            </p>
            
            <p>Best regards,<br/>The Elmansa Team</p>
        </div>
        <div class=""footer"">
            <p>&copy; 2025 Elmansa. All rights reserved.</p>
            <p>For support, visit <a href=""https://elmanssa.com"">elmanssa.com</a></p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
