using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// System audit trail for all user actions and system events
    /// </summary>
    public class AuditLog : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // Create, Update, Delete, View, Process, etc.

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = string.Empty; // Template, Output, User, etc.

        public int? EntityId { get; set; }

        [StringLength(200)]
        public string? EntityName { get; set; }

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? Description { get; set; }

        // Request details
        [StringLength(500)]
        public string? RequestUrl { get; set; }

        [StringLength(20)]
        public string? HttpMethod { get; set; }

        [StringLength(100)]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        public string? UserAgent { get; set; }

        // Change tracking
        [Column(TypeName = "nvarchar(max)")]
        public string? OldValues { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? NewValues { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? AffectedColumns { get; set; }

        // Result information
        [Required]
        public bool IsSuccess { get; set; } = true;

        [StringLength(1000)]
        public string? ErrorMessage { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? StackTrace { get; set; }

        // Performance metrics
        public long ExecutionTimeMs { get; set; } = 0;

        // Security and compliance
        [StringLength(50)]
        public string? SecurityLevel { get; set; } // Public, Internal, Confidential, Restricted

        public bool IsFinancialData { get; set; } = false;

        public bool RequiresApproval { get; set; } = false;

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        // Additional context
        [StringLength(100)]
        public string? SessionId { get; set; }

        [StringLength(100)]
        public string? CorrelationId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? AdditionalData { get; set; }
    }
}