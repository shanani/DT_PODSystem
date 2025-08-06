using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.Entities
{
    public class ProcessedFile : BaseEntity
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(6)]
        public string PeriodId { get; set; } = string.Empty; // yyyyMM

        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string OriginalFilePath { get; set; } = string.Empty;

        [StringLength(500)]
        public string OrganizedFilePath { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = string.Empty; // Success, Failed, Fuzzy

        [StringLength(1000)]
        public string ProcessingMessage { get; set; }

        public bool NeedApproval { get; set; } = false;

        public bool HasFinancialInfo { get; set; } = false;

        public DateTime ProcessedDate { get; set; } = DateTime.UtcNow;

        // 🎯 NEW: Anchor-based confidence properties
        public decimal AnchorConfidence { get; set; } = 1.0m;        // Overall anchor text matching confidence

        public int AnchorsFound { get; set; } = 0;                   // How many anchors were successfully extracted

        public int AnchorsConfigured { get; set; } = 0;             // Total anchors configured for template

        public int AnchorsMatched { get; set; } = 0;                // How many anchors matched their reference text

        public string AnchorDetails { get; set; } = string.Empty;   // JSON details of each anchor result

        // Navigation
        public virtual PdfTemplate Template { get; set; } = null!;
        public virtual ICollection<ProcessedField> ProcessedFields { get; set; } = new List<ProcessedField>();
    }
}