using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;


namespace DT_PODSystem.Areas.Security.Services.Implementations

{
    public class ApiEmailService : IApiEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _baseUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApiEmailService(HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _baseUrl = configuration["ApiSettings:BaseUrl"];
            _httpContextAccessor = httpContextAccessor;
        }


        #region Authentication


        public async Task<string> GetTokenAsync()
        {
            // Read username and password from configuration
            string username = _configuration["ApiSettings:Username"];
            string password = _configuration["ApiSettings:Password"];

            var loginDto = new LoginDto
            {
                Username = username,
                Password = password
            };

            // Make the request to get the token
            var response = await _httpClient.PostAsJsonAsync($"{_configuration["ApiSettings:BaseUrl"]}/auth/get-token", loginDto);

            if (response.IsSuccessStatusCode)
            {
                // 🆕 NEW: Parse the JSON response to extract the token
                var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponseDto>();

                if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                {
                    return tokenResponse.Token; // Return just the token string
                }

                throw new Exception("Invalid token response format from AD API");
            }

            throw new Exception("Failed to retrieve AD token: " + response.ReasonPhrase);
        }

        private async Task<string> EnsureTokenAsync()
        {
            var token = _httpContextAccessor.HttpContext.Session.GetString("AuthToken");

            if (string.IsNullOrEmpty(token))
            {
                token = await GetTokenAsync(); // Fetch a new token if not found in session
                _httpContextAccessor.HttpContext.Session.SetString("AuthToken", token); // Store token in session for future use
            }

            return token;
        }

        #endregion


        #region Direct Email SendingAPIs

        /// <summary>
        /// Sends a single email
        /// </summary>
        /// <param name="emailModel">Email details including subject, recipient, body, etc.</param>
        /// <returns>True if email was sent successfully</returns>
        public async Task<bool> SendEmailAsync(EmailModel emailModel)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/send-email", emailModel);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Sends bulk emails to multiple recipients
        /// </summary>
        /// <param name="bulkEmailModel">Bulk email details with multiple recipients</param>
        /// <returns>Result containing success/failure counts and details</returns>
        public async Task<BulkEmailResultModel> SendBulkEmailAsync(BulkEmailModel bulkEmailModel)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/send-bulk-email", bulkEmailModel);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BulkEmailResultModel>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send bulk email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Sends an email using a template with placeholders
        /// </summary>
        /// <param name="templateModel">Email template with placeholders</param>
        /// <param name="recipientEmail">Recipient email address</param>
        /// <returns>True if template email was sent successfully</returns>
        public async Task<bool> SendTemplateEmailAsync(EmailTemplateModel templateModel, string recipientEmail)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/send-template-email?recipientEmail={Uri.EscapeDataString(recipientEmail)}", templateModel);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send template email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Sends an email with attachments (with size validation)
        /// </summary>
        /// <param name="emailModel">Email details including attachments</param>
        /// <returns>True if email with attachments was sent successfully</returns>
        public async Task<bool> SendEmailWithAttachmentsAsync(EmailModel emailModel)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/send-email-with-attachments", emailModel);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.RequestEntityTooLarge)
            {
                throw new Exception("Attachment size exceeds the maximum allowed limit (25MB)");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to send email with attachments: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Tests the email service connection
        /// </summary>
        /// <returns>Connection test result with server details and status</returns>
        public async Task<EmailConnectionTestResult> TestEmailConnectionAsync()
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{_baseUrl}/api/test-email-connection");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EmailConnectionTestResult>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Email connection test failed: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Sends a simple email with minimal parameters
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML supported)</param>
        /// <param name="isHtml">Whether the body is HTML formatted (default: true)</param>
        /// <returns>True if email was sent successfully</returns>
        public async Task<bool> SendSimpleEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            var emailModel = new EmailModel
            {
                RecipientEmail = to,
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            return await SendEmailAsync(emailModel);
        }

        /// <summary>
        /// Sends an email with CC and BCC recipients
        /// </summary>
        /// <param name="to">Primary recipient</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="cc">CC recipients (optional)</param>
        /// <param name="bcc">BCC recipients (optional)</param>
        /// <param name="priority">Email priority (default: Normal)</param>
        /// <returns>True if email was sent successfully</returns>
        public async Task<bool> SendEmailWithCopyAsync(string to, string subject, string body,
            List<string> cc = null, List<string> bcc = null, MailPriority priority = MailPriority.Normal)
        {
            var emailModel = new EmailModel
            {
                RecipientEmail = to,
                Subject = subject,
                Body = body,
                CC = cc ?? new List<string>(),
                BCC = bcc ?? new List<string>(),
                Priority = priority,
                IsBodyHtml = true
            };

            return await SendEmailAsync(emailModel);
        }

