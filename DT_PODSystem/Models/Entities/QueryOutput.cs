using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Query outputs - renamed from CalculatedField, now belongs to Query instead of Template
    /// FIXED VERSION - Added missing properties used in QueryService
    /// </summary>
    public class QueryOutput : BaseEntity
    {
        [Required]
        public int QueryId { get; set; }

        // OPTIONAL: Link to canvas (can be null if multiple canvas support)
        public int? FormulaCanvasId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? DisplayName { get; set; }

        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;

        [StringLength(500)]
        public string? Description { get; set; }

        // STANDARDIZED FORMULA (No "=" prefix, no output name)
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string FormulaExpression { get; set; } = string.Empty;
        // Example: "([Input:InvoiceAmount] * 0.15)"

        // OUTPUT EXECUTION ORDER (Critical for dependencies)
        public int ExecutionOrder { get; set; } = 0;

        // AUTO-PARSED DEPENDENCIES (JSON arrays)
        [StringLength(1000)]
        public string? InputDependencies { get; set; } = "[]";    // ["Sales", "Amount"]

        [StringLength(1000)]
        public string? OutputDependencies { get; set; } = "[]";   // ["TaxAmount", "Discount"]

        [StringLength(1000)]
        public string? GlobalDependencies { get; set; } = "[]";   // ["TaxRate", "MaxDiscount"]

        [StringLength(1000)]
        public string? LocalDependencies { get; set; } = "[]";    // ["AdminFee", "ProcessFee"]

        // FORMATTING (Keep for worker)
        [StringLength(100)]
        public string? FormatString { get; set; } = "N2";         // Number format
        public int DecimalPlaces { get; set; } = 2;
        [StringLength(10)]
        public string? CurrencySymbol { get; set; }

        // VALIDATION
        public bool IsValid { get; set; } = false;
        [StringLength(1000)]
        public string? ValidationErrors { get; set; }
        public DateTime? LastValidated { get; set; }

        // OUTPUT CONFIGURATION  
        public bool IncludeInOutput { get; set; } = true;
        public bool IsRequired { get; set; } = false;
        [StringLength(500)]
        public string? DefaultValue { get; set; }

        // DISPLAY PROPERTIES
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; } = true;

        // ✅ ADDED MISSING PROPERTIES from QueryService usage
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("QueryId")]
        public virtual Query Query { get; set; } = null!;

        [ForeignKey("FormulaCanvasId")]
        public virtual FormulaCanvas? FormulaCanvas { get; set; }
    }
}