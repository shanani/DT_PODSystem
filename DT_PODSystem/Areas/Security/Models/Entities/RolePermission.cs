
using System;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    public class RolePermission
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; } = true;
        public DateTime GrantedAt { get; set; }
        public string GrantedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation - using your existing SecurityRole
        public virtual SecurityRole Role { get; set; }
        public virtual Permission Permission { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; }

        // Helper properties
        public bool IsEffective => IsActive && IsGranted && RevokedAt == null;
        public string StatusBadge => IsEffective ? "bg-success" : "bg-danger";
        public string StatusText => IsEffective ? "Granted" : "Revoked";


    }
}

