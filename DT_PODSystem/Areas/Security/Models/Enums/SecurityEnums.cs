
using System.ComponentModel.DataAnnotations;

namespace DT_PODSystem.Areas.Security.Models.Enums
{
    public enum PermissionScope
    {
        Global = 1,
        Department = 2,
        Personal = 3,
        Organization = 4,
        Project = 5
    }

    /// <summary>
    /// Defines the action type of a permission
    /// </summary>
    public enum PermissionAction
    {
        [Display(Name = "Create")]
        Create = 0,

        [Display(Name = "Read")]
        Read = 1,

        [Display(Name = "Update")]
        Update = 2,

        [Display(Name = "Delete")]
        Delete = 3,

        [Display(Name = "Execute")]
        Execute = 4,

        [Display(Name = "Approve")]
        Approve = 5,

        [Display(Name = "Manage")]
        Manage = 6,

        [Display(Name = "Export")]
        Export = 7,


        [Display(Name = "Import")]
        Import = 8

    }

    /// <summary>
    /// Types of security audit actions
    /// </summary>
    public enum AuditActionType
    {
        // Permission Management
        PermissionTypeCreated = 1,
        PermissionTypeUpdated = 2,
        PermissionTypeDeleted = 3,
        PermissionCreated = 4,
        PermissionUpdated = 5,
        PermissionDeleted = 6,

        // Role Management
        RoleCreated = 10,
        RoleUpdated = 11,
        RoleDeleted = 12,
        RoleAssigned = 13,
        RoleRevoked = 14,

        // User Management
        UserCreated = 20,
        UserUpdated = 21,
        UserDeleted = 22,
        UserLocked = 23,
        UserUnlocked = 24,

        // Permission Assignment
        PermissionGranted = 30,
        PermissionRevoked = 31,
        BulkPermissionUpdate = 32,

        // System Actions
        SystemConfigUpdated = 40,
        SecuritySettingsChanged = 41,
        DatabaseSeeded = 42
    }


}