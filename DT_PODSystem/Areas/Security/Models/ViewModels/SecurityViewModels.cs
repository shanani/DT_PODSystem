using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace DT_PODSystem.Areas.Security.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Role Permissions tree view management
    /// Add this class to your SecurityViewModels.cs file
    /// </summary>
    public class RolePermissionsViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoleDescription { get; set; } = string.Empty;
        public bool IsSystemRole { get; set; }

        // Permission Tree Data
        public PermissionTreeAssignmentViewModel PermissionTree { get; set; } = new();
        public List<int> SelectedPermissionIds { get; set; } = new();

        // Statistics
        public int TotalPermissions => PermissionTree?.PermissionTypes?.Sum(pt => pt.Permissions?.Count ?? 0) ?? 0;
        public int AssignedPermissions => SelectedPermissionIds?.Count ?? 0;
        public int UnassignedPermissions => TotalPermissions - AssignedPermissions;

        // Helper properties for UI
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
    }
    public class PermissionTreeAssignmentViewModel
    {
        public int? EntityId { get; set; } // Role ID, User ID, etc.
        public string EntityType { get; set; } = "Role"; // "Role", "User", etc.
        public string EntityName { get; set; } = "";

        public List<PermissionTypeAssignmentViewModel> PermissionTypes { get; set; } = new();
        public List<int> SelectedPermissionIds { get; set; } = new();

        // Helper properties
        public int TotalPermissions => PermissionTypes?.Sum(pt => pt.Permissions?.Count ?? 0) ?? 0;
        public int AssignedPermissions => PermissionTypes?.Sum(pt => pt.Permissions?.Count(p => p.IsAssigned) ?? 0) ?? 0;
        public int AvailablePermissions => TotalPermissions - AssignedPermissions;
    }

    public class PermissionTypeAssignmentViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "fas fa-folder";
        public string Color { get; set; } = "primary";
        public bool IsActive { get; set; } = true;

        public List<PermissionAssignmentItemViewModel> Permissions { get; set; } = new();

        // Helper properties
        public int AssignedCount => Permissions?.Count(p => p.IsAssigned) ?? 0;
        public int TotalCount => Permissions?.Count ?? 0;
        public bool HasAssignedPermissions => AssignedCount > 0;
        public string AssignmentSummary => $"{AssignedCount}/{TotalCount}";
    }

    public class PermissionAssignmentItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "fas fa-key";
        public string Color { get; set; } = "primary";

        // Hierarchy properties
        public int? ParentPermissionId { get; set; }
        public int Level { get; set; } = 0;
        public string HierarchyPath { get; set; } = "";

        // Assignment properties
        public bool IsAssigned { get; set; } = false;
        public bool IsSystemPermission { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Display helpers
        public string EffectiveDisplayName => !string.IsNullOrEmpty(DisplayName) ? DisplayName : Name;
        public bool IsRootPermission => ParentPermissionId == null;
        public bool IsChildPermission => Level > 0;
        public string IndentPrefix => Level > 0 ? new string(' ', Level * 4) : "";
    }

    public class TreeNodeViewModel
    {
        public string Id { get; set; } = "";
        public string Text { get; set; } = "";
        public string Type { get; set; } = ""; // "permission_type", "permission", "permission_child"
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public object? Data { get; set; }
        public object? State { get; set; }
        public List<TreeNodeViewModel> Children { get; set; } = new();

        // 🆕 HIERARCHICAL PROPERTIES
        public int Level { get; set; } = 0;
        public bool IsExpanded { get; set; } = true;
        public bool CanHaveChildren { get; set; } = true;
        public string? ParentId { get; set; }
        public string? HierarchyPath { get; set; }

        // 🆕 HIERARCHY ANALYSIS
        public int ChildrenCount => Children?.Count ?? 0;
        public int TotalDescendants => GetTotalDescendants();
        public bool IsLeaf => ChildrenCount == 0;
        public bool IsRoot => string.IsNullOrEmpty(ParentId);

        private int GetTotalDescendants()
        {
            int count = ChildrenCount;
            foreach (var child in Children)
            {
                count += child.GetTotalDescendants();
            }
            return count;
        }

        public object Li_attr { get; set; } = new();



    }

    public class PermissionStatisticsViewModel
    {
        public int TotalPermissionTypes { get; set; }
        public int TotalPermissions { get; set; }
        public int SystemPermissionTypes { get; set; }
        public int SystemPermissions { get; set; }
        public int CustomPermissionTypes { get; set; }
        public int CustomPermissions { get; set; }

        // 🆕 HIERARCHY STATISTICS
        public int RootPermissions { get; set; }
        public int ChildPermissions { get; set; }
        public int MaxDepth { get; set; }
        public int OrphanedPermissions { get; set; }
        public int PermissionsWithChildren { get; set; }
        public int LeafPermissions { get; set; }
        public double AverageChildrenPerPermission { get; set; }
        public int TotalTypes { get; set; }
        public int ActivePermissions { get; set; }
    }

    public class HierarchyAnalyticsViewModel
    {
        public int DeepestLevel { get; set; }
        public int OrphanedCount { get; set; }
        public int ChildlessCount { get; set; }
        public List<PermissionTypeDepthViewModel> TypeDepths { get; set; } = new();
        public List<string> ValidationWarnings { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();

        public bool HasWarnings => ValidationWarnings.Count > 0;
        public bool HasErrors => ValidationErrors.Count > 0;
        public bool IsHealthy => !HasWarnings && !HasErrors;
    }

    // 🆕 PERMISSION TYPE DEPTH VIEW MODEL
    public class PermissionTypeDepthViewModel
    {
        public int PermissionTypeId { get; set; }
        public string PermissionTypeName { get; set; } = "";
        public int MaxDepth { get; set; }
        public int TotalPermissions { get; set; }
        public int RootPermissions { get; set; }
        public int ChildPermissions { get; set; }
    }

    // 🆕 CREATE PERMISSION REQUEST MODEL
    public class CreatePermissionRequest
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(150)]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int PermissionTypeId { get; set; }

        public int? ParentPermissionId { get; set; }

        public bool? CanHaveChildren { get; set; } = true;

        [StringLength(50)]
        public string Scope { get; set; } = "Global";

        [StringLength(50)]
        public string Action { get; set; } = "Read";

        [StringLength(100)]
        public string? Icon { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }
        public int SortOrder { get; set; } = 0;

    }

    // 🆕 MOVE PERMISSION REQUEST MODEL
    public class MovePermissionRequest
    {
        public int PermissionId { get; set; }
        public int? NewParentPermissionId { get; set; }
        public int NewSortOrder { get; set; }
        public string Direction { get; set; } = ""; // "up", "down", "to-parent"
    }

    // 🆕 BULK PERMISSION OPERATION REQUEST
    public class BulkPermissionOperationRequest
    {
        public List<int> PermissionIds { get; set; } = new();
        public string Operation { get; set; } = ""; // "activate", "deactivate", "delete", "move"
        public int? TargetParentId { get; set; }
        public bool IncludeChildren { get; set; } = false;
    }

    // 🆕 PERMISSION HIERARCHY VALIDATION RESULT
    public class PermissionHierarchyValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<OrphanedPermissionViewModel> OrphanedPermissions { get; set; } = new();
        public List<CircularReferenceViewModel> CircularReferences { get; set; } = new();
    }

    // 🆕 ORPHANED PERMISSION VIEW MODEL
    public class OrphanedPermissionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public int PermissionTypeId { get; set; }
        public string PermissionTypeName { get; set; } = "";
        public int? ParentPermissionId { get; set; }
        public string Issue { get; set; } = "";
    }

    // 🆕 CIRCULAR REFERENCE VIEW MODEL
    public class CircularReferenceViewModel
    {
        public List<int> PermissionIds { get; set; } = new();
        public List<string> PermissionNames { get; set; } = new();
        public string CircularPath { get; set; } = "";
    }
    public class PermissionTypeViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(50)]
        public string Icon { get; set; } = "fas fa-folder";

        [StringLength(20)]
        public string Color { get; set; } = "primary";

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsSystemType { get; set; } = false;


        public int PermissionsCount { get; set; }
        public int TotalPermissions { get; set; }

        public bool CanDelete { get; set; } = true;
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        // Helper properties
        public string IconClass => !string.IsNullOrEmpty(Icon) ? Icon : "fas fa-cog";
        public string ColorClass => !string.IsNullOrEmpty(Color) ? Color : "primary";
        public double UtilizationPercentage => TotalPermissions > 0 ?
            Math.Round((double)PermissionsCount / TotalPermissions * 100, 1) : 0;


    }

    public class PermissionFormViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Permission Name")]
        public string Name { get; set; } = "";

        [StringLength(150)]
        [Display(Name = "Display Name")]
        public string? DisplayName { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Permission Type")]
        public int PermissionTypeId { get; set; }

        // 🆕 HIERARCHICAL PROPERTIES
        [Display(Name = "Parent Permission")]
        public int? ParentPermissionId { get; set; }

        [Required]
        [Display(Name = "Scope")]
        public string Scope { get; set; } = "Global";

        [Required]
        [Display(Name = "Action")]
        public string Action { get; set; } = "Read";

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is System Permission")]
        public bool IsSystemPermission { get; set; } = false;

        [Display(Name = "Can Have Children")]
        public bool CanHaveChildren { get; set; } = true;

        [StringLength(50)]
        [Display(Name = "Icon")]
        public string Icon { get; set; } = "fas fa-key";

        [StringLength(20)]
        [Display(Name = "Color")]
        public string Color { get; set; } = "secondary";

        // 🆕 DROPDOWN DATA
        public List<SelectListItem> PermissionTypes { get; set; } = new();
        public List<SelectListItem> ParentPermissions { get; set; } = new();

        // Helper properties
        public bool IsEditing => Id > 0;
        public string FormTitle => IsEditing ? "Edit Permission" : "Create Permission";
        public string SubmitButtonText => IsEditing ? "Update" : "Create";
        public string SubmitButtonClass => IsEditing ? "btn-warning" : "btn-success";
        public string PermissionTypeName { get; set; } = "";

        // 🆕 HIERARCHY DISPLAY
        public string ParentPermissionName { get; set; } = "";
        public int Level { get; set; } = 0;
        public string HierarchyPath { get; set; } = "";
        public string FullHierarchyName { get; set; } = "";

        // 🆕 VALIDATION HELPERS
        public bool CanSelectParent => !IsSystemPermission;
        public string LevelIndicator => Level > 0 ? new string('→', Level) + " " : "";
    }


    public class PermissionTreeViewModel
    {
        public string Title { get; set; } = "Permission Management";
        public string Description { get; set; } = "";
        public List<TreeNodeViewModel> TreeData { get; set; } = new();
        public PermissionStatisticsViewModel Statistics { get; set; } = new();

        // 🆕 HIERARCHY ANALYTICS
        public HierarchyAnalyticsViewModel Analytics { get; set; } = new();
        public bool IsProductionMode { get; internal set; }
    }


    public class PermissionTypeFormViewModel
    {
        // Make Id nullable to handle both create (null/0) and edit (actual ID) scenarios
        public int? Id { get; set; }

        [Required(ErrorMessage = "Type Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Type Name must be between 2 and 100 characters")]
        [Display(Name = "Type Name")]
        public string Name { get; set; } = "";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(50, ErrorMessage = "Icon class cannot exceed 50 characters")]
        [Display(Name = "Icon")]
        public string? Icon { get; set; } = "fas fa-folder";

        [StringLength(20, ErrorMessage = "Color cannot exceed 20 characters")]
        [Display(Name = "Color")]
        public string? Color { get; set; } = "primary";

        [Display(Name = "Sort Order")]
        public int SortOrder { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Is System Type")]
        public bool IsSystemType { get; set; } = false;
    }



    // Super Admin Dashboard ViewModel
    public class SuperAdminDashboardViewModel
    {
        public bool IsProductionMode { get; set; }
        public SecurityStatisticsDto SystemStatistics { get; set; } = new SecurityStatisticsDto();
        public List<PermissionTypeDto> PermissionTypes { get; set; } = new List<PermissionTypeDto>();
        public List<SecurityAuditDto> RecentSystemActions { get; set; } = new List<SecurityAuditDto>();

        // Quick actions permissions
        public bool CanManagePermissionTypes { get; set; }
        public bool CanManagePermissions { get; set; }
        public bool CanAccessSystemSettings { get; set; }
        public bool CanViewAuditLogs { get; set; }

        // System health indicators
        public int DatabasePermissionTypes { get; set; }
        public int DatabasePermissions { get; set; }
        public int DatabaseRolePermissions { get; set; }

        public int PendingMigrations { get; set; }
    }


    /// <summary>
    /// ViewModel for Security Users Index page
    /// </summary>
    public class SecurityUsersIndexViewModel
    {
        public SecurityUserStatisticsViewModel Statistics { get; set; } = new SecurityUserStatisticsViewModel();
        public List<SelectListItem> RoleFilters { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> StatusFilters { get; set; } = new List<SelectListItem>();
    }

    /// <summary>
    /// Statistics for Security Users
    /// </summary>
    public class SecurityUserStatisticsViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int InactiveUsers { get; set; }
        public int UsersWithRoles { get; set; }

        // Calculated properties
        public double RoleCoveragePercentage => TotalUsers > 0 ? (UsersWithRoles * 100.0 / TotalUsers) : 0;
        public double ActiveUserPercentage => TotalUsers > 0 ? (ActiveUsers * 100.0 / TotalUsers) : 0;

    }

    /// <summary>
    /// ViewModel for Security User Details page
    /// </summary>
    public class SecurityUserDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "User Code")]
        public string Code { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        // 🔥 FIX: Added missing Photo properties
        [Display(Name = "Photo")]
        public byte[] Photo { get; set; }

        [Display(Name = "Photo Base64")]
        public string Photo_base64 { get; set; }

        [Display(Name = "Has Photo")]
        public bool HasPhoto { get; set; }

        [Display(Name = "Photo Data URL")]
        public string PhotoDataUrl { get; set; }

        [Display(Name = "Initials")]
        public string Initials { get; set; }

        // Navigation Properties
        public List<SecurityUserRole> UserRoles { get; set; } = new List<SecurityUserRole>();

        // Helper Properties
        public string FullName => $"{FirstName} {LastName}";
        public string StatusDisplay => !IsActive ? "Locked" : (IsActive ? "Active" : "Inactive");
        public string StatusBadgeClass => !IsActive ? "warning" : (IsActive ? "success" : "secondary");
        public int DaysInSystem => (DateTime.UtcNow.AddHours(3) - CreatedAt).Days;
        public int? DaysSinceLastLogin => LastLoginAt.HasValue ? (DateTime.UtcNow.AddHours(3) - LastLoginAt.Value).Days : null;
        public int AssignedRolesCount => UserRoles?.Count ?? 0;
    }

    /// <summary>
    /// ViewModel for User Export operations
    /// </summary>
    public class SecurityUserExportViewModel
    {
        public string Code { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Status { get; set; }
        public string Roles { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// ViewModel for Bulk User Operations
    /// </summary>
    public class SecurityUserBulkOperationViewModel
    {
        public List<int> SelectedUserIds { get; set; } = new List<int>();
        public string Operation { get; set; } // activate, deactivate, lock, unlock, delete
        public string Reason { get; set; }
        public List<int> RoleIds { get; set; } = new List<int>(); // For bulk role assignment
        public bool ConfirmOperation { get; set; }
    }

    /// <summary>
    /// ViewModel for User Search and Filtering
    /// </summary>
    public class SecurityUserSearchViewModel
    {
        public string SearchTerm { get; set; }
        public string RoleFilter { get; set; }
        public string StatusFilter { get; set; } // active, inactive, locked, all
        public string DepartmentFilter { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public DateTime? LastLoginFrom { get; set; }
        public DateTime? LastLoginTo { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string SortBy { get; set; } = "FirstName";
        public string SortDirection { get; set; } = "asc";
    }

    /// <summary>
    /// ViewModel for User Role Assignment
    /// </summary>
    public class SecurityUserRoleAssignmentViewModel
    {
        public int UserId { get; set; }
        public string UserCode { get; set; }
        public string UserFullName { get; set; }
        public List<SecurityUserRoleViewModel> CurrentRoles { get; set; } = new List<SecurityUserRoleViewModel>();
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
        public List<int> SelectedRoleIds { get; set; } = new List<int>();
        public string AssignmentReason { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying User Roles
    /// </summary>
    public class SecurityUserRoleViewModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public string AssignedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string RevokedBy { get; set; }

        // Helper properties
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Revoked";
        public int DaysAssigned => (DateTime.UtcNow.AddHours(3) - AssignedAt).Days;
    }

    /// <summary>
    /// ViewModel for User Activity and Audit
    /// </summary>
    public class SecurityUserActivityViewModel
    {
        public int UserId { get; set; }
        public string UserCode { get; set; }
        public string UserFullName { get; set; }
        public List<SecurityUserAuditViewModel> ActivityLog { get; set; } = new List<SecurityUserAuditViewModel>();
        public DateTime? LastLoginDate { get; set; }
        public int LoginCount { get; set; }
        public List<SecurityUserRoleChangeViewModel> RoleChanges { get; set; } = new List<SecurityUserRoleChangeViewModel>();
    }

    /// <summary>
    /// ViewModel for User Audit entries
    /// </summary>
    public class SecurityUserAuditViewModel
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        // Helper properties
        public string ActionBadgeClass => Action switch
        {
            "Created" => "success",
            "Updated" => "info",
            "Deleted" => "danger",
            "Locked" => "warning",
            "Unlocked" => "success",
            "Login" => "primary",
            "Logout" => "secondary",
            _ => "light"
        };

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow.AddHours(3) - Timestamp;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalDays < 30) return $"{(int)timeSpan.TotalDays} days ago";
                return Timestamp.ToString("yyyy-MM-dd");
            }
        }
    }

    /// <summary>
    /// ViewModel for User Role Changes
    /// </summary>
    public class SecurityUserRoleChangeViewModel
    {
        public int Id { get; set; }
        public string Action { get; set; } // assigned, revoked
        public string RoleName { get; set; }
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; }
        public string Reason { get; set; }

        // Helper properties
        public string ActionBadgeClass => Action switch
        {
            "assigned" => "success",
            "revoked" => "warning",
            _ => "secondary"
        };

        public string ActionText => Action switch
        {
            "assigned" => "Role Assigned",
            "revoked" => "Role Revoked",
            _ => Action
        };
    }

    /// <summary>
    /// ViewModel for User Import operations
    /// </summary>
    public class SecurityUserImportViewModel
    {
        public List<SecurityUserImportRowViewModel> Users { get; set; } = new List<SecurityUserImportRowViewModel>();
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public bool HasErrors => InvalidRows > 0;
        public string ImportStatus { get; set; }
    }

    /// <summary>
    /// ViewModel for individual User Import row
    /// </summary>
    public class SecurityUserImportRowViewModel
    {
        public int RowNumber { get; set; }
        public string Code { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Roles { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string Status { get; set; } = "Pending";
    }

    /// <summary>
    /// ViewModel for User Dashboard/Summary
    /// </summary>
    public class SecurityUserDashboardViewModel
    {
        public SecurityUserStatisticsViewModel Statistics { get; set; } = new SecurityUserStatisticsViewModel();
        public List<SecurityUserSummaryViewModel> RecentUsers { get; set; } = new List<SecurityUserSummaryViewModel>();
        public List<SecurityUserActivityViewModel> RecentActivity { get; set; } = new List<SecurityUserActivityViewModel>();
        public List<SecurityUserRoleDistributionViewModel> RoleDistribution { get; set; } = new List<SecurityUserRoleDistributionViewModel>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow.AddHours(3);
    }

    /// <summary>
    /// ViewModel for User Summary display
    /// </summary>
    public class SecurityUserSummaryViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int RoleCount { get; set; }

        // Helper properties
        public string StatusBadge => IsLocked ? "warning" : (IsActive ? "success" : "secondary");
        public string StatusText => IsLocked ? "Locked" : (IsActive ? "Active" : "Inactive");
    }

    /// <summary>
    /// ViewModel for Role Distribution analytics
    /// </summary>
    public class SecurityUserRoleDistributionViewModel
    {
        public string RoleName { get; set; }
        public int UserCount { get; set; }
        public double Percentage { get; set; }
        public string Color { get; set; }
    }

    /// <summary>
    /// ViewModel for DataTables response
    /// </summary>
    public class SecurityUsersDataTableViewModel
    {
        public int Draw { get; set; }
        public int RecordsTotal { get; set; }
        public int RecordsFiltered { get; set; }
        public List<SecurityUserDataRowViewModel> Data { get; set; } = new List<SecurityUserDataRowViewModel>();
    }

    /// <summary>
    /// ViewModel for DataTables row data
    /// </summary>
    public class SecurityUserDataRowViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Roles { get; set; }
        public bool IsActive { get; set; }
        public bool IsLocked { get; set; }
        public string LastLoginAt { get; set; }
        public string StatusBadge { get; set; }
        public string Actions { get; set; }

        // Additional properties for sorting/filtering
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }
    }

    /// <summary>
    /// ViewModel for User Quick Actions
    /// </summary>
    public class SecurityUserQuickActionViewModel
    {
        public int UserId { get; set; }
        public string Action { get; set; } // lock, unlock, activate, deactivate, delete
        public string Reason { get; set; }
        public bool ConfirmAction { get; set; }
    }

    /// <summary>
    /// ViewModel for User Password Management
    /// </summary>
    public class SecurityUserPasswordViewModel
    {
        public int UserId { get; set; }
        public string UserCode { get; set; }
        public string UserFullName { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Password confirmation does not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Force Password Change on Next Login")]
        public bool ForceChangeOnNextLogin { get; set; } = true;

        [Display(Name = "Send Password via Email")]
        public bool SendPasswordByEmail { get; set; } = false;

        public string Reason { get; set; }
    }

    /// <summary>
    /// Response ViewModel for AJAX operations
    /// </summary>
    public class SecurityUserActionResponseViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string RedirectUrl { get; set; }
    }

    public class PermissionGroupViewModel
    {
        public int PermissionTypeId { get; set; }
        public string PermissionTypeName { get; set; }
        public string PermissionTypeDescription { get; set; }
        public string PermissionTypeIcon { get; set; }
        public string PermissionTypeColor { get; set; }
        public List<RolePermissionViewModel> Permissions { get; set; } = new List<RolePermissionViewModel>();
    }

    // And the RolePermissionViewModel if it doesn't exist:
    public class RolePermissionViewModel
    {
        public int Id { get; set; }
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        public bool IsGranted { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        // Navigation Properties
        public string RoleName { get; set; }
        public string PermissionName { get; set; }
        public string PermissionDisplayName { get; set; }
        public string PermissionDescription { get; set; }
        public string PermissionTypeName { get; set; }
        public string PermissionTypeColor { get; set; }
        public string PermissionIcon { get; set; }

        // Helper properties
        public string StatusBadge => IsGranted && IsActive ? "success" : "danger";
        public string StatusText => IsGranted && IsActive ? "Granted" : "Denied";
        public string PermissionTypeClass => !string.IsNullOrEmpty(PermissionTypeColor) ?
            PermissionTypeColor : "secondary";

    }


    public class RolePermissionManagementViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public List<PermissionGroupViewModel> PermissionGroups { get; set; } = new List<PermissionGroupViewModel>();
        public List<RoleSummaryDto> Roles { get; set; } = new List<RoleSummaryDto>();
        public string SearchQuery { get; set; }
        public string SelectedStatus { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        // Quick stats
        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int RolesWithPermissions { get; set; }

        // Permissions
        public bool CanAssignPermissions { get; set; }
        public bool CanRevokePermissions { get; set; }
        public bool CanBulkUpdate { get; set; }
    }

    #region DataTable Request Model





    public class PermissionTypeDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-cog";
        public string Color { get; set; } = "primary";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemType { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;

        public List<PermissionSummaryDto> AssociatedPermissions { get; set; } = new();

        // Helper properties
        public string IconClass => !string.IsNullOrEmpty(Icon) ? Icon : "fas fa-cog";
        public string ColorClass => !string.IsNullOrEmpty(Color) ? Color : "primary";
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeBadge => IsSystemType ? "warning" : "info";
        public string TypeText => IsSystemType ? "System Type" : "Custom Type";
    }

    public class PermissionTypeDeleteViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-cog";
        public string Color { get; set; } = "primary";
        public bool IsSystemType { get; set; }
        public int PermissionsCount { get; set; }
        public bool CanDelete { get; set; }
        public List<string> RelatedPermissions { get; set; } = new();

        // Helper properties
        public string IconClass => !string.IsNullOrEmpty(Icon) ? Icon : "fas fa-cog";
        public string ColorClass => !string.IsNullOrEmpty(Color) ? Color : "primary";
        public string DeleteWarning => !CanDelete
            ? (IsSystemType ? "System permission types cannot be deleted."
                            : "This permission type has associated permissions and cannot be deleted.")
            : string.Empty;
    }






    public class PermissionSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fas fa-key";
        public string Color { get; set; } = "primary";
        public bool IsActive { get; set; }
        public bool IsSystemPermission { get; set; }
        public int AssignedToRolesCount { get; set; }

        // Helper properties
        public string IconClass => !string.IsNullOrEmpty(Icon) ? Icon : "fas fa-key";
        public string ColorClass => !string.IsNullOrEmpty(Color) ? Color : "primary";
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeBadge => IsSystemPermission ? "warning" : "info";
        public string TypeText => IsSystemPermission ? "System" : "Custom";
    }


    // Permission Type Index ViewModel
    public class PermissionTypeIndexViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int PermissionCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatedBy { get; set; }
        public string UpdatedBy { get; set; }

        // Helper properties
        public string IconClass => !string.IsNullOrEmpty(Icon) ? Icon : "fas fa-cog";
        public string ColorClass => !string.IsNullOrEmpty(Color) ? Color : "primary";
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
    }



    // Super Admin ViewModel
    public class SuperAdminViewModel
    {
        // System Information
        public string ApplicationVersion { get; set; }
        public string EnvironmentName { get; set; }
        public bool IsProductionMode { get; set; }
        public DateTime SystemStartTime { get; set; }
        public TimeSpan SystemUptime { get; set; }

        // Database Statistics
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int TotalPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public int TotalPermissionTypes { get; set; }
        public int ActivePermissionTypes { get; set; }

        // Security Statistics
        public int SuperAdminUsers { get; set; }
        public int SystemPermissions { get; set; }
        public int CustomPermissions { get; set; }
        public int RolePermissionAssignments { get; set; }
        public int UserRoleAssignments { get; set; }

        // Recent Activity
        public List<SecurityAuditSummary> RecentSecurityActions { get; set; } = new List<SecurityAuditSummary>();
        public List<UserActivitySummary> RecentUserActivity { get; set; } = new List<UserActivitySummary>();

        // System Health
        public bool DatabaseConnectionHealthy { get; set; }
        public bool SecurityModuleHealthy { get; set; }
        public bool IdentitySystemHealthy { get; set; }

        // Permissions
        public bool CanManagePermissionTypes { get; set; }
        public bool CanManagePermissions { get; set; }
        public bool CanManageRoles { get; set; }
        public bool CanManageUsers { get; set; }
        public bool CanAccessSystemSettings { get; set; }
        public bool CanViewAuditLogs { get; set; }

        // Helper Properties
        public string EnvironmentBadgeClass => IsProductionMode ? "danger" : "warning";
        public string EnvironmentBadgeText => IsProductionMode ? "PRODUCTION" : "DEVELOPMENT";
        public double UserActivationRate => TotalUsers > 0 ? (double)ActiveUsers / TotalUsers * 100 : 0;
        public double RoleActivationRate => TotalRoles > 0 ? (double)ActiveRoles / TotalRoles * 100 : 0;
        public double PermissionActivationRate => TotalPermissions > 0 ? (double)ActivePermissions / TotalPermissions * 100 : 0;
    }

    // Supporting classes for SuperAdminViewModel
    public class SecurityAuditSummary
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }

        // Helper properties
        public string ActionBadgeClass => Action switch
        {
            "Create" => "success",
            "Update" => "warning",
            "Delete" => "danger",
            "Login" => "info",
            "Logout" => "secondary",
            _ => "primary"
        };

        public string TimeAgo
        {
            get
            {
                var diff = DateTime.UtcNow.AddHours(3) - Timestamp;
                return diff.TotalMinutes < 1 ? "Just now" :
                       diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago" :
                       diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago" :
                       $"{(int)diff.TotalDays}d ago";
            }
        }
    }

    public class UserActivitySummary
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public DateTime LastLoginAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public int LoginCount { get; set; }
        public int RoleCount { get; set; }
        public int PermissionCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsOnline { get; set; }

        // Helper properties
        public string StatusBadgeClass => IsActive ? (IsOnline ? "success" : "info") : "secondary";
        public string StatusText => IsActive ? (IsOnline ? "Online" : "Active") : "Inactive";

        public string LastSeenText
        {
            get
            {
                var diff = DateTime.UtcNow.AddHours(3) - LastActivityAt;
                return IsOnline ? "Online now" :
                       diff.TotalMinutes < 5 ? "Just now" :
                       diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago" :
                       diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago" :
                       diff.TotalDays < 7 ? $"{(int)diff.TotalDays}d ago" :
                       LastActivityAt.ToString("MMM dd");
            }
        }
    }

    // Role Assignment ViewModel for bulk operations
    public class RoleAssignmentViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public List<int> SelectedPermissionIds { get; set; } = new List<int>();
        public List<PermissionSelectionItem> AvailablePermissions { get; set; } = new List<PermissionSelectionItem>();
        public List<PermissionSelectionItem> CurrentPermissions { get; set; } = new List<PermissionSelectionItem>();

        // Bulk operation settings
        public string BulkAction { get; set; } // "grant", "revoke", "replace"
        public string Notes { get; set; }
        public bool NotifyUsers { get; set; }
    }

    public class PermissionSelectionItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string PermissionTypeName { get; set; }
        public string PermissionTypeColor { get; set; }
        public string Icon { get; set; }
        public bool IsSelected { get; set; }
        public bool IsSystemPermission { get; set; }
        public bool IsCurrentlyGranted { get; set; }

        // Helper properties
        public string BadgeClass => PermissionTypeColor ?? "primary";
        public string IconClass => Icon ?? "fas fa-key";
        public string SystemBadge => IsSystemPermission ? "System" : "Custom";
        public string SystemBadgeClass => IsSystemPermission ? "warning" : "info";
    }



    public class DataTableRequest
    {
        public int Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string SearchValue { get; set; }
        public string PermissionTypeFilter { get; set; }
        public string StatusFilter { get; set; }
        public string SystemTypeFilter { get; set; }
        public List<DataTableColumn> Columns { get; set; }
        public List<DataTableOrder> Order { get; set; }
    }

    public class DataTableColumn
    {
        public string Data { get; set; }
        public string Name { get; set; }
        public bool Searchable { get; set; }
        public bool Orderable { get; set; }
        public DataTableSearch Search { get; set; }
    }

    public class DataTableOrder
    {
        public int Column { get; set; }
        public string Dir { get; set; }
    }

    public class DataTableSearch
    {
        public string Value { get; set; }
        public bool Regex { get; set; }
    }

    #endregion


    public class PermissionDetailsViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Permission Name")]
        public string Name { get; set; }

        [Display(Name = "System Name")]
        public string SystemName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Icon")]
        public string Icon { get; set; }

        [Display(Name = "Color")]
        public string Color { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "System Permission")]
        public bool IsSystemPermission { get; set; }

        // Permission Type Information
        public PermissionTypeViewModel PermissionType { get; set; }

        // Audit Information
        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        // Role Assignments
        public IEnumerable<AssignedRoleViewModel> AssignedRoles { get; set; }

        // Statistics
        public int TotalUsersWithAccess { get; set; }

        // Related Permissions (same type)
        public IEnumerable<RelatedPermissionViewModel> RelatedPermissions { get; set; }

        // Constructor
        public PermissionDetailsViewModel()
        {
            AssignedRoles = new List<AssignedRoleViewModel>();
            RelatedPermissions = new List<RelatedPermissionViewModel>();
        }
    }

    public class AssignedRoleViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public int UsersCount { get; set; }
        public bool IsActive { get; set; }
        public DateTime AssignedAt { get; set; }
        public string AssignedBy { get; set; }
    }

    public class RelatedPermissionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool IsActive { get; set; }
    }


    public class PermissionViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Permission Name")]
        public string Name { get; set; }

        [Display(Name = "System Name")]
        public string SystemName { get; set; }

        [Display(Name = "Description")]
        public string Description { get; set; }

        [Display(Name = "Icon")]
        public string Icon { get; set; }

        [Display(Name = "Color")]
        public string Color { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "System Permission")]
        public bool IsSystemPermission { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; }

        // Permission Type Information
        public PermissionTypeViewModel PermissionType { get; set; }

        // Statistics
        public int AssignedToRolesCount { get; set; }
        public int UsersWithAccessCount { get; set; }

        // Constructor
        public PermissionViewModel()
        {
            Icon = "fas fa-key";
            Color = "primary";
            IsActive = true;
            IsSystemPermission = false;
            AssignedToRolesCount = 0;
            UsersWithAccessCount = 0;
        }
    }



    public class SecurityRoleManagementViewModel
    {

        public int Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(100, ErrorMessage = "Role name cannot exceed 100 characters")]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = "";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        public string Description { get; set; } = "";

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "System Role")]
        public bool IsSystemRole { get; set; } = false;

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string CreatedBy { get; set; } = "";
        public string UpdatedBy { get; set; } = "";

        // Statistics
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }

        // 🆕 ADD: Permission tree for assignment
        public PermissionTreeAssignmentViewModel PermissionTree { get; set; } = new();
        public List<int> SelectedPermissionIds { get; set; } = new();

        // Helper properties
        public bool IsEditing => Id > 0;
        public string FormTitle => IsEditing ? $"Edit Role - {Name}" : "Create New Role";
        public string SubmitButtonText => IsEditing ? "Update Role" : "Create Role";
        public string SubmitButtonClass => IsEditing ? "btn-warning" : "btn-success";
    }



    /// <summary>
    /// ViewModel for Security User Management (Create/Edit) - UPDATED WITH AUDIT PROPERTIES
    /// </summary>
    public class SecurityUserManagementViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "User code is required")]
        [StringLength(50, ErrorMessage = "User code cannot exceed 50 characters")]
        [Display(Name = "User Code")]
        public string Code { get; set; } = "";

        [Required(ErrorMessage = "First name is required")]
        [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "";

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "";

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(200, ErrorMessage = "Email cannot exceed 200 characters")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [StringLength(100, ErrorMessage = "Department cannot exceed 100 characters")]
        [Display(Name = "Department")]
        public string Department { get; set; } = "";

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;

        // ADD MISSING AUDIT PROPERTIES
        [Display(Name = "Created Date")]
        public DateTime CreatedAt { get; set; }

        [Display(Name = "Updated Date")]
        public DateTime? UpdatedAt { get; set; }

        [Display(Name = "Created By")]
        public string CreatedBy { get; set; } = "";

        [Display(Name = "Updated By")]
        public string UpdatedBy { get; set; } = "";

        [Display(Name = "Last Login")]
        public DateTime? LastLoginAt { get; set; }

        // Role Management
        public List<int> SelectedRoleIds { get; set; } = new List<int>();
        public List<SelectListItem> AvailableRoles { get; set; } = new List<SelectListItem>();
        public List<SecurityUserRole> UserRoles { get; set; } = new List<SecurityUserRole>();

        // Helper Properties
        public string FullName => $"{FirstName} {LastName}";
        public string StatusBadge => IsActive ? "success" : "secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";

        // Helper properties for editing
        public bool IsEditing => Id > 0;
        public string FormTitle => IsEditing ? $"Edit User - {FullName}" : "Create New User";
        public string SubmitButtonText => IsEditing ? "Update User" : "Create User";
        public string SubmitButtonClass => IsEditing ? "btn-warning" : "btn-primary";
    }

    public class SecurityDashboardViewModel
    {
        public List<RoleDistributionDto> RoleDistribution { get; set; } = new List<RoleDistributionDto>();
        public List<SecurityAuditDto> RecentActivities { get; set; } = new List<SecurityAuditDto>();
        public SecurityStatisticsViewModel Statistics { get; set; } = new SecurityStatisticsViewModel();
        public List<PermissionTypeViewModel> PermissionTypes { get; set; } = new List<PermissionTypeViewModel>();
        public List<ActivityViewModel> RecentActivity { get; set; } = new List<ActivityViewModel>();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow.AddHours(3);


        public List<RolePermissionSummaryDto> RolePermissions { get; set; } = new List<RolePermissionSummaryDto>();


        // User access information
        public bool CanManagePermissionTypes { get; set; }
        public bool CanManagePermissions { get; set; }
        public bool CanManageRoles { get; set; }
        public bool CanManageUsers { get; set; }
        public bool IsSuperAdmin { get; set; }
        public bool IsProductionMode { get; set; }
    }


    public class SecurityStatisticsViewModel
    {
        public int NewUsersThisMonth { get; set; }
        public int PermissionTypes { get; set; }
        public int ActiveUsers { get; set; }

        // Helper properties for UI
        public string HealthStatus =>
            RolesUtilization >= 90 ? "Excellent" :
            RolesUtilization >= 75 ? "Good" :
            RolesUtilization >= 60 ? "Fair" : "Poor";

        public string HealthStatusColor =>
            RolesUtilization >= 90 ? "#28a745" :
            RolesUtilization >= 75 ? "#17a2b8" :
            RolesUtilization >= 60 ? "#ffc107" : "#dc3545";

        public int TotalPermissionTypes { get; set; }
        public int ActivePermissionTypes { get; set; }
        public int TotalPermissions { get; set; }
        public int ActivePermissions { get; set; }
        public int TotalRoles { get; set; }
        public int ActiveRoles { get; set; }
        public int TotalUsers { get; set; }
        public int UsersWithRoles { get; set; }

        // Calculated properties
        public double PermissionTypesUtilization { get; set; }
        public double PermissionsUtilization { get; set; }
        public double RolesUtilization { get; set; }
        public double UsersWithRolesPercentage { get; set; }
    }



    public class ActivityViewModel
    {
        public string Date { get; set; }
        public string Activity { get; set; }
        public string Type { get; set; }
        public string User { get; set; }
        public DateTime Timestamp { get; set; }

        // Helper properties
        public string TypeBadgeClass => Type switch
        {
            "Permission" => "bg-success",
            "Type" => "badge-info",
            "Role" => "bg-warning",
            _ => "bg-secondary"
        };

        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow.AddHours(3) - Timestamp;
                if (timeSpan.TotalMinutes < 1) return "Just now";
                if (timeSpan.TotalHours < 1) return $"{(int)timeSpan.TotalMinutes} minutes ago";
                if (timeSpan.TotalDays < 1) return $"{(int)timeSpan.TotalHours} hours ago";
                return $"{(int)timeSpan.TotalDays} days ago";
            }
        }
    }


    public class PermissionTypeGroupViewModel
    {
        public PermissionTypeDto PermissionType { get; set; } = new();
        public List<PermissionAssignmentViewModel> Permissions { get; set; } = new List<PermissionAssignmentViewModel>();

        // Helper properties
        public int AssignedCount => Permissions?.Count(p => p.IsAssigned) ?? 0;
        public int TotalCount => Permissions?.Count ?? 0;
        public bool HasAssignedPermissions => AssignedCount > 0;
        public string AssignmentSummary => $"{AssignedCount}/{TotalCount}";
        public double AssignmentPercentage => TotalCount > 0 ? Math.Round((double)AssignedCount / TotalCount * 100, 1) : 0;


    }

    public class PermissionAssignmentViewModel
    {
        public int PermissionId { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public bool IsAssigned { get; set; }
    }

    public class RolePermissionAssignmentViewModel
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDescription { get; set; }
        public List<PermissionTypeGroupViewModel> Permissions { get; set; } = new List<PermissionTypeGroupViewModel>();


        public List<PermissionTypeWithPermissionsDto> PermissionTypes { get; set; } = new List<PermissionTypeWithPermissionsDto>();
        public List<int> GrantedPermissionIds { get; set; } = new List<int>();

        // Search and filter
        public string SearchQuery { get; set; }
        public int? SelectedPermissionTypeId { get; set; }

        // Actions
        public string Action { get; set; } // "grant", "revoke", "bulk"
        public List<int> SelectedPermissionIds { get; set; } = new List<int>();

        // Statistics
        public int TotalPermissions { get; set; }
        public int GrantedPermissions { get; set; }
        public int PendingPermissions { get; set; }
    }


    public class RoleManagementViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public bool IsSystemRole { get; set; }
        public int UsersCount { get; set; }
        public int PermissionsCount { get; set; }

        // Helper properties for display
        public string StatusBadgeClass => IsActive ? "bg-success" : "bg-secondary";
        public string StatusText => IsActive ? "Active" : "Inactive";
        public string TypeBadgeClass => IsSystemRole ? "bg-warning" : "badge-info";
        public string TypeText => IsSystemRole ? "System" : "Custom";
    }





    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }





    public class PermissionTypeListViewModel
    {
        public List<PermissionTypeDto> PermissionTypes { get; set; } = new List<PermissionTypeDto>();
        public string SearchQuery { get; set; }
        public string SelectedStatus { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortOrder { get; set; } = "asc";

        // Permissions
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }



    public class PermissionListViewModel
    {
        public List<PermissionDto> Permissions { get; set; } = new List<PermissionDto>();
        public string SearchQuery { get; set; }
        public int? SelectedPermissionTypeId { get; set; }
        public string SelectedScope { get; set; } = "all";
        public string SelectedAction { get; set; } = "all";
        public string SelectedStatus { get; set; } = "all";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public string SortBy { get; set; } = "DisplayName";
        public string SortOrder { get; set; } = "asc";

        // Filter options
        public List<PermissionTypeDto> AvailablePermissionTypes { get; set; } = new List<PermissionTypeDto>();
        public List<SelectListItem> ScopeOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ActionOptions { get; set; } = new List<SelectListItem>();

        // Permissions
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }





    public class BulkPermissionUpdateViewModel
    {
        public List<int> RoleIds { get; set; } = new List<int>();
        public List<int> PermissionIds { get; set; } = new List<int>();
        public string Action { get; set; } // "grant" or "revoke"
        public string Notes { get; set; }

        // For display
        public List<RoleSummaryDto> SelectedRoles { get; set; } = new List<RoleSummaryDto>();
        public List<PermissionDto> SelectedPermissions { get; set; } = new List<PermissionDto>();
    }




    public class SystemSettingsViewModel
    {
        [Display(Name = "Enable Audit Logging")]
        public bool EnableAuditLogging { get; set; } = true;

        [Display(Name = "Auto-Approve Role Assignments")]
        public bool AutoApproveRoleAssignments { get; set; } = false;

        [Display(Name = "Require Approval for Permission Changes")]
        public bool RequireApprovalForPermissionChanges { get; set; } = true;

        [Display(Name = "Maximum Failed Login Attempts")]
        public int MaxFailedLoginAttempts { get; set; } = 5;

        [Display(Name = "Lockout Duration (minutes)")]
        public int LockoutDurationMinutes { get; set; } = 30;

        [Display(Name = "Default Session Timeout (minutes)")]
        public int DefaultSessionTimeoutMinutes { get; set; } = 60;

        [Display(Name = "Require Strong Passwords")]
        public bool RequireStrongPasswords { get; set; } = true;

        [Display(Name = "Enable Two-Factor Authentication")]
        public bool EnableTwoFactorAuth { get; set; } = false;

        // Email notifications
        [Display(Name = "Send Email on Role Assignment")]
        public bool SendEmailOnRoleAssignment { get; set; } = true;

        [Display(Name = "Send Email on Permission Change")]
        public bool SendEmailOnPermissionChange { get; set; } = true;

        [Display(Name = "Admin Email for Notifications")]
        [EmailAddress]
        public string AdminNotificationEmail { get; set; }
    }
}

