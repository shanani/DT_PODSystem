using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// Department lookup with General Directorate relationship
    /// </summary>
    public class Department : BaseEntity
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;



        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? ManagerName { get; set; }

        [StringLength(100)]
        public string? ContactEmail { get; set; }

        [StringLength(20)]
        public string? ContactPhone { get; set; }

        public int DisplayOrder { get; set; }

        // Foreign Key
        [Required]
        public int GeneralDirectorateId { get; set; }

        // Navigation properties
        [ForeignKey("GeneralDirectorateId")]
        public virtual GeneralDirectorate GeneralDirectorate { get; set; } = null!;

        public virtual ICollection<PdfTemplate> Templates { get; set; } = new List<PdfTemplate>();
    }
}