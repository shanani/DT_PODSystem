using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// UploadedFile - Central repository for all file uploads
    /// Single source of truth for file information across the entire system
    /// Now supports direct template relationship (one-to-one)
    /// </summary>
    public class UploadedFile : BaseEntity
    {
        [Required]
        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string SavedFileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSize { get; set; }

        [StringLength(100)]
        public string? FileHash { get; set; }

        [Required]
        public bool IsTemporary { get; set; } = true;

        public DateTime? ProcessedDate { get; set; }

        [StringLength(100)]
        public string? ProcessedBy { get; set; }

        // File metadata
        [StringLength(100)]
        public string? MimeType { get; set; }

        public DateTime? ExpiryDate { get; set; } // For temporary files cleanup

        [StringLength(200)]
        public string? UploadSource { get; set; } // "Wizard", "POD", "Bulk", etc.

        // Navigation properties - Central hub for all file references

        // ✅ REMOVED: TemplateAttachment collection (replaced with direct relationship)
        // public virtual ICollection<TemplateAttachment> TemplateAttachments { get; set; } = new List<TemplateAttachment>();

        // ✅ NEW: Direct template relationship (one-to-one)
        public virtual PdfTemplate? Template { get; set; }

        public virtual ICollection<PODAttachment> PODAttachments { get; set; } = new List<PODAttachment>();

        // Future extensibility for other attachment types
        // public virtual ICollection<UserProfileAttachment> UserProfileAttachments { get; set; } = new List<UserProfileAttachment>();
        // public virtual ICollection<SystemDocumentAttachment> SystemDocumentAttachments { get; set; } = new List<SystemDocumentAttachment>();
    }
}