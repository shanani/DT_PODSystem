// Areas/Security/Models/Entities/PermissionType.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    /// <summary>
    /// Represents a category of permissions (Domain, Zone, Vendor, Workflow, etc.)
    /// </summary>
    public class PermissionType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Icon { get; set; } = "fas fa-cog";

        [StringLength(20)]
        public string Color { get; set; } = "primary";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemType { get; set; } = false;

        // Audit
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // Navigation
        public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
    }
}

