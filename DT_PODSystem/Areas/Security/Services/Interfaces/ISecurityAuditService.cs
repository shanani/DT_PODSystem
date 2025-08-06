using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface ISecurityAuditService
    {
        // Audit Log Creation
        Task<bool> LogSecurityEventAsync(string action, string entityType, string entityId,
            string userId, string userName, string description, string ipAddress = null,
            string userAgent = null, bool isSuccessful = true, string errorMessage = null);
        Task<bool> LogUserActionAsync(string action, string userId, string userName,
            string description, string ipAddress = null, string userAgent = null,
            bool isSuccessful = true, string errorMessage = null);
        Task<bool> LogRoleActionAsync(string action, int roleId, string userId, string userName,
            string description, string ipAddress = null, string userAgent = null,
            bool isSuccessful = true, string errorMessage = null);
        Task<bool> LogPermissionActionAsync(string action, int permissionId, string userId, string userName,
            string description, string ipAddress = null, string userAgent = null,
            bool isSuccessful = true, string errorMessage = null);
        Task<bool> LogLoginAttemptAsync(string userId, string userName, bool isSuccessful,
            string ipAddress = null, string userAgent = null, string errorMessage = null);
        Task<bool> LogLogoutAsync(string userId, string userName, string ipAddress = null, string userAgent = null);

        // Generic Log Method
        Task<bool> LogAsync(string action, string entityType = null, string entityId = null,
            string userId = null, string userName = null, string description = null,
            string ipAddress = null, string userAgent = null, bool isSuccessful = true, string errorMessage = null);
        Task LogSystemActionAsync(string action, string userId, string details, string ipAddress);
        Task LogPermissionTypeActionAsync(string action, int permissionTypeId, string permissionTypeName,
            string userId, string userName, string details, string ipAddress, bool success, string errorMessage);

        // Audit Log Queries
        Task<IEnumerable<SecurityAuditDto>> GetRecentAuditLogsAsync(int count = 50);
        Task<IEnumerable<SecurityAuditDto>> GetAuditLogsByUserAsync(string userId, int count = 50);
        Task<IEnumerable<SecurityAuditDto>> GetAuditLogsByEntityAsync(string entityType, string entityId, int count = 50);
        Task<IEnumerable<SecurityAuditDto>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int count = 100);
        Task<Dictionary<string, int>> GetAuditActivityByDateAsync(DateTime fromDate, DateTime toDate);

        // Audit Statistics
        Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, int>> GetEntityTypeStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<Dictionary<string, int>> GetHourlyActivityAsync(DateTime date);

        // Security Alerts
        Task<IEnumerable<SecurityAuditDto>> GetFailedLoginAttemptsAsync(int count = 50);
        Task<IEnumerable<SecurityAuditDto>> GetSuspiciousActivitiesAsync(int count = 50);
        Task<Dictionary<string, int>> GetFailedLoginsByIPAsync(DateTime fromDate, int count = 20);

        // Audit Log Cleanup
        Task<int> CleanupOldAuditLogsAsync(DateTime beforeDate);
        Task<int> GetAuditLogCountAsync();
    }
}