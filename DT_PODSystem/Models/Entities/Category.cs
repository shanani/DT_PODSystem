using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Template categories for organizational grouping
    /// </summary>
    public class Category : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(20)]
        public string? ColorCode { get; set; }

        [StringLength(50)]
        public string? IconClass { get; set; }

        public int DisplayOrder { get; set; }

        // Navigation properties
        public virtual ICollection<PdfTemplate> Templates { get; set; } = new List<PdfTemplate>();
    }
}