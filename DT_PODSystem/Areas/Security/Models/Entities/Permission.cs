// Areas/Security/Models/Entities/Permission.cs
using System;

using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DT_PODSystem.Areas.Security.Models.Enums;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    /// <summary>
    /// Represents a hierarchical permission within a permission type
    /// </summary>
    public class Permission
    {
        public int Id { get; set; }
        public int PermissionTypeId { get; set; }

        // 🆕 HIERARCHICAL PROPERTIES
        public int? ParentPermissionId { get; set; }
        public int Level { get; set; } = 0; // 0 = root, 1 = child, 2 = grandchild, etc.
        public string? HierarchyPath { get; set; } // e.g., "1/5/12" for traversal

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(150)]
        public string DisplayName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(50)]
        public string Icon { get; set; } = "fas fa-key";

        [StringLength(20)]
        public string Color { get; set; } = "primary";

        public PermissionScope Scope { get; set; }
        public PermissionAction Action { get; set; }

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemPermission { get; set; } = false;

        // 🆕 HIERARCHICAL FLAGS
        public bool HasChildren => Children?.Any() == true;
        public bool IsRootPermission => ParentPermissionId == null;
        public bool CanHaveChildren { get; set; } = true; // Some permissions may not allow children

        // Audit
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // 🆕 HIERARCHICAL NAVIGATION PROPERTIES
        public virtual Permission? ParentPermission { get; set; }
        public virtual ICollection<Permission> Children { get; set; } = new List<Permission>();

        // Existing Navigation
        public virtual PermissionType PermissionType { get; set; }
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        // 🆕 HELPER METHODS
        public string GetFullHierarchyName()
        {
            if (ParentPermission == null)
                return DisplayName ?? Name;

            return $"{ParentPermission.GetFullHierarchyName()} → {DisplayName ?? Name}";
        }

        public List<Permission> GetAllDescendants()
        {
            var descendants = new List<Permission>();

            foreach (var child in Children)
            {
                descendants.Add(child);
                descendants.AddRange(child.GetAllDescendants());
            }

            return descendants;
        }

        public List<Permission> GetAncestors()
        {
            var ancestors = new List<Permission>();
            var current = ParentPermission;

            while (current != null)
            {
                ancestors.Insert(0, current);
                current = current.ParentPermission;
            }

            return ancestors;
        }

        public bool IsAncestorOf(Permission permission)
        {
            if (permission.ParentPermissionId == null)
                return false;

            var current = permission.ParentPermission;
            while (current != null)
            {
                if (current.Id == this.Id)
                    return true;
                current = current.ParentPermission;
            }

            return false;
        }

        public bool IsDescendantOf(Permission permission)
        {
            return permission.IsAncestorOf(this);
        }

        public int GetDepth()
        {
            int depth = 0;
            var current = ParentPermission;

            while (current != null)
            {
                depth++;
                current = current.ParentPermission;
            }

            return depth;
        }

        public void UpdateHierarchyPath()
        {
            if (ParentPermission == null)
            {
                HierarchyPath = Id.ToString();
            }
            else
            {
                HierarchyPath = string.IsNullOrEmpty(ParentPermission.HierarchyPath)
                    ? $"{ParentPermission.Id}/{Id}"
                    : $"{ParentPermission.HierarchyPath}/{Id}";
            }
        }
    }
}


