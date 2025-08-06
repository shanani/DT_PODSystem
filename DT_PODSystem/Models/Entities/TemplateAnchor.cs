using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Template-level calibration anchors for coordinate offset detection and confidence measurement
    /// </summary>
    public class TemplateAnchor : BaseEntity
    {
        // Template association
        [Required]
        public int TemplateId { get; set; }

        // Page location
        [Required]
        public int PageNumber { get; set; } = 1;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        // Rectangular area coordinates (double for PDF precision)
        [Required]
        public double X { get; set; }

        [Required]
        public double Y { get; set; }

        [Required]
        public double Width { get; set; }

        [Required]
        public double Height { get; set; }

        // Reference text to find in document (MAIN calibration text)
        [Required]
        [StringLength(200)]
        public string ReferenceText { get; set; } = string.Empty;

        // Optional regex pattern for flexible text matching
        [StringLength(300)]
        public string? ReferencePattern { get; set; }

        // Validation settings
        public bool IsRequired { get; set; } = true;
        public decimal ConfidenceThreshold { get; set; } = 0.8m;

        // Display properties
        public int DisplayOrder { get; set; }

        [StringLength(50)]
        public string? Color { get; set; } = "#00C48C";

        // UI properties
        public bool IsVisible { get; set; } = true;

        [StringLength(50)]
        public string? BorderColor { get; set; } = "#00C48C";

        // Navigation property to Template
        [ForeignKey("TemplateId")]
        public virtual PdfTemplate Template { get; set; } = null!;
    }
}