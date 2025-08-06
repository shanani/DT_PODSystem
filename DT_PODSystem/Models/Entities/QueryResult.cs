// ✅ SIMPLIFIED: QueryResult - Single entity to store calculated query outputs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// QueryResult - stores individual calculated outputs from Query execution
    /// Similar to ProcessedField but for calculated outputs instead of extracted fields
    /// </summary>
    public class QueryResult : BaseEntity
    {
        [Required]
        public int QueryId { get; set; }

        [Required]
        public int QueryOutputId { get; set; }

        [Required]
        public int ProcessedFileId { get; set; }

        [Required]
        [StringLength(6)]
        public string PeriodId { get; set; } = string.Empty; // yyyyMM

        [Required]
        [StringLength(100)]
        public string OutputName { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string CalculatedValue { get; set; }

        [StringLength(50)]
        public string OutputDataType { get; set; } = "Number";

        // Calculation details for audit
        [Column(TypeName = "nvarchar(max)")]
        public string OriginalFormula { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string ProcessedFormula { get; set; }

        // Execution details
        public DateTime ExecutedDate { get; set; } = DateTime.UtcNow;
        public long ExecutionTimeMs { get; set; }

        // Confidence and validation
        public decimal CalculationConfidence { get; set; } = 1.0m;
        public bool IsValid { get; set; } = true;

        [StringLength(500)]
        public string ValidationErrors { get; set; }

        // Approval workflow
        public bool NeedApproval { get; set; } = false;
        public bool HasFinancialData { get; set; } = false;
        public bool IsApproved { get; set; } = false;

        [StringLength(100)]
        public string ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }

        // Navigation properties
        [ForeignKey("QueryId")]
        public virtual Query Query { get; set; } = null!;

        [ForeignKey("QueryOutputId")]
        public virtual QueryOutput QueryOutput { get; set; } = null!;

        [ForeignKey("ProcessedFileId")]
        public virtual ProcessedFile ProcessedFile { get; set; } = null!;
    }
}