using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Visual PDF field mappings with coordinates and validation
    /// </summary>
    public class FieldMapping : BaseEntity
    {
        [Required]
        public int TemplateId { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DisplayName { get; set; }

        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;

        [StringLength(500)]
        public string? Description { get; set; }

        // PDF Coordinates (pixels from top-left)
        [Required]
        public double X { get; set; }

        [Required]
        public double Y { get; set; }

        [Required]
        public double Width { get; set; }

        [Required]
        public double Height { get; set; }

        [Required]
        public int PageNumber { get; set; } = 1;

        // Validation rules
        [Required]
        public bool IsRequired { get; set; } = false;

        [StringLength(500)]
        public string? ValidationPattern { get; set; }

        [StringLength(200)]
        public string? ValidationMessage { get; set; }

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        [StringLength(500)]
        public string? DefaultValue { get; set; }

        // OCR/Text extraction settings
        public bool UseOCR { get; set; } = true;

        [StringLength(100)]
        public string? OCRLanguage { get; set; } = "eng";

        public decimal OCRConfidenceThreshold { get; set; } = 0.7m;

        // Display settings
        public int DisplayOrder { get; set; }

        [StringLength(50)]
        public string? BorderColor { get; set; } = "#A54EE1";

        public bool IsVisible { get; set; } = true;

        // Navigation properties
        [ForeignKey("TemplateId")]
        public virtual PdfTemplate Template { get; set; } = null!;


    }
}