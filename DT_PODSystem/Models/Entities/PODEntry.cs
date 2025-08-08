using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DT_PODSystem.Models.Enums;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// PODEntry - Input fields and tables configuration for POD
    /// Stores both single values and table structures as normalized entries
    /// </summary>
    public class PODEntry : BaseEntity
    {
        [Required]
        public int PODId { get; set; }

        [Required]
        [StringLength(50)]
        public string EntryType { get; set; } = string.Empty; // 'single' or 'table'

        [Required]
        public int EntryOrder { get; set; } = 0;

        [Required]
        public string EntryData { get; set; } = string.Empty; // JSON for individual entry

        [StringLength(200)]
        public string? EntryName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Metadata for better organization
        [StringLength(100)]
        public string? Category { get; set; } // Optional grouping

        public bool IsRequired { get; set; } = false;

        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("PODId")]
        public virtual POD POD { get; set; } = null!;
    }
}