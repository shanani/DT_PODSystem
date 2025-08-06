using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Visual formula canvas - NOW BELONGS TO QUERY ONLY (TemplateVariable renamed to QueryConstant)
    /// </summary>
    public class FormulaCanvas : BaseEntity
    {
        [Required]
        public int QueryId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Canvas properties
        public int Width { get; set; } = 1200;
        public int Height { get; set; } = 800;
        public decimal ZoomLevel { get; set; } = 1.0m;

        // MAIN SERIALIZED STATE (Everything visual)
        [Column(TypeName = "nvarchar(max)")]
        public string? CanvasState { get; set; }

        // USER-VISIBLE FORMULAS (Multi-line display)
        [Column(TypeName = "nvarchar(max)")]
        public string? FormulaExpression { get; set; }

        // Validation results
        public bool IsValid { get; set; } = false;
        [StringLength(1000)]
        public string? ValidationErrors { get; set; }
        public DateTime? LastValidated { get; set; }

        // Version control
        [StringLength(20)]
        public string? Version { get; set; } = "1.0";
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [ForeignKey("QueryId")]
        public virtual Query Query { get; set; } = null!;

        // QueryOutputs for worker execution
        public virtual ICollection<QueryOutput> QueryOutputs { get; set; } = new List<QueryOutput>();
    }
}