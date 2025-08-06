using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace DT_PODSystem.Areas.Security.Models.ViewModels
{
    public class EmailModel
    {
        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Recipient email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string RecipientEmail { get; set; }

        public string? RecipientName { get; set; }

        [Required(ErrorMessage = "Email body is required")]
        public string Body { get; set; }

        public List<string>? CC { get; set; } = new();

        public List<string>? BCC { get; set; } = new();

        [EmailAddress(ErrorMessage = "Invalid reply-to email address")]
        public string? ReplyTo { get; set; }

        public MailPriority Priority { get; set; } = MailPriority.Normal;

        public bool IsBodyHtml { get; set; } = true;

        public List<EmailAttachment>? Attachments { get; set; } = new();

        public Dictionary<string, string>? CustomHeaders { get; set; } = new();

        public bool RequestDeliveryNotification { get; set; } = false;

        public bool RequestReadReceipt { get; set; } = false;
    }

    /// <summary>
    /// Email attachment model
    /// </summary>
    public class EmailAttachment
    {
        /// <summary>
        /// File name with extension
        /// </summary>
        [Required]
        public string FileName { get; set; }

        /// <summary>
        /// Base64 encoded file content
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// MIME content type (e.g., application/pdf, image/png)
        /// </summary>
        public string? ContentType { get; set; }

        /// <summary>
        /// Content ID for inline attachments
        /// </summary>
        public string? ContentId { get; set; }

        /// <summary>
        /// Is this an inline attachment
        /// </summary>
        public bool IsInline { get; set; } = false;
    }

    /// <summary>
    /// Bulk email model for sending to multiple recipients
    /// </summary>
    public class BulkEmailModel
    {
        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Email body content
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// List of recipient emails
        /// </summary>
        [Required]
        public List<string> Recipients { get; set; }

        /// <summary>
        /// CC recipients for all emails
        /// </summary>
        public List<string>? CC { get; set; }

        /// <summary>
        /// BCC recipients for all emails
        /// </summary>
        public List<string>? BCC { get; set; }

        /// <summary>
        /// Email priority
        /// </summary>
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        /// <summary>
        /// Is email body HTML formatted
        /// </summary>
        public bool IsBodyHtml { get; set; } = true;

        /// <summary>
        /// File attachments
        /// </summary>
        public List<EmailAttachment>? Attachments { get; set; }

        /// <summary>
        /// Send emails individually (true) or as one email with multiple recipients (false)
        /// </summary>
        public bool SendIndividually { get; set; } = true;

        public BulkEmailModel()
        {
            Recipients = new List<string>();
            CC = new List<string>();
            BCC = new List<string>();
            Attachments = new List<EmailAttachment>();
        }
    }

    /// <summary>
    /// Result model for bulk email operations
    /// </summary>
    public class BulkEmailResultModel
    {
        /// <summary>
        /// Total number of emails attempted
        /// </summary>
        public int TotalEmails { get; set; }

        /// <summary>
        /// Number of successfully sent emails
        /// </summary>
        public int SuccessfulSends { get; set; }

        /// <summary>
        /// Number of failed email sends
        /// </summary>
        public int FailedSends { get; set; }

        /// <summary>
        /// Detailed results for each email
        /// </summary>
        public List<EmailSendResult> Results { get; set; }

        /// <summary>
        /// Total time taken to process all emails
        /// </summary>
        public TimeSpan TotalProcessingTime { get; set; }

        public BulkEmailResultModel()
        {
            Results = new List<EmailSendResult>();
        }
    }

    /// <summary>
    /// Individual email send result
    /// </summary>
    public class EmailSendResult
    {
        /// <summary>
        /// Recipient email address
        /// </summary>
        public string RecipientEmail { get; set; }

        /// <summary>
        /// Whether the email was sent successfully
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if the send failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Timestamp when the email was sent
        /// </summary>
        public DateTime SentAt { get; set; }
    }

    /// <summary>
    /// Email template model with placeholders
    /// </summary>
    public class EmailTemplateModel
    {
        /// <summary>
        /// Template name for identification
        /// </summary>
        [Required]
        public string TemplateName { get; set; }

        /// <summary>
        /// Email subject with placeholders
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Email body with placeholders
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// Key-value pairs for placeholder replacement
        /// </summary>
        public Dictionary<string, string>? Placeholders { get; set; }

        public EmailTemplateModel()
        {
            Placeholders = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Email connection test result
    /// </summary>
    public class EmailConnectionTestResult
    {
        /// <summary>
        /// Whether the connection test was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Test result message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// SMTP server address
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// SMTP server port
        /// </summary>
        public string Port { get; set; }

        /// <summary>
        /// Sender email address
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// Timestamp when test was performed
        /// </summary>
        public DateTime TestSentAt { get; set; }

        /// <summary>
        /// Error message if test failed
        /// </summary>
        public string? Error { get; set; }
    }

    /// <summary>
    /// Simple email request model
    /// </summary>
    public class SimpleEmailRequest
    {
        /// <summary>
        /// Recipient email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string To { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Email body
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// Is body HTML formatted
        /// </summary>
        public bool IsHtml { get; set; } = true;
    }

    /// <summary>
    /// Email with copy recipients model
    /// </summary>
    public class EmailWithCopyRequest
    {
        /// <summary>
        /// Primary recipient
        /// </summary>
        [Required]
        [EmailAddress]
        public string To { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Email body
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// CC recipients
        /// </summary>
        public List<string>? CC { get; set; }

        /// <summary>
        /// BCC recipients
        /// </summary>
        public List<string>? BCC { get; set; }

        /// <summary>
        /// Email priority
        /// </summary>
        public MailPriority Priority { get; set; } = MailPriority.Normal;

        public EmailWithCopyRequest()
        {
            CC = new List<string>();
            BCC = new List<string>();
        }
    }

    /// <summary>
    /// File attachment request model
    /// </summary>
    public class FileAttachmentRequest
    {
        /// <summary>
        /// Recipient email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string To { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Email body
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// File paths to attach
        /// </summary>
        [Required]
        public List<string> FilePaths { get; set; }

        public FileAttachmentRequest()
        {
            FilePaths = new List<string>();
        }
    }

    /// <summary>
    /// Notification email request model
    /// </summary>
    public class NotificationEmailRequest
    {
        /// <summary>
        /// List of recipient emails
        /// </summary>
        [Required]
        public List<string> Recipients { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        [Required]
        public string Subject { get; set; }

        /// <summary>
        /// Email body
        /// </summary>
        [Required]
        public string Body { get; set; }

        /// <summary>
        /// Send individually or as group
        /// </summary>
        public bool SendIndividually { get; set; } = true;

        public NotificationEmailRequest()
        {
            Recipients = new List<string>();
        }
    }

    /// <summary>
    /// Welcome email request model
    /// </summary>
    public class WelcomeEmailRequest
    {
        /// <summary>
        /// New user's email address
        /// </summary>
        [Required]
        [EmailAddress]
        public string RecipientEmail { get; set; }

        /// <summary>
        /// User's name for personalization
        /// </summary>
        [Required]
        public string UserName { get; set; }

        /// <summary>
        /// Additional information for template
        /// </summary>
        public Dictionary<string, string>? AdditionalInfo { get; set; }

        public WelcomeEmailRequest()
        {
            AdditionalInfo = new Dictionary<string, string>();
        }
    }



    /// <summary>
    /// Email validation result
    /// </summary>
    public class EmailValidationResult
    {
        /// <summary>
        /// Whether the email format is valid
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Validation error message if invalid
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Suggested correction if applicable
        /// </summary>
        public string? SuggestedCorrection { get; set; }
    }
}