        /// <summary>
        /// Sends an email with file attachments from file paths
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="filePaths">List of file paths to attach</param>
        /// <returns>True if email was sent successfully</returns>
        public async Task<bool> SendEmailWithFileAttachmentsAsync(string to, string subject, string body, List<string> filePaths)
        {
            var attachments = new List<EmailAttachment>();

            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    var fileBytes = await File.ReadAllBytesAsync(filePath);
                    var fileName = Path.GetFileName(filePath);
                    var contentType = GetContentType(filePath);

                    attachments.Add(new EmailAttachment
                    {
                        FileName = fileName,
                        Content = Convert.ToBase64String(fileBytes),
                        ContentType = contentType,
                        IsInline = false
                    });
                }
            }

            var emailModel = new EmailModel
            {
                RecipientEmail = to,
                Subject = subject,
                Body = body,
                Attachments = attachments,
                IsBodyHtml = true
            };

            return await SendEmailWithAttachmentsAsync(emailModel);
        }

        /// <summary>
        /// Sends a notification email to multiple users
        /// </summary>
        /// <param name="recipients">List of recipient emails</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="sendIndividually">Whether to send individual emails or one email with multiple recipients</param>
        /// <returns>Bulk email result with success/failure details</returns>
        public async Task<BulkEmailResultModel> SendNotificationEmailAsync(List<string> recipients, string subject, string body, bool sendIndividually = true)
        {
            var bulkEmailModel = new BulkEmailModel
            {
                Recipients = recipients,
                Subject = subject,
                Body = body,
                SendIndividually = sendIndividually,
                IsBodyHtml = true
            };

            return await SendBulkEmailAsync(bulkEmailModel);
        }

        /// <summary>
        /// Sends a welcome email using a template
        /// </summary>
        /// <param name="recipientEmail">New user's email</param>
        /// <param name="userName">User's name for personalization</param>
        /// <param name="additionalInfo">Additional info to include in template</param>
        /// <returns>True if welcome email was sent successfully</returns>
        public async Task<bool> SendWelcomeEmailAsync(string recipientEmail, string userName, Dictionary<string, string> additionalInfo = null)
        {
            var placeholders = new Dictionary<string, string>
    {
        { "UserName", userName },
        { "Date", DateTime.Now.ToString("MMMM dd, yyyy") },
        { "Year", DateTime.Now.Year.ToString() }
    };

            // Add additional placeholders if provided
            if (additionalInfo != null)
            {
                foreach (var info in additionalInfo)
                {
                    placeholders[info.Key] = info.Value;
                }
            }

            var templateModel = new EmailTemplateModel
            {
                TemplateName = "Welcome Email",
                Subject = "Welcome to our platform, {UserName}!",
                Body = @"
            <html>
            <body>
                <h1>Welcome {UserName}!</h1>
                <p>Thank you for joining our platform on {Date}.</p>
                <p>We're excited to have you on board!</p>
                <hr>
                <p><small>&copy; {Year} Our Company. All rights reserved.</small></p>
            </body>
            </html>",
                Placeholders = placeholders
            };

            return await SendTemplateEmailAsync(templateModel, recipientEmail);
        }

        /// <summary>
        /// Helper method to determine content type based on file extension
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>MIME content type</returns>
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                _ => "application/octet-stream"
            };
        }

        #endregion


        #region Queue Email APIs (New Implementation)

        /// <summary>
        /// Queue a regular email for processing
        /// </summary>
        /// <param name="request">Email queue request details</param>
        /// <returns>Queue response with ID and status</returns>
        public async Task<QueueEmailResponse> QueueEmailAsync(QueueEmailRequest request)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/email/queue", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QueueEmailResponse>()
                       ?? new QueueEmailResponse { QueueId = Guid.Empty, Message = "Failed to parse response" }; // ✅ FIXED
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to queue email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Queue a template-based email for processing
        /// </summary>
        /// <param name="request">Template email queue request</param>
        /// <returns>Queue response with ID and status</returns>
        public async Task<QueueEmailResponse> QueueTemplateEmailAsync(QueueTemplateEmailRequest request)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/email/queue-template", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QueueEmailResponse>()
                       ?? new QueueEmailResponse { QueueId = Guid.Empty, Message = "Failed to parse response" }; // ✅ FIXED
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to queue template email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Queue multiple emails in bulk
        /// </summary>
        /// <param name="request">Bulk email queue request</param>
        /// <returns>Bulk queue response with multiple IDs</returns>
        public async Task<BulkQueueEmailResponse> QueueBulkEmailAsync(QueueBulkEmailRequest request)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/email/queue-bulk", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BulkQueueEmailResponse>()
                       ?? new BulkQueueEmailResponse // ✅ FIXED
                       {
                           Results = new List<QueueEmailResponse>(),
                           TotalQueued = 0,
                           SuccessfulQueues = 0,
                           FailedQueues = 0,
                           SuccessRate = 0,
                           TotalProcessingTime = TimeSpan.Zero
                       };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to queue bulk email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Get status of a specific queued email
        /// </summary>
        /// <param name="queueId">Queue ID to check</param>
        /// <returns>Email status details</returns>
        public async Task<EmailStatusResponse> GetEmailStatusAsync(Guid queueId)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{_baseUrl}/email/status/{queueId}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EmailStatusResponse>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Email not found
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get email status: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Get status of multiple queued emails
        /// </summary>
        /// <param name="queueIds">List of queue IDs to check</param>
        /// <returns>List of email status details</returns>
        public async Task<List<EmailStatusResponse>> GetBatchEmailStatusAsync(List<Guid> queueIds)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/email/status/batch", queueIds);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<EmailStatusResponse>>()
                       ?? new List<EmailStatusResponse>();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get batch email status: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Cancel a queued email
        /// </summary>
        /// <param name="queueId">Queue ID to cancel</param>
        /// <returns>True if successfully cancelled</returns>
        public async Task<bool> CancelEmailAsync(Guid queueId)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.PostAsync($"{_baseUrl}/email/cancel/{queueId}", null);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<bool>();
            }

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false; // Email not found or cannot be cancelled
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to cancel email: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Get email queue health status
        /// </summary>
        /// <returns>Queue health information</returns>
        public async Task<QueueHealthResponse> GetQueueHealthAsync()
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.GetAsync($"{_baseUrl}/email/health");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QueueHealthResponse>()
                       ?? new QueueHealthResponse { IsHealthy = false, Status = "Unknown" };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get queue health: {response.ReasonPhrase} - {errorContent}");
        }

        /// <summary>
        /// Get email queue statistics
        /// </summary>
        /// <param name="fromDate">Start date for statistics (optional)</param>
        /// <param name="toDate">End date for statistics (optional)</param>
        /// <returns>Queue statistics data</returns>
        public async Task<QueueStatisticsResponse> GetQueueStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var queryParams = new List<string>();
            if (fromDate.HasValue)
                queryParams.Add($"fromDate={HttpUtility.UrlEncode(fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
            if (toDate.HasValue)
                queryParams.Add($"toDate={HttpUtility.UrlEncode(toDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
            var response = await _httpClient.GetAsync($"{_baseUrl}/email/statistics{queryString}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<QueueStatisticsResponse>()
                       ?? new QueueStatisticsResponse();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get queue statistics: {response.ReasonPhrase} - {errorContent}");
        }

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
        public async Task<PagedEmailQueueResponse> GetQueuedEmailsAsync(
            int page = 1,
            int pageSize = 50,
            string? status = null,
            string? priority = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? search = null)
        {
            var token = await EnsureTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={HttpUtility.UrlEncode(status)}");
            if (!string.IsNullOrWhiteSpace(priority))
                queryParams.Add($"priority={HttpUtility.UrlEncode(priority)}");
            if (fromDate.HasValue)
                queryParams.Add($"fromDate={HttpUtility.UrlEncode(fromDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
            if (toDate.HasValue)
                queryParams.Add($"toDate={HttpUtility.UrlEncode(toDate.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={HttpUtility.UrlEncode(search)}");

            var queryString = "?" + string.Join("&", queryParams);
            var response = await _httpClient.GetAsync($"{_baseUrl}/email/list{queryString}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<PagedEmailQueueResponse>()
                       ?? new PagedEmailQueueResponse();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get queued emails: {response.ReasonPhrase} - {errorContent}");
        }

        #endregion

        #region Helper Methods for Queue APIs

        /// <summary>
        /// Queue a simple email with minimal parameters
        /// </summary>
        /// <param name="to">Recipient email address</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body (HTML supported)</param>
        /// <param name="isHtml">Whether the body is HTML formatted (default: true)</param>
        /// <param name="priority">Email priority (default: Normal)</param>
        /// <param name="scheduledSendTime">Optional scheduled send time</param>
        /// <returns>Queue response with ID and status</returns>
        public async Task<QueueEmailResponse> QueueSimpleEmailAsync(
            string to,
            string subject,
            string body,
            bool isHtml = true,
            string priority = "Normal",
            DateTime? scheduledSendTime = null)
        {
            var request = new QueueEmailRequest
            {
                ToEmails = to,
                Subject = subject,
                Body = body,
                IsHtml = isHtml,
                Priority = ConvertPriorityToInt(priority), // ✅ FIXED: Convert to int
                ScheduledFor = scheduledSendTime, // ✅ FIXED: Correct property name
                CreatedBy = "API_USER",
                RequestSource = "DT_PODSystem"
            };

            return await QueueEmailAsync(request);
        }

        // <summary>
        /// Queue an email with CC and BCC recipients
        /// </summary>
        /// <param name="to">Primary recipient</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="cc">CC recipients (optional)</param>
        /// <param name="bcc">BCC recipients (optional)</param>
        /// <param name="priority">Email priority (default: Normal)</param>
        /// <param name="scheduledSendTime">Optional scheduled send time</param>
        /// <returns>Queue response with ID and status</returns>
        public async Task<QueueEmailResponse> QueueEmailWithCopyAsync(
            string to,
            string subject,
            string body,
            List<string>? cc = null,
            List<string>? bcc = null,
            string priority = "Normal",
            DateTime? scheduledSendTime = null)
        {
            var request = new QueueEmailRequest
            {
                ToEmails = to,
                CcEmails = cc != null && cc.Any() ? string.Join(",", cc) : null,
                BccEmails = bcc != null && bcc.Any() ? string.Join(",", bcc) : null,
                Subject = subject,
                Body = body,
                IsHtml = true,
                Priority = ConvertPriorityToInt(priority), // ✅ FIXED: Convert to int
                ScheduledFor = scheduledSendTime, // ✅ FIXED: Correct property name
                CreatedBy = "API_USER",
                RequestSource = "DT_PODSystem"
            };

            return await QueueEmailAsync(request);
        }

        // <summary>
        /// Convert string priority to integer for API
        /// </summary>
        /// <param name="priority">Priority string (Low, Normal, High)</param>
        /// <returns>Priority integer (1, 2, 3)</returns>
        private int ConvertPriorityToInt(string priority)
        {
            return priority?.ToLower() switch
            {
                "low" => 1,
                "normal" => 2,
                "high" => 3,
                _ => 2 // Default to Normal
            };
        }

        /// <summary>
        /// Queue notification emails to multiple recipients
        /// </summary>
        /// <param name="recipients">List of recipient email addresses</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="sendIndividually">Whether to send individual emails (default: true)</param>
        /// <param name="priority">Email priority (default: Normal)</param>
        /// <param name="scheduledSendTime">Optional scheduled send time</param>
        /// <returns>Bulk queue response</returns>
        public async Task<BulkQueueEmailResponse> QueueNotificationEmailAsync(
            List<string> recipients,
            string subject,
            string body,
            bool sendIndividually = true,
            string priority = "Normal",
            DateTime? scheduledSendTime = null)
        {
            var request = new QueueBulkEmailRequest
            {
                Recipients = recipients, // ✅ FIXED: Direct string list, no BulkEmailRecipient
                Subject = subject,
                Body = body,
                IsHtml = true,
                Priority = ConvertPriorityToInt(priority), // ✅ FIXED: Convert to int
                ScheduledFor = scheduledSendTime, // ✅ FIXED: Correct property name
                SendIndividually = sendIndividually,
                CreatedBy = "API_USER",
                RequestSource = "DT_PODSystem"
            };

            return await QueueBulkEmailAsync(request);
        }


        #endregion

    }
}
