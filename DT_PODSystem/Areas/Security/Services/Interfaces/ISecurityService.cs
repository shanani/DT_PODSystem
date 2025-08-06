using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface ISecurityService
    {
        Task<bool> CanAccessSecurityAreaAsync(int userId);
        Task<List<string>> GetRolePermissionsAsync(int roleId);
        Task<SecurityContextDto> GetSecurityContextAsync(int userId);
        Task<bool> RoleHasPermissionAsync(int roleId, string permissionName);

        // Dashboard and Statistics
        Task<SecurityStatisticsDto> GetSecurityStatisticsAsync();
        Task<List<SecurityAuditDto>> GetRecentSecurityActivitiesAsync(int count = 10);

        // User Permission Checking
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<bool> HasPermissionAsync(string userCode, string permissionName);
        Task<List<string>> GetUserPermissionsAsync(int userId);
        Task<List<string>> GetUserPermissionsAsync(string userCode);

        // Super Admin Functions
        Task<bool> IsSuperAdminAsync(int userId);
        Task<bool> IsSuperAdminAsync(string userCode);
        Task<bool> IsProductionModeAsync();
        Task<List<SecurityUser>> GetSuperAdminUsersAsync();

        // Permission Validation
        bool IsValidPermissionName(string permissionName);
        string GeneratePermissionName(string permissionType, string action);
        Task<bool> PermissionExistsAsync(string permissionName);
    }
}