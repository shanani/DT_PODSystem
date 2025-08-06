using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Multiple files per template (1:M relationship)
    /// </summary>
    public class TemplateAttachment : BaseEntity
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        public int UploadedFileId { get; set; }

        [Required]
        public AttachmentType Type { get; set; }

        [StringLength(200)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; } = false;

        // PDF specific properties
        public int? PageCount { get; set; }

        [StringLength(50)]
        public string? PdfVersion { get; set; }

        public bool HasFormFields { get; set; } = false;

        // Navigation properties
        [ForeignKey("TemplateId")]
        public virtual PdfTemplate Template { get; set; } = null!;

        [ForeignKey("UploadedFileId")]
        public virtual UploadedFile UploadedFile { get; set; } = null!;

        [StringLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        [StringLength(255)]
        public string SavedFileName { get; set; } = string.Empty;

        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;



    }
}