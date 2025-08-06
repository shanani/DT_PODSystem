
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DT_PODSystem.Areas.Security.Models.ViewModels;


namespace DT_PODSystem.Areas.Security.Models.DTOs
{
    public class CacheStatisticsDto
    {
        public DateTime StartTime { get; set; }
        public int TotalHits { get; set; }
        public int TotalMisses { get; set; }
        public int TotalInvalidations { get; set; }
        public int TotalRequests => TotalHits + TotalMisses;
        public double HitRatio { get; set; }
        public Dictionary<string, int> HitsByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MissesByType { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> InvalidationsByType { get; set; } = new Dictionary<string, int>();
    }
    public class RolePermissionsTreeViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleDescription { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }

        // 🎯 REUSE: Same tree structure as PermissionsController
        public List<TreeNodeViewModel> TreeData { get; set; } = new();
        public List<int> AssignedPermissionIds { get; set; } = new();
        public object Statistics { get; set; } = new();

        // Helper properties
        public int TotalPermissions => CountPermissionsInTree(TreeData);
        public int AssignedPermissions => AssignedPermissionIds?.Count ?? 0;
        public int UnassignedPermissions => TotalPermissions - AssignedPermissions;

        public string AssignmentPercentage
        {
            get
            {
                if (TotalPermissions == 0) return "0";
                var percentage = (double)AssignedPermissions / TotalPermissions * 100;
                return percentage.ToString("F0");
            }
        }

        public string ProgressBarClass
        {
            get
            {
                var percentage = double.Parse(AssignmentPercentage);
                return percentage switch
                {
                    >= 75 => "bg-success",
                    >= 50 => "bg-warning",
                    >= 25 => "bg-info",
                    _ => "bg-danger"
                };
            }
        }

        /// <summary>
        /// Count total permissions in tree structure
        /// </summary>
        private int CountPermissionsInTree(List<TreeNodeViewModel> nodes)
        {
            int count = 0;
            foreach (var node in nodes)
            {
                if (node.Type == "permission" || node.Type == "permission_child")
                {
                    count++;
                }

                if (node.Children?.Any() == true)
                {
                    count += CountPermissionsInTree(node.Children);
                }
            }
            return count;
        }
    }


    public class EditPermissionRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? DisplayName { get; set; }
        public string? Description { get; set; }

        // 🚨 ADD THESE MISSING PROPERTIES:
        public int PermissionTypeId { get; set; }
        public int? ParentPermissionId { get; set; }

