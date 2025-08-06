using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// PODAttachment - Official documents attached to POD (contracts, POs, specifications, etc.)
    /// Clean design: File information accessed only through UploadedFile navigation
    /// </summary>
    public class PODAttachment : BaseEntity
    {
        [Required]
        public int PODId { get; set; }

        [Required]
        public int UploadedFileId { get; set; }

        [Required]
        public PODAttachmentType Type { get; set; } = PODAttachmentType.Contract;

        [StringLength(200)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsPrimary { get; set; } = false;

        // Official document metadata (document-specific properties)
        [StringLength(100)]
        public string? DocumentNumber { get; set; }

        public DateTime? DocumentDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(100)]
        public string? IssuedBy { get; set; }

        [StringLength(50)]
        public string? DocumentVersion { get; set; }

        // Document approval workflow
        public bool RequiresApproval { get; set; } = false;

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        [StringLength(500)]
        public string? ApprovalNotes { get; set; }

        // Document status
        [StringLength(50)]
        public string? DocumentStatus { get; set; } = "Active"; // Active, Expired, Superseded, etc.

        // Navigation properties
        [ForeignKey("PODId")]
        public virtual POD POD { get; set; } = null!;

        [ForeignKey("UploadedFileId")]
        public virtual UploadedFile UploadedFile { get; set; } = null!;

        // ✅ File information accessed via: UploadedFile.OriginalFileName, UploadedFile.SavedFileName, UploadedFile.FilePath
    }
}