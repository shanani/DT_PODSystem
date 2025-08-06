using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
 

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// ProcessedField - represents extracted field data from processed documents
    /// ✅ FIXED: Should reference FieldMappingId not CalculatedFieldId based on navigation property
    /// </summary>
    public class ProcessedField : BaseEntity
    {
        [Required]
        public int ProcessedFileId { get; set; }

        // ✅ FIXED: Should be FieldMappingId to match navigation property
        [Required]
        public int FieldMappingId { get; set; }

        [Required]
        [StringLength(100)]
        public string FieldName { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string OutputValue { get; set; }

        [StringLength(50)]
        public string OutputDataType { get; set; } = "String"; // String, Number, Date, Currency

        [StringLength(20)]
        public string CurrencySymbol { get; set; }

        public int? DecimalPlaces { get; set; }

        // Confidence metrics
        public decimal ExtractionConfidence { get; set; } = 0;
        public decimal CalculationConfidence { get; set; } = 0;

        // Validation
        public bool IsValid { get; set; } = true;

        [StringLength(500)]
        public string ValidationErrors { get; set; }

        // Navigation properties
        [ForeignKey("ProcessedFileId")]
        public virtual ProcessedFile ProcessedFile { get; set; } = null!;

        [ForeignKey("FieldMappingId")]
        public virtual FieldMapping MappedField { get; set; } = null!;
    }
}