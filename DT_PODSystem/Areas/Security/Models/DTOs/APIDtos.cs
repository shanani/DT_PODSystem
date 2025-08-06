
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Spreadsheet;

#region Queue Email Request DTOs



namespace DT_PODSystem.Areas.Security.Models.DTOs
{
    public class QueueEmailRequest
    {
        [Required]
        public string ToEmails { get; set; } = string.Empty;  // ✅ Matches server
        public string? CcEmails { get; set; }                 // ✅ Matches server
        public string? BccEmails { get; set; }                // ✅ Matches server

        [Required]
        public string Subject { get; set; } = string.Empty;   // ✅ Matches server

        [Required]
        public string Body { get; set; } = string.Empty;      // ✅ Matches server

        public bool IsHtml { get; set; } = true;              // ✅ Matches server
        public int Priority { get; set; } = 2;                // ✅ Matches server (integer)
        public List<EmailAttachmentRequest> Attachments { get; set; } = new(); // ✅ Matches server
        public DateTime? ScheduledFor { get; set; }           // ✅ Matches server
        public string? CreatedBy { get; set; }                // ✅ Matches server
        public string? RequestSource { get; set; }            // ✅ Matches server
    }


    public class QueueBulkEmailRequest
    {
        [Required]
        public List<string> Recipients { get; set; } = new(); // ✅ Matches server (List<string>, not objects)

        [Required]
        public string Subject { get; set; } = string.Empty;   // ✅ Matches server

        [Required]
        public string Body { get; set; } = string.Empty;      // ✅ Matches server

        public bool IsHtml { get; set; } = true;              // ✅ Matches server
        public int Priority { get; set; } = 2;                // ✅ Matches server
        public bool SendIndividually { get; set; } = true;    // ✅ Matches server
        public List<EmailAttachmentRequest> Attachments { get; set; } = new(); // ✅ Matches server
        public DateTime? ScheduledFor { get; set; }           // ✅ Matches server
        public string? CreatedBy { get; set; }                // ✅ Matches server
        public string? RequestSource { get; set; }            // ✅ Matches server
    }

    // ==========================================
    // 3. ADD EmailAttachmentRequest to match server
    // ==========================================

    public class EmailAttachmentRequest
    {
        [Required]
        public string FileName { get; set; } = string.Empty;  // ✅ Matches server

        [Required]
        public string Content { get; set; } = string.Empty;   // ✅ Matches server (base64 string)

        public string? ContentType { get; set; }              // ✅ Matches server
        public string? FilePath { get; set; }                 // ✅ Matches server
    }

    // ==========================================
    // 4. ADD Response models to match server
    // ==========================================

    public class QueueEmailResponse
    {
        public Guid QueueId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public DateTime? ScheduledFor { get; set; }
        public DateTime? EstimatedProcessingTime { get; set; }
    }

    public class BulkQueueEmailResponse
    {
        public List<QueueEmailResponse> Results { get; set; } = new();
        public int TotalQueued { get; set; }
        public int SuccessfulQueues { get; set; }
        public int FailedQueues { get; set; }
        public double SuccessRate { get; set; }
        public TimeSpan TotalProcessingTime { get; set; }
    }


    public class QueueTemplateEmailRequest
    {
        [Required]
        [EmailAddress]
        public string To { get; set; } = string.Empty;

        public string? Cc { get; set; }
        public string? Bcc { get; set; }

        [Required]
        public string TemplateId { get; set; } = string.Empty;

        [Required]
        public Dictionary<string, string> TemplateData { get; set; } = new();

        public string Priority { get; set; } = "Normal";
        public DateTime? ScheduledSendTime { get; set; }
        public List<EmailAttachmentDto> Attachments { get; set; } = new();
    }


    public class EmailAttachmentDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public bool IsEmbedded { get; set; } = false;
        public string? ContentId { get; set; }
        public byte[]? FileData { get; set; }
    }


    #endregion

    #region Queue Email Response DTOs



    public class EmailStatusResponse
    {
        public Guid QueueId { get; set; }
        public string Status { get; set; } = string.Empty; // Queued, Processing, Sent, Failed, Cancelled
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DateTime? SentAt { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public string Priority { get; set; } = string.Empty;
    }

    public class QueueHealthResponse
    {
        public bool IsHealthy { get; set; }
        public int PendingEmails { get; set; }
        public int ProcessingEmails { get; set; }
        public int FailedEmails { get; set; }
        public int CompletedToday { get; set; }
        public DateTime LastProcessedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Issues { get; set; } = new();
    }

    public class QueueStatisticsResponse
    {
        public int TotalQueued { get; set; }
        public int TotalSent { get; set; }
        public int TotalFailed { get; set; }
        public int TotalCancelled { get; set; }
        public decimal SuccessRate { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public List<DailyQueueStatsDto> DailyStats { get; set; } = new();
    }

    public class DailyQueueStatsDto
    {
        public DateTime Date { get; set; }
        public int Queued { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public int Cancelled { get; set; }
    }

    public class PagedEmailQueueResponse
    {
        public List<EmailQueueItemDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }

    public class EmailQueueItemDto
    {
        public Guid QueueId { get; set; }
        public string RecipientEmail { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime QueuedAt { get; set; }
        public DateTime? ScheduledSendTime { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    #endregion
}