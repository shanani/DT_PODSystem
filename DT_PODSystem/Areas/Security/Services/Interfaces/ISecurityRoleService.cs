using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface ISecurityRoleService
    {
        // Basic CRUD
        Task<SecurityRole> GetRoleByIdAsync(int roleId);
        Task<SecurityRole> GetRoleByNameAsync(string roleName);
        Task<IEnumerable<SecurityRole>> GetAllRolesAsync();
        Task<IEnumerable<SecurityRole>> GetActiveRolesAsync();
        Task<SecurityRole> CreateRoleAsync(SecurityRole role, string createdBy = null);
        Task<SecurityRole> UpdateRoleAsync(SecurityRole role, string updatedBy = null);
        Task<bool> DeleteRoleAsync(int roleId, string deletedBy = null);

        // Role Validation
        Task<bool> RoleExistsAsync(string roleName);
        Task<bool> CanDeleteRoleAsync(int roleId);

        // Role Statistics
        Task<RoleSummaryDto> GetRoleSummaryAsync(int roleId);
        Task<IEnumerable<RoleSummaryDto>> GetAllRoleSummariesAsync();
        Task<Dictionary<string, int>> GetRoleStatisticsAsync();

        // Role Users Management
        Task<IEnumerable<SecurityUser>> GetRoleUsersAsync(int roleId);
        Task<int> GetRoleUserCountAsync(int roleId);

        // System Roles Management
        Task<IEnumerable<SecurityRole>> GetSystemRolesAsync();


        // Role Permissions
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);
        Task<int> GetRolePermissionCountAsync(int roleId);
    }
}