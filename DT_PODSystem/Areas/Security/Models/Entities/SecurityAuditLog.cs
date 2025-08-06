// Areas/Security/Models/Entities/SecurityAuditLog.cs
using System;
using System.ComponentModel.DataAnnotations;
using DT_PODSystem.Areas.Security.Models.Enums;

namespace DT_PODSystem.Areas.Security.Models.Entities
{
    /// <summary>
    /// Audit log for security-related actions
    /// </summary>
    public class SecurityAuditLog
    {
        public int Id { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Description { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }

        public AuditActionType ActionType { get; set; }


        [MaxLength(200)]
        public string EntityName { get; set; }


        [MaxLength(2000)]
        public string Details { get; set; } // JSON or description

        [MaxLength(100)]
        public string PerformedBy { get; set; }



        public DateTime Timestamp { get; set; } = DateTime.UtcNow.AddHours(3);

        // Helper properties
        public string ActionIcon => ActionType switch
        {
            AuditActionType.PermissionCreated => "fas fa-plus text-success",
            AuditActionType.PermissionUpdated => "fas fa-edit text-warning",
            AuditActionType.PermissionDeleted => "fas fa-trash text-danger",
            AuditActionType.RoleAssigned => "fas fa-user-tag text-info",
            AuditActionType.RoleRevoked => "fas fa-user-minus text-warning",
            AuditActionType.UserCreated => "fas fa-user-plus text-success",
            AuditActionType.UserUpdated => "fas fa-user-edit text-warning",
            AuditActionType.UserDeleted => "fas fa-user-times text-danger",
            _ => "fas fa-shield text-secondary"
        };

        public string ActionColor => ActionType switch
        {
            AuditActionType.PermissionCreated or AuditActionType.UserCreated => "success",
            AuditActionType.PermissionUpdated or AuditActionType.UserUpdated => "warning",
            AuditActionType.PermissionDeleted or AuditActionType.UserDeleted => "danger",
            AuditActionType.RoleAssigned => "info",
            AuditActionType.RoleRevoked => "warning",
            _ => "secondary"
        };
    }
}

