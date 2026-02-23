namespace Elmansa_api.Configuration
{
    /// <summary>
    /// Email settings for SMTP configuration
    /// </summary>
    public class EmailSettings
    {
        public const string SectionName = "EmailSettings";

        /// <summary>
        /// SMTP server address (e.g., smtp.gmail.com)
        /// </summary>
        public string SmtpServer { get; set; } = string.Empty;

        /// <summary>
        /// SMTP port (typically 587 for TLS, 465 for SSL)
        /// </summary>
        public int SmtpPort { get; set; } = 587;

        /// <summary>
        /// Email address to send from
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// Display name for sender
        /// </summary>
        public string SenderName { get; set; } = "Elmansa";

        /// <summary>
        /// SMTP username (usually same as email)
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// SMTP password (use app-specific password for Gmail)
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// Whether to use TLS (true for port 587, false for 465)
        /// </summary>
        public bool UseTls { get; set; } = true;
    }
}
