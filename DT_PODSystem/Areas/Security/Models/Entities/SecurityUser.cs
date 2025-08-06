// SecurityUser.cs - Compatible with existing table structure (ApplicationUsers)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    public class SecurityUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        [StringLength(100)]
        public string FirstName { get; set; }

        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; }

        [StringLength(200)]
        public string Department { get; set; }

        [StringLength(200)]  // Updated from existing 100 to 200
        public string JobTitle { get; set; }

        [StringLength(20)]
        public string Mobile { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        // 🔥 CLEAN: Simple flags only (remove IsLocked)
        public bool IsActive { get; set; } = true;
        public bool IsAdmin { get; set; } = false;
        public bool IsSuperAdmin { get; set; } = false;

        // Security tracking
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; } = 0;
        public DateTime? LastLoginDate { get; set; }
        public DateTime? ExpirationDate { get; set; }

        // AD Integration
        public DateTime? LastADInfoUpdateTime { get; set; }
        public string ADObjectId { get; set; }

        // System tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow.AddHours(3);
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow.AddHours(3);

        [StringLength(100)]
        public string CreatedBy { get; set; }

        [StringLength(100)]
        public string UpdatedBy { get; set; }

        // Preferences (existing in the current structure)
        public bool ReceiveEmailNotifications { get; set; } = true;
        public bool ReceiveSystemNotifications { get; set; } = true;
        public bool ReceiveAnnouncementNotifications { get; set; } = true;
        public bool ReceiveMDTNotifications { get; set; } = true;

        [StringLength(10)]
        public string PreferredLanguage { get; set; } = "en";

        [StringLength(50)]
        public string TimeZone { get; set; } = "UTC";

        public byte[] Photo { get; set; }

        // Navigation properties - keeping existing structure
        public virtual ICollection<SecurityUserRole> UserRoles { get; set; } = new List<SecurityUserRole>();

        // 🔥 FIX: SecurityAuditLog uses SecurityUserId foreign key, not navigation property
        public virtual ICollection<SecurityAuditLog> CreatedAuditLogs { get; set; } = new List<SecurityAuditLog>();

        // 🔥 CLEAN: Computed Properties
        public string FullName => $"{FirstName} {LastName}".Trim();
        public string DisplayName => !string.IsNullOrWhiteSpace(FullName) ? FullName : Code;

        // 🔥 CLEAN: Access validation methods
        public bool HasValidAccess => IsActive && (ExpirationDate == null || ExpirationDate > DateTime.UtcNow.AddHours(3));

        public bool HasAdminAccess => HasValidAccess && IsAdmin;

        public bool HasSuperAdminAccess => HasValidAccess && IsSuperAdmin && !IsProductionMode();

        // Existing role checking methods
        public bool IsInRole(string roleName)
        {
            return UserRoles.Any(ur => ur.IsActive && ur.Role.IsActive && ur.Role.Name == roleName);
        }

        public List<string> GetRoles()
        {
            return UserRoles.Where(ur => ur.IsActive && ur.Role.IsActive)
                           .Select(ur => ur.Role.Name)
                           .ToList();
        }

        // 🔥 CLEAN: Convenience role properties (keeping existing pattern)
        public bool IsUser => IsInRole("User");
        public bool IsAuditor => IsInRole("Auditor");

        private bool IsProductionMode()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);
        }
    }
}