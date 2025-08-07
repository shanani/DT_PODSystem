using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// PdfTemplate - Simplified technical entity for PDF processing configuration
    /// Now a child of POD. Contains only technical PDF processing settings.
    /// Business logic (Name, Category, Department, Vendor, Description) moved to POD parent.
    /// </summary>
    public class PdfTemplate : BaseEntity
    {
        // Parent POD relationship
        [Required]
        public int PODId { get; set; }


        [StringLength(100)]
        [Required]
        public string? Title { get; set; } = "Untitled Template";
         

        // Technical PDF processing configuration
        [Required]
        [StringLength(100)]
        public string NamingConvention { get; set; } = "DOC_POD";

        [Required]
        public TemplateStatus Status { get; set; } = TemplateStatus.Draft;

        [StringLength(50)]
        public string? Version { get; set; } = "1.0";

        // Technical processing settings
        public int ProcessingPriority { get; set; } = 5; // 1-10 scale (can override POD priority)

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public DateTime? LastProcessedDate { get; set; }

        public int ProcessedCount { get; set; } = 0;

        // Technical notes for this specific template configuration
        [StringLength(500)]
        public string? TechnicalNotes { get; set; }

        // PDF-specific settings
        public bool HasFormFields { get; set; } = false;

        [StringLength(50)]
        public string? ExpectedPdfVersion { get; set; }

        public int? ExpectedPageCount { get; set; }

        // Navigation properties
        [ForeignKey("PODId")]
        public virtual POD POD { get; set; } = null!;

        // Technical child entities - PDF processing specific
        public virtual ICollection<TemplateAttachment> Attachments { get; set; } = new List<TemplateAttachment>();
        public virtual ICollection<FieldMapping> FieldMappings { get; set; } = new List<FieldMapping>();
        public virtual ICollection<TemplateAnchor> TemplateAnchors { get; set; } = new List<TemplateAnchor>();

        // Processing results still linked to template for technical tracking
        public virtual ICollection<ProcessedFile> ProcessedFiles { get; set; } = new List<ProcessedFile>();
    }
}