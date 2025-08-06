using System;
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    public class SecurityUserRole
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; }
        public string AssignedBy { get; set; }
        public bool IsActive { get; set; }

        // ADD MISSING PROPERTIES
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; }

        // Navigation Properties
        public SecurityUser User { get; set; }
        public SecurityRole Role { get; set; }

        // EXTENSION METHODS/PROPERTIES
        public bool IsEffective => IsActive && (!RevokedAt.HasValue || RevokedAt > DateTime.UtcNow.AddHours(3));
        public string StatusText => IsEffective ? "Active" : "Inactive";
        public string StatusBadge => IsEffective ? "bg-success" : "bg-secondary";
    }
}