        public string Scope { get; set; } = "Global";
        public string Action { get; set; } = "Read";
        public string Icon { get; set; } = "fas fa-key";
        public string Color { get; set; } = "primary";
        public bool CanHaveChildren { get; set; } = true;
    }

    public class UserDto
    {
        public int ID { get; set; }
        public string Code { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

        // Photo properties
        public byte[] Photo { get; set; } // Binary data (for completeness)
        public string Photo_base64 { get; set; } // Base64 string
        public bool HasPhoto { get; set; } // Has photo flag
        public string PhotoDataUrl { get; set; } // Ready-to-use data URL for img src
        public string Initials { get; set; } // Fallback initials

        // 🔥 FIX: Simple properties - no computed getters causing circular references
        public string FullName { get; set; }
        public string DisplayName { get; set; }

        // 🔥 FIX: Simple boolean flags
        public bool IsActive { get; set; }  // Simple property, not computed
        public bool IsAdmin { get; set; }   // Change from field to property
        public bool IsSuperAdmin { get; set; } // Change from field to property

        public string Mobile { get; set; }
        public string Tel { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? LastADInfoUpdateTime { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 🔥 FIX: Computed properties with JsonIgnore to prevent serialization issues
        [JsonIgnore]
        public string RolesText => string.Join(", ", Roles);

        [JsonIgnore]
        public bool HasValidAccess => IsActive && (ExpirationDate == null || ExpirationDate > DateTime.UtcNow.AddHours(3));

        [JsonIgnore]
        public string StatusText => IsActive ? "Active" : "Inactive";

        [JsonIgnore]
        public string InitialsFromName => $"{FirstName?.Substring(0, 1)?.ToUpper()}{LastName?.Substring(0, 1)?.ToUpper()}";
    }
    public class LoginDto
    {

        public string Username { get; set; }

        public string Password { get; set; }

        public bool RememberMe { get; set; }

        public string SSOToken { get; set; }
    }

    public class ADUserDetails
    {
        public string Department { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string LoginName { get; set; }
        public string LoginNameWithDomain { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string HomePhone { get; set; }
        public string Extension { get; set; }
        public string Mobile { get; set; }
        public string Fax { get; set; }
        public string EmailAddress { get; set; }
        public string Title { get; set; }
        public string Company { get; set; }
        public string Manager { get; set; }
        public string ManagerName { get; set; }
        public bool Enabled { get; set; }
        public byte[] Photo { get; set; }

    }


    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string DisplayName { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }

        // 🔥 ADD MISSING ADMIN FLAGS
        public bool IsActive { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsSuperAdmin { get; set; }

        // 🔥 FIX: Use LockoutEnd instead of IsLocked
        public DateTime? LockoutEnd { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();

        // Computed properties
        public bool IsLockedOut => LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;

        public string StatusBadgeClass => IsActive switch
        {
            true when !IsLockedOut => "bg-success",
            true when IsLockedOut => "bg-warning",
            false => "bg-danger"
        };

        public string StatusText => IsActive switch
        {
            true when !IsLockedOut => "Active",
            true when IsLockedOut => "Locked",
            false => "Inactive"
        };

        public string LastLoginText => LastLoginDate?.ToString("MMM dd, yyyy") ?? "Never";
        public string RolesText => string.Join(", ", Roles);
    }

    public class AdminLoginResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public UserSummaryDto User { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string RedirectUrl { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public bool IsLockedOut { get; set; }
        public DateTime? LockoutEnd { get; set; }
    }

    // Security Statistics DTO
    public class SecurityStatisticsDto
    {
        public int TotalPermissionTypes { get; set; }
        public int ActivePermissionTypes { get; set; }
        public int SystemPermissionTypes { get; set; }
        public int CustomPermissionTypes { get; set; }

        public int TotalPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public int SystemPermissions { get; set; }
        public int CustomPermissions { get; set; }

        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int SystemRoles { get; set; }
        public int CustomRoles { get; set; }

        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int SuperAdminUsers { get; set; }

        public int RolePermissionAssignments { get; set; }
        public int UserRoleAssignments { get; set; }

        // Calculated properties
        public double PermissionTypeActivationRate => TotalPermissionTypes > 0
            ? (double)ActivePermissionTypes / TotalPermissionTypes * 100 : 0;

        public double PermissionActivationRate => TotalPermissions > 0
            ? (double)ActivePermissions / TotalPermissions * 100 : 0;

        public double UserActivationRate => TotalUsers > 0
            ? (double)ActiveUsers / TotalUsers * 100 : 0;


        public int InactiveUsers { get; set; }


        // These were the missing properties causing compilation errors:
        public Dictionary<string, int> UsersWithRoles { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> RolesWithPermissions { get; set; } = new Dictionary<string, int>();


        // Additional useful statistics
        public int UsersWithoutRoles { get; set; }
        public int RolesWithoutPermissions { get; set; }
        public int SuperAdminCount { get; set; }


        public int TotalRoleAssignments { get; set; }
        public int TotalPermissionAssignments { get; set; }
        public Dictionary<string, int> UsersByRole { get; set; } = new Dictionary<string, int>();

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow.AddHours(3);

        // Calculated percentages
        public double ActiveUsersPercentage => TotalUsers > 0 ? (double)ActiveUsers / TotalUsers * 100 : 0;
        public double ActiveRolesPercentage => TotalRoles > 0 ? (double)ActiveRoles / TotalRoles * 100 : 0;
        public double ActivePermissionsPercentage => TotalPermissions > 0 ? (double)ActivePermissions / TotalPermissions * 100 : 0;
        public double ActivePermissionTypesPercentage => TotalPermissionTypes > 0 ? (double)ActivePermissionTypes / TotalPermissionTypes * 100 : 0;
    }

    // Permission Type DTO
    public class PermissionTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "fas fa-folder";
        public string Color { get; set; } = "primary";
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        // Helper properties
        public string IconClass => !string.IsNullOrEmpty(Icon) ? Icon : "fas fa-folder";
        public string ColorClass => !string.IsNullOrEmpty(Color) ? Color : "primary";
        public bool IsSystemType { get; set; }
        public int PermissionsCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // Display properties
        public string StatusClass => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeClass => IsSystemType ? "warning" : "info";
        public string TypeText => IsSystemType ? "System" : "Custom";
        public int PermissionCount { get; set; }

    }

    // Permission DTO
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public int PermissionTypeId { get; set; }
        public string PermissionTypeName { get; set; }
        public string PermissionTypeIcon { get; set; }
        public string PermissionTypeColor { get; set; }
        public string Scope { get; set; }
        public string Action { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemPermission { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }

        // Display properties
        public string StatusClass => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeClass => IsSystemPermission ? "warning" : "info";
        public string TypeText => IsSystemPermission ? "System" : "Custom";
        public string FullName => !string.IsNullOrEmpty(Scope) && !string.IsNullOrEmpty(Action)
            ? $"{Scope}.{Action}" : Name;


        public int RolesCount { get; set; }

        public string StatusBadge => IsActive ? "bg-success" : "bg-secondary";

        public string TypeBadge => IsSystemPermission ? "bg-warning" : "badge-info";

        public string ActionIcon => GetActionIcon(Action);
        public string ScopeIcon => GetScopeIcon(Scope);

        public int? ParentPermissionId { get; internal set; }
        public int Level { get; set; }

        private string GetActionIcon(string action) => action switch
        {
            "Create" => "fas fa-plus",
            "Read" => "fas fa-eye",
            "Update" => "fas fa-edit",
            "Delete" => "fas fa-trash",
            "Execute" => "fas fa-play",
            "Approve" => "fas fa-check",
            _ => "fas fa-shield"
        };

        private string GetScopeIcon(string scope) => scope switch
        {
            "Global" => "fas fa-globe",
            "Department" => "fas fa-building",
            "Personal" => "fas fa-user",
            _ => "fas fa-shield"
        };

    }

    // Security Audit DTO
    public class SecurityAuditDto
    {
        public int Id { get; set; }
        public string ActionType { get; set; }
        public string Description { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string AdditionalData { get; set; }
        public bool IsSystemAction { get; set; }
        public string Severity { get; set; }

        // Display properties
        public string SeverityClass => Severity?.ToLower() switch
        {
            "critical" => "danger",
            "high" => "warning",
            "medium" => "info",
            "low" => "secondary",
            _ => "primary"
        };

        public string ActionTypeClass => ActionType?.ToLower() switch
        {
            "create" => "success",
            "update" => "warning",
            "delete" => "danger",
            "view" => "info",
            "login" => "primary",
            "logout" => "secondary",
            _ => "primary"
        };

        public string TimeAgo => GetTimeAgo(Timestamp);

        private string GetTimeAgo(DateTime timestamp)
        {
            var timeSpan = DateTime.UtcNow.AddHours(3) - timestamp;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 7)
                return $"{(int)timeSpan.TotalDays} days ago";

            return timestamp.ToString("dd/MM/yyyy");
        }


        public string Action { get; set; }

        public DateTime CreatedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

        // Additional metadata
        public string ActionIcon { get; set; }
        public string ActionColor { get; set; }
        public string RelativeTime { get; set; }


        public string Name { get; set; }

        public bool IsActive { get; set; }
        public bool IsSystemRole { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }

        public string CreatedBy { get; set; }
    }

    // Role Summary DTO
    public class RoleSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemRole { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }

        // Display properties
        public string StatusClass => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeClass => IsSystemRole ? "warning" : "info";
        public string TypeText => IsSystemRole ? "System" : "Custom";
    }

    // User Activity Summary DTO
    public class UserActivitySummary
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public DateTime LastActivity { get; set; }
        public string LastAction { get; set; }
        public int ActionsToday { get; set; }
        public bool IsOnline { get; set; }
        public string Role { get; set; }

        // Display properties
        public string StatusClass => IsOnline ? "success" : "secondary";
        public string StatusText => IsOnline ? "Online" : "Offline";
        public string ActivityLevel => ActionsToday switch
        {
            > 50 => "High",
            > 20 => "Medium",
            > 5 => "Low",
            _ => "Minimal"
        };

        public string ActivityLevelClass => ActionsToday switch
        {
            > 50 => "danger",
            > 20 => "warning",
            > 5 => "info",
            _ => "secondary"
        };
    }

    // System Health DTO
    public class SystemHealthDto
    {
        public bool DatabaseConnection { get; set; }
        public bool SecurityModule { get; set; }
        public bool IdentitySystem { get; set; }
        public bool FileSystem { get; set; }
        public bool EmailService { get; set; }
        public bool CacheService { get; set; }

        public int ResponseTimeMs { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }

        public DateTime LastChecked { get; set; }
        public TimeSpan Uptime { get; set; }

        // Overall health status
        public string OverallStatus => GetOverallStatus();
        public string OverallStatusClass => GetOverallStatusClass();

        private string GetOverallStatus()
        {
            if (!DatabaseConnection || !SecurityModule || !IdentitySystem)
                return "Critical";
            if (!FileSystem || !EmailService || !CacheService)
                return "Warning";
            if (ResponseTimeMs > 1000 || CpuUsage > 80 || MemoryUsage > 80)
                return "Degraded";

            return "Healthy";
        }

        private string GetOverallStatusClass()
        {
            return OverallStatus switch
            {
                "Critical" => "danger",
                "Warning" => "warning",
                "Degraded" => "info",
                "Healthy" => "success",
                _ => "secondary"
            };
        }

        public double HealthScore { get; set; }
        public string Status { get; set; }
        public string Color { get; set; }
        public int TotalEntities { get; set; }
        public int ActiveEntities { get; set; }
        public int UsersWithoutRoles { get; set; }
        public int InactiveEntities { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public List<HealthMetric> Metrics { get; set; } = new List<HealthMetric>();

    }


    #region Missing DTOs for Dashboard


    public class RoleDistributionDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string Description { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
        public string Color { get; set; }
        public bool IsSystemRole { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }

        // Helper properties for UI
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeBadge => IsSystemRole ? "danger" : "info";
        public string TypeText => IsSystemRole ? "System" : "Custom";
        public string DisplayColor => Color ?? (IsSystemRole ? "#dc3545" : "#007bff");
        public double UserPercentage { get; set; } // To be calculated by service
        public string UserCountText => $"{UserCount} user{(UserCount != 1 ? "s" : "")}";
        public string PermissionCountText => $"{PermissionCount} permission{(PermissionCount != 1 ? "s" : "")}";
    }

    #endregion

    #region Additional Missing DTOs that might be needed

    public class UserActivitySummaryDto
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string UserCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public int LoginCount { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int RoleChanges { get; set; }
        public int PermissionChanges { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }

        // Helper properties
        public string StatusBadge => IsActive ? (IsLocked ? "warning" : "success") : "secondary";
        public string StatusText => IsActive ? (IsLocked ? "Locked" : "Active") : "Inactive";
        public string LastLoginText => LastLoginAt?.ToString("MMM dd, yyyy") ?? "Never";
        public string ActivityLevel =>
            LoginCount >= 100 ? "High" :
            LoginCount >= 20 ? "Medium" : "Low";
        public string ActivityBadge =>
            LoginCount >= 100 ? "success" :
            LoginCount >= 20 ? "warning" : "secondary";
    }

    public class DashboardQuickStatsDto
    {
        public int TotalActiveUsers { get; set; }
        public int NewUsersThisMonth { get; set; }
        public int TotalActiveRoles { get; set; }
        public int TotalActivePermissions { get; set; }
        public int PermissionTypesCount { get; set; }
        public int RecentActivitiesCount { get; set; }
        public double SystemHealthScore { get; set; }
        public string HealthStatus { get; set; }
        public string HealthColor { get; set; }
        public DateTime LastUpdated { get; set; }

        // Trend indicators
        public int UserGrowthPercentage { get; set; }
        public int RoleGrowthPercentage { get; set; }
        public int PermissionGrowthPercentage { get; set; }
        public bool IsGrowthPositive => UserGrowthPercentage >= 0;
    }





    public class HealthMetric
    {
        public string Name { get; set; }
        public double Value { get; set; }
        public double MaxValue { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
    }

    public class ChartDataDto
    {
        public string Label { get; set; }
        public double Value { get; set; }
        public string Color { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class TimeSeriesDataDto
    {
        public DateTime Date { get; set; }
        public double Value { get; set; }
        public string Label { get; set; }
        public string Category { get; set; }
    }

    #endregion

    public class RolePermissionTreeDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public List<PermissionNodeDto> Permissions { get; set; } = new List<PermissionNodeDto>();
    }

    public class PermissionNodeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool IsAssigned { get; set; }
        public bool IsSystemPermission { get; set; }
    }

    public class PermissionMatrixDto
    {
        public List<RoleDto> Roles { get; set; } = new List<RoleDto>();
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
        public Dictionary<string, bool> Assignments { get; set; } = new Dictionary<string, bool>();
    }

    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsSystemRole { get; set; }
    }

    public class SecurityContextDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public bool IsSuperAdmin { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
        public bool CanAccessSecurityArea { get; set; }

        public string UserCode { get; set; }

        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }

        public bool IsProductionMode { get; set; }
    }

    public class SecurityRoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
        public List<string> Permissions { get; set; } = new List<string>();
        public int UserCount { get; set; }
    }



    public class RolePermissionSummaryDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public int TotalPermissions { get; set; }
        public int GrantedPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }

        // Calculated properties
        public double PermissionCoverage => TotalPermissions > 0 ?
            (double)GrantedPermissions / TotalPermissions * 100 : 0;

        public string CoverageClass => PermissionCoverage switch
        {
            >= 80 => "text-success",
            >= 50 => "text-warning",
            _ => "text-danger"
        };
    }



    public class PermissionTypeWithPermissionsDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool IsActive { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();

        // Helper properties
        public string IconClass => $"{Icon} text-{Color}";
        public int ActivePermissionsCount => Permissions.Count(p => p.IsActive);
    }


    public class RoleComparisonDto
    {
        public List<RoleSummaryDto> Roles { get; set; } = new List<RoleSummaryDto>();
        public List<PermissionDto> CommonPermissions { get; set; } = new List<PermissionDto>();
        public List<RolePermissionDifference> Differences { get; set; } = new List<RolePermissionDifference>();
    }

    public class RolePermissionDifference
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public List<PermissionDto> UniquePermissions { get; set; } = new List<PermissionDto>();
    }

    public class RolePermissionStatisticsDto
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public int TotalPermissions { get; set; }
        public int GrantedPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public DateTime LastUpdated { get; set; }
        public string LastUpdatedBy { get; set; }
        public List<PermissionTypeUsage> PermissionTypeUsage { get; set; } = new List<PermissionTypeUsage>();
    }

    public class PermissionUsageStatisticsDto
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; }
        public string DisplayName { get; set; }
        public int AssignedToRoles { get; set; }
        public int ActiveAssignments { get; set; }
        public List<string> RoleNames { get; set; } = new List<string>();
        public DateTime FirstAssigned { get; set; }
        public DateTime LastAssigned { get; set; }
    }

    public class PermissionTypeUsage
    {
        public int PermissionTypeId { get; set; }
        public string PermissionTypeName { get; set; }
        public int TotalPermissions { get; set; }
        public int GrantedPermissions { get; set; }
        public double UsagePercentage => TotalPermissions > 0 ? (double)GrantedPermissions / TotalPermissions * 100 : 0;
    }

    public class AuditStatisticsDto
    {
        public int TotalAuditLogs { get; set; }
        public int LogsToday { get; set; }
        public int LogsThisWeek { get; set; }
        public int LogsThisMonth { get; set; }
        public List<AuditActionTypeCount> ActionTypeCounts { get; set; } = new List<AuditActionTypeCount>();
        public List<AuditUserActivityDto> TopUsers { get; set; } = new List<AuditUserActivityDto>();
        public DateTime OldestLogDate { get; set; }
        public DateTime NewestLogDate { get; set; }
    }

    public class AuditActionTypeCount
    {
        public string ActionType { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class AuditActivityDto
    {
        public DateTime Date { get; set; }
        public int ActivityCount { get; set; }
    }

    public class AuditUserActivityDto
    {
        public string UserName { get; set; }
        public int ActivityCount { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class AuditHealthDto
    {
        public bool IsHealthy { get; set; }
        public int TotalLogs { get; set; }
        public int LogsLastHour { get; set; }
        public int LogsLastDay { get; set; }
        public double AverageLogsPerDay { get; set; }
        public DateTime OldestLog { get; set; }
        public long DatabaseSizeBytes { get; set; }
        public List<string> HealthIssues { get; set; } = new List<string>();
    }
}
