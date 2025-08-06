using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Models.Entities
{
    /// <summary>
    /// General Directorate lookup for organizational hierarchy
    /// </summary>
    public class GeneralDirectorate : BaseEntity
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

        // Navigation properties
        public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
    }
}