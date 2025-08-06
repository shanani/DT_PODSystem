using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Main template definitions with organizational relationships
    /// </summary>
    public class PdfTemplate : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string NamingConvention { get; set; } = "DOC_POD";

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public TemplateStatus Status { get; set; } = TemplateStatus.Draft;

        [StringLength(50)]
        public string? Version { get; set; } = "1.0";

        // Organizational relationships
        [Required]
        public int CategoryId { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        public int? VendorId { get; set; }

        // Template configuration
        public bool RequiresApproval { get; set; } = false;

        public bool IsFinancialData { get; set; } = false;

        public int ProcessingPriority { get; set; } = 5; // 1-10 scale

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public DateTime? LastProcessedDate { get; set; }

        public int ProcessedCount { get; set; } = 0;

        // Navigation properties
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;

        [ForeignKey("DepartmentId")]
        public virtual Department Department { get; set; } = null!;

        [ForeignKey("VendorId")]
        public virtual Vendor? Vendor { get; set; }

        public virtual ICollection<TemplateAttachment> Attachments { get; set; } = new List<TemplateAttachment>();
        public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();

        public virtual ICollection<ProcessedFile> ProcessedFiles { get; set; } = new List<ProcessedFile>();

        public virtual ICollection<TemplateAnchor> TemplateAnchors { get; set; } = new List<TemplateAnchor>();
    }
}