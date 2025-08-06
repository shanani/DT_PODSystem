using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Query entity - handles formula calculations and constants (previously Step 4 logic)
    /// </summary>
    public class Query : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public QueryStatus Status { get; set; } = QueryStatus.Draft;

        [StringLength(50)]
        public string? Version { get; set; } = "1.0";

        // Query configuration
        public bool IsActive { get; set; } = true;
        public int ExecutionPriority { get; set; } = 5; // 1-10 scale

        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        public DateTime? ApprovalDate { get; set; }

        public DateTime? LastExecutedDate { get; set; }

        public int ExecutionCount { get; set; } = 0;

        // Navigation properties
        public virtual ICollection<QueryConstant> QueryConstants { get; set; } = new List<QueryConstant>();
        public virtual ICollection<QueryOutput> QueryOutputs { get; set; } = new List<QueryOutput>();
        public virtual FormulaCanvas? FormulaCanvas { get; set; }
    }


}