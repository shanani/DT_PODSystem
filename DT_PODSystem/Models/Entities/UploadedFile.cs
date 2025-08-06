using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Tracks all uploaded files before template creation
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

        // Navigation properties
        public virtual ICollection<TemplateAttachment> TemplateAttachments { get; set; } = new List<TemplateAttachment>();
    }
}