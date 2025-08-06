using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Query constants - renamed from TemplateVariable, now with nullable QueryId for global constants
    /// FIXED VERSION - Added missing properties used in QueryService
    /// </summary>
    public class QueryConstant : BaseEntity
    {
        // ✅ NULLABLE QueryId to allow global constants (NULL = global, value = query-specific)
        public int? QueryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? DisplayName { get; set; }

        public DataTypeEnum DataType { get; set; } = DataTypeEnum.Number;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(255)]
        public string? DefaultValue { get; set; }

        public bool IsRequired { get; set; } = false;

        public bool IsConstant { get; set; } = true; // Always true for constants

        public bool IsGlobal { get; set; } = false; // True = global (QueryId = NULL), False = query-specific

        // ✅ ADDED MISSING PROPERTIES from QueryService usage
        public bool IsActive { get; set; } = true;

        [StringLength(50)]
        public string? InputType { get; set; }

        [StringLength(1000)]
        public string? SelectOptions { get; set; }

        [StringLength(100)]
        public string? SystemSource { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [StringLength(50)]
        public string? ValidationPattern { get; set; }

        [StringLength(255)]
        public string? ValidationMessage { get; set; }

        // Navigation properties
        [ForeignKey("QueryId")]
        public virtual Query? Query { get; set; } // NULL for global constants
    }
}