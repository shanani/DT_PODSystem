using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// POD (Process Owner Document) - Parent entity containing business logic and organizational details
    /// Templates are now technical children of POD for PDF processing configuration
    /// </summary>
    public class POD : BaseEntity
    {
        // Business Identification
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string PODCode { get; set; } = Guid.NewGuid().ToString(); // Auto-generated, will be customized later

        [StringLength(1000)]
        public string? Description { get; set; }

        // Optional Business References
        [StringLength(100)]
        public string? PONumber { get; set; }

        [StringLength(100)]
        public string? ContractNumber { get; set; }

        // Organizational relationships (moved from PdfTemplate)
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public int? VendorId { get; set; }

        // Business Configuration
        [Required]
        public AutomationStatus AutomationStatus { get; set; } = AutomationStatus.PDF;

        [Required]
        public ProcessingFrequency Frequency { get; set; } = ProcessingFrequency.Monthly;

        // SPOC (Single Point of Contact) Users
        [StringLength(100)]
        public string? VendorSPOCUsername { get; set; }

        [StringLength(100)]
        public string? GovernorSPOCUsername { get; set; }

        [StringLength(100)]
        public string? FinanceSPOCUsername { get; set; }

        // Business Status and Approval
        [Required]
        public PODStatus Status { get; set; } = PODStatus.Draft;

        [StringLength(50)]
        public string? Version { get; set; } = "1.0";

        public bool RequiresApproval { get; set; } = false;

        public bool IsFinancialData { get; set; } = false;

        public int ProcessingPriority { get; set; } = 5; // 1-10 scale

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public DateTime? LastProcessedDate { get; set; }

        public int ProcessedCount { get; set; } = 0;

        // Navigation properties for organizational relationships
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }

        // Navigation properties for child entities
        public virtual ICollection<PdfTemplate> Templates { get; set; } = new List<PdfTemplate>();

        public virtual ICollection<PODAttachment> Attachments { get; set; } = new List<PODAttachment>();
         

        // ✅ NEW: Add POD Entries navigation property
        public virtual ICollection<PODEntry> Entries { get; set; } = new List<PODEntry>();

        // Processed files will be linked through templates, but we can add direct navigation if needed
        // public virtual ICollection<ProcessedFile> ProcessedFiles { get; set; } = new List<ProcessedFile>();
    }
}