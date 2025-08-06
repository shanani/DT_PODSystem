// Updated DT_PODSystem/Areas/Security/Services/Interfaces/IApiEmailService.cs

using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.ViewModels;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface IApiEmailService
    {
        #region Authentication
        Task<string> GetTokenAsync();
        #endregion

        #region Direct Email Sending APIs (Existing)
        Task<BulkEmailResultModel> SendBulkEmailAsync(BulkEmailModel bulkEmailModel);
        Task<bool> SendEmailAsync(EmailModel emailModel);
        Task<bool> SendEmailWithAttachmentsAsync(EmailModel emailModel);
        Task<bool> SendEmailWithCopyAsync(string to, string subject, string body, List<string> cc = null, List<string> bcc = null, MailPriority priority = MailPriority.Normal);
        Task<bool> SendEmailWithFileAttachmentsAsync(string to, string subject, string body, List<string> filePaths);
        Task<BulkEmailResultModel> SendNotificationEmailAsync(List<string> recipients, string subject, string body, bool sendIndividually = true);
        Task<bool> SendSimpleEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendTemplateEmailAsync(EmailTemplateModel templateModel, string recipientEmail);
        Task<bool> SendWelcomeEmailAsync(string recipientEmail, string userName, Dictionary<string, string> additionalInfo = null);
        Task<EmailConnectionTestResult> TestEmailConnectionAsync();
        #endregion

        #region Queue Email APIs (New)

        /// <summary>
        /// Queue a regular email for processing
        /// </summary>
        /// <param name="request">Email queue request details</param>
        /// <returns>Queue response with ID and status</returns>
        Task<QueueEmailResponse> QueueEmailAsync(QueueEmailRequest request);

        /// <summary>
        /// Queue a template-based email for processing
        /// </summary>
        /// <param name="request">Template email queue request</param>
        /// <returns>Queue response with ID and status</returns>
        Task<QueueEmailResponse> QueueTemplateEmailAsync(QueueTemplateEmailRequest request);

        /// <summary>
        /// Queue multiple emails in bulk
        /// </summary>
        /// <param name="request">Bulk email queue request</param>
        /// <returns>Bulk queue response with multiple IDs</returns>
        Task<BulkQueueEmailResponse> QueueBulkEmailAsync(QueueBulkEmailRequest request);

        /// <summary>
        /// Get status of a specific queued email
        /// </summary>
        /// <param name="queueId">Queue ID to check</param>
        /// <returns>Email status details</returns>
        Task<EmailStatusResponse> GetEmailStatusAsync(Guid queueId);

        /// <summary>
        /// Get status of multiple queued emails
        /// </summary>
        /// <param name="queueIds">List of queue IDs to check</param>
        /// <returns>List of email status details</returns>
        Task<List<EmailStatusResponse>> GetBatchEmailStatusAsync(List<Guid> queueIds);

        /// <summary>
        /// Cancel a queued email
        /// </summary>
        /// <param name="queueId">Queue ID to cancel</param>
        /// <returns>True if successfully cancelled</returns>
        Task<bool> CancelEmailAsync(Guid queueId);

        /// <summary>
        /// Get email queue health status
        /// </summary>
        /// <returns>Queue health information</returns>
        Task<QueueHealthResponse> GetQueueHealthAsync();

        /// <summary>
        /// Get email queue statistics
        /// </summary>
        /// <param name="fromDate">Start date for statistics (optional)</param>
        /// <param name="toDate">End date for statistics (optional)</param>
        /// <returns>Queue statistics data</returns>
        Task<QueueStatisticsResponse> GetQueueStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);

        /// <summary>
        /// Get paginated list of queued emails
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Items per page (default: 50, max: 100)</param>
        /// <param name="status">Filter by status (optional)</param>
        /// <param name="priority">Filter by priority (optional)</param>
        /// <param name="fromDate">Filter from date (optional)</param>
        /// <param name="toDate">Filter to date (optional)</param>
        /// <param name="search">Search term (optional)</param>
        /// <returns>Paginated list of queued emails</returns>
        Task<PagedEmailQueueResponse> GetQueuedEmailsAsync(
            int page = 1,
            int pageSize = 50,
            string? status = null,
            string? priority = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? search = null);

        #endregion
    }
}