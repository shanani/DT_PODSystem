using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    public class SecurityRole
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // ADD MISSING PROPERTIES
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        // Navigation Properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<SecurityUserRole> UserRoles { get; set; } = new List<SecurityUserRole>();


    }
}