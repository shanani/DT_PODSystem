using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// TemplateAttachment - PDF processing files linked to templates
    /// Clean design: File information accessed only through UploadedFile navigation
    /// </summary>
    public class TemplateAttachment : BaseEntity
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int UploadedFileId { get; set; }

        [Required]
        public AttachmentType Type { get; set; } = AttachmentType.Reference;

        [StringLength(200)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        // PDF-specific technical properties (attachment-level metadata)
        public int? PageCount { get; set; }

        [StringLength(50)]
        public string? PdfVersion { get; set; }

        public bool HasFormFields { get; set; } = false;

        // Processing metadata
        public DateTime? LastProcessed { get; set; }

        [StringLength(100)]
        public string? ProcessingStatus { get; set; }

        // Navigation properties
        [ForeignKey("TemplateId")]
        public virtual PdfTemplate Template { get; set; } = null!;

        [ForeignKey("UploadedFileId")]
        public virtual UploadedFile UploadedFile { get; set; } = null!;

        // ✅ File information accessed via: UploadedFile.OriginalFileName, UploadedFile.SavedFileName, UploadedFile.FilePath
    }
}