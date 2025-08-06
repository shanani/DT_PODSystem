using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface IRolePermissionService
    {
        // Role Permission Assignment
        Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId, string assignedBy = null);
        Task<bool> RevokePermissionFromRoleAsync(int roleId, int permissionId, string revokedBy = null);
        Task<bool> RoleHasPermissionAsync(int roleId, int permissionId);
        Task<bool> RoleHasPermissionAsync(int roleId, string permissionName);

        // Bulk Role Permission Operations
        Task<bool> AssignMultiplePermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, string assignedBy = null);
        Task<bool> RevokeAllPermissionsFromRoleAsync(int roleId, string revokedBy = null);
        Task<bool> SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, string updatedBy = null);

        // Role Permission Queries
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);
        Task<IEnumerable<SecurityRole>> GetPermissionRolesAsync(int permissionId);
        Task<IEnumerable<RolePermission>> GetRolePermissionAssignmentsAsync(int roleId);

        // Permission Assignment Statistics
        Task<Dictionary<string, int>> GetRolePermissionStatisticsAsync(int roleId);
        Task<Dictionary<string, int>> GetGlobalPermissionStatisticsAsync();

        // Permission Tree and Hierarchy
        Task<RolePermissionTreeDto> GetRolePermissionTreeAsync(int roleId);

        // Permission Matrix
        Task<PermissionMatrixDto> GetPermissionMatrixAsync();

        // Copy and Clone Operations
        Task<bool> CopyPermissionsFromRoleAsync(int sourceRoleId, int targetRoleId, string copiedBy = null);
    }
}