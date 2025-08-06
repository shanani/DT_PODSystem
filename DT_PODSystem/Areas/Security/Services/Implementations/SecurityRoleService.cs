using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Repositories.Interfaces;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Implementations
{
    public class SecurityRoleService : ISecurityRoleService
    {
        private readonly ISecurityUnitOfWork _securityUnitOfWork;
        private readonly ILogger<SecurityRoleService> _logger;

        public SecurityRoleService(
            ISecurityUnitOfWork securityUnitOfWork,
            ILogger<SecurityRoleService> logger)
        {
            _securityUnitOfWork = securityUnitOfWork;
            _logger = logger;
        }

        #region Basic CRUD

        public async Task<SecurityRole> GetRoleByIdAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(r => r.UserRoles)
                        .ThenInclude(ur => ur.User)
                    .FirstOrDefaultAsync(r => r.Id == roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by ID: {RoleId}", roleId);
                return null;
            }
        }

        public async Task<SecurityRole> GetRoleByNameAsync(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return null;

                return await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(r => r.UserRoles)
                        .ThenInclude(ur => ur.User)
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by name: {RoleName}", roleName);
                return null;
            }
        }

        public async Task<IEnumerable<SecurityRole>> GetAllRolesAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(r => r.UserRoles)
                        .ThenInclude(ur => ur.User)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all roles");
                return new List<SecurityRole>();
            }
        }

        public async Task<IEnumerable<SecurityRole>> GetActiveRolesAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Where(r => r.IsActive)
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Include(r => r.UserRoles)
                        .ThenInclude(ur => ur.User)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active roles");
                return new List<SecurityRole>();
            }
        }

        public async Task<SecurityRole> CreateRoleAsync(SecurityRole role, string createdBy = null)
        {
            try
            {
                if (role == null)
                    throw new ArgumentNullException(nameof(role));

                // Check if role already exists
                var existingRole = await GetRoleByNameAsync(role.Name);
                if (existingRole != null)
                    throw new InvalidOperationException($"Role with name '{role.Name}' already exists");

                // Set audit fields
                role.CreatedAt = DateTime.UtcNow.AddHours(3);
                role.UpdatedAt = DateTime.UtcNow.AddHours(3);
                role.CreatedBy = createdBy ?? "System";
                role.UpdatedBy = createdBy ?? "System";

                await _securityUnitOfWork.Repository<SecurityRole>().AddAsync(role);
                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Role created: {RoleName} by {CreatedBy}", role.Name, createdBy);
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role: {RoleName}", role?.Name);
                throw;
            }
        }

        public async Task<SecurityRole> UpdateRoleAsync(SecurityRole role, string updatedBy = null)
        {
            try
            {
                if (role == null)
                    throw new ArgumentNullException(nameof(role));

                role.UpdatedAt = DateTime.UtcNow.AddHours(3);
                role.UpdatedBy = updatedBy ?? "System";

                _securityUnitOfWork.Repository<SecurityRole>().Update(role);
                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Role updated: {RoleName} by {UpdatedBy}", role.Name, updatedBy);
                return role;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role: {RoleName}", role?.Name);
                throw;
            }
        }

        public async Task<bool> DeleteRoleAsync(int roleId, string deletedBy = null)
        {
            try
            {
                var role = await GetRoleByIdAsync(roleId);
                if (role == null)
                    return false;

                // Check if role is a system role
                if (role.IsSystemRole)
                {
                    _logger.LogWarning("Cannot delete system role: {RoleName}", role.Name);
                    return false;
                }

                // Check if role is assigned to any users
                var hasUsers = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .AnyAsync(ur => ur.RoleId == roleId && ur.IsActive);

                if (hasUsers)
                {
                    _logger.LogWarning("Cannot delete role with active users: {RoleName}", role.Name);
                    return false;
                }

                // Soft delete by deactivating
                role.IsActive = false;
                role.UpdatedAt = DateTime.UtcNow.AddHours(3);
                role.UpdatedBy = deletedBy ?? "System";

                await UpdateRoleAsync(role, deletedBy);

                _logger.LogInformation("Role deleted: {RoleName} by {DeletedBy}", role.Name, deletedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role: {RoleId}", roleId);
                return false;
            }
        }

        #endregion

        #region Role Validation

        public async Task<bool> RoleExistsAsync(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return false;

                var role = await GetRoleByNameAsync(roleName);
                return role != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if role exists: {RoleName}", roleName);
                return false;
            }
        }

        public async Task<bool> CanDeleteRoleAsync(int roleId)
        {
            try
            {
                var role = await GetRoleByIdAsync(roleId);
                if (role == null || role.IsSystemRole)
                    return false;

                var hasUsers = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .AnyAsync(ur => ur.RoleId == roleId && ur.IsActive);

                return !hasUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if role can be deleted: {RoleId}", roleId);
                return false;
            }
        }

        #endregion

        #region Role Statistics

        public async Task<RoleSummaryDto> GetRoleSummaryAsync(int roleId)
        {
            try
            {
                var role = await GetRoleByIdAsync(roleId);
                if (role == null)
                    return null;

                var userCount = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .CountAsync(ur => ur.RoleId == roleId && ur.IsActive);

                var permissionCount = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .CountAsync(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted);

                return new RoleSummaryDto
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = role.IsActive,
                    IsSystemRole = role.IsSystemRole,
                    UserCount = userCount,
                    PermissionCount = permissionCount,
                    CreatedAt = role.CreatedAt,
                    //CreatedBy = role.CreatedBy
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role summary: {RoleId}", roleId);
                return null;
            }
        }

        public async Task<IEnumerable<RoleSummaryDto>> GetAllRoleSummariesAsync()
        {
            try
            {
                var roles = await GetAllRolesAsync();
                var summaries = new List<RoleSummaryDto>();

                foreach (var role in roles)
                {
                    var summary = await GetRoleSummaryAsync(role.Id);
                    if (summary != null)
                        summaries.Add(summary);
                }

                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all role summaries");
                return new List<RoleSummaryDto>();
            }
        }

        public async Task<Dictionary<string, int>> GetRoleStatisticsAsync()
        {
            try
            {
                var totalRoles = await _securityUnitOfWork.Repository<SecurityRole>().CountAsync();
                var activeRoles = await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .CountAsync(r => r.IsActive);
                var systemRoles = await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .CountAsync(r => r.IsSystemRole);
                var customRoles = totalRoles - systemRoles;

                return new Dictionary<string, int>
                {
                    ["Total"] = totalRoles,
                    ["Active"] = activeRoles,
                    ["Inactive"] = totalRoles - activeRoles,
                    ["System"] = systemRoles,
                    ["Custom"] = customRoles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role statistics");
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Role Users Management

        public async Task<IEnumerable<SecurityUser>> GetRoleUsersAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.RoleId == roleId && ur.IsActive))
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role users: {RoleId}", roleId);
                return new List<SecurityUser>();
            }
        }

        public async Task<int> GetRoleUserCountAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .CountAsync(ur => ur.RoleId == roleId && ur.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role user count: {RoleId}", roleId);
                return 0;
            }
        }

        #endregion

        #region System Roles Management

        public async Task<IEnumerable<SecurityRole>> GetSystemRolesAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Where(r => r.IsSystemRole)
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system roles");
                return new List<SecurityRole>();
            }
        }



        #endregion

        #region Role Permissions

        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<Permission>()
                    .GetQueryable()
                    .Where(p => p.RolePermissions.Any(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted))
                    .OrderBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permissions: {RoleId}", roleId);
                return new List<Permission>();
            }
        }

        public async Task<int> GetRolePermissionCountAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .CountAsync(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permission count: {RoleId}", roleId);
                return 0;
            }
        }

        #endregion
    }
}