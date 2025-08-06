using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Repositories.Interfaces;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Implementations
{
    public class SecurityService : ISecurityService
    {
        private readonly ISecurityUnitOfWork _securityUnitOfWork;
        private readonly ISecurityUserService _userService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityService> _logger;

        public SecurityService(
            ISecurityUnitOfWork securityUnitOfWork,
            ISecurityUserService userService,
            IConfiguration configuration,
            ILogger<SecurityService> logger)
        {
            _securityUnitOfWork = securityUnitOfWork;
            _userService = userService;
            _configuration = configuration;
            _logger = logger;
        }

        #region Basic Security Methods

        public async Task<bool> CanAccessSecurityAreaAsync(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null || !user.IsActive) return false;

                // Super admin always has access
                if (user.IsSuperAdmin) return true;

                // Check if user has admin role or security permissions
                var userRoles = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.UserId == userId && ur.IsActive)
                    .Include(ur => ur.Role)
                    .ToListAsync();

                return userRoles.Any(ur => ur.Role.Name == "Admin" || ur.Role.Name == "SecurityAdmin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking security area access for user: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<string>> GetRolePermissionsAsync(int roleId)
        {
            try
            {
                var permissions = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission.Name)
                    .ToListAsync();

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permissions: {RoleId}", roleId);
                return new List<string>();
            }
        }

        public async Task<SecurityContextDto> GetSecurityContextAsync(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null) return null;

                var userRoles = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.UserId == userId && ur.IsActive)
                    .Include(ur => ur.Role)
                    .ToListAsync();

                var roleNames = userRoles.Select(ur => ur.Role.Name).ToList();
                var permissions = new List<string>();

                foreach (var userRole in userRoles)
                {
                    var rolePermissions = await GetRolePermissionsAsync(userRole.RoleId);
                    permissions.AddRange(rolePermissions);
                }

                return new SecurityContextDto
                {
                    UserId = userId,
                    UserName = user.Code,
                    IsSuperAdmin = user.IsSuperAdmin,
                    Roles = roleNames,
                    Permissions = permissions.Distinct().ToList(),
                    CanAccessSecurityArea = await CanAccessSecurityAreaAsync(userId)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security context: {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> RoleHasPermissionAsync(int roleId, string permissionName)
        {
            try
            {
                var hasPermission = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted)
                    .Include(rp => rp.Permission)
                    .AnyAsync(rp => rp.Permission.Name == permissionName);

                return hasPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role permission: {RoleId}, {PermissionName}", roleId, permissionName);
                return false;
            }
        }

        #endregion

        #region Dashboard and Statistics

        public async Task<SecurityStatisticsDto> GetSecurityStatisticsAsync()
        {
            try
            {
                var users = await _securityUnitOfWork.Repository<SecurityUser>().GetAllAsync();
                var roles = await _securityUnitOfWork.Repository<SecurityRole>().GetAllAsync();
                var permissions = await _securityUnitOfWork.Repository<Permission>().GetAllAsync();
                var roleAssignments = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.IsActive)
                    .ToListAsync();
                var permissionAssignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.IsActive && rp.IsGranted)
                    .ToListAsync();

                var usersByRole = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.IsActive)
                    .Include(ur => ur.Role)
                    .GroupBy(ur => ur.Role.Name)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count);

                var usersWithRolesDict = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.IsActive)
                    .Include(ur => ur.Role)
                    .GroupBy(ur => ur.Role.Name)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count);

                var rolesWithPermissionsDict = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.IsActive && rp.IsGranted)
                    .Include(rp => rp.Role)
                    .GroupBy(rp => rp.Role.Name)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count);

                // Count permission types
                var permissionTypes = permissions
                    .Where(p => p.Name.Contains("_"))
                    .Select(p => p.Name.Split('_')[0])
                    .Distinct()
                    .Count();

                var activePermissionTypes = permissions
                    .Where(p => p.IsActive && p.Name.Contains("_"))
                    .Select(p => p.Name.Split('_')[0])
                    .Distinct()
                    .Count();

                // Count users without roles
                var usersWithoutRoles = await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Where(u => u.IsActive && !u.UserRoles.Any(ur => ur.IsActive))
                    .CountAsync();

                // Count roles without permissions
                var rolesWithoutPermissions = await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Where(r => r.IsActive && !r.RolePermissions.Any(rp => rp.IsActive && rp.IsGranted))
                    .CountAsync();

                return new SecurityStatisticsDto
                {
                    // User statistics
                    TotalUsers = users.Count(),
                    ActiveUsers = users.Count(u => u.IsActive),
                    InactiveUsers = users.Count(u => !u.IsActive),
                    SuperAdminUsers = users.Count(u => u.IsSuperAdmin),
                    SuperAdminCount = users.Count(u => u.IsSuperAdmin),
                    UsersWithoutRoles = usersWithoutRoles,

                    // Role statistics
                    TotalRoles = roles.Count(),
                    ActiveRoles = roles.Count(r => r.IsActive),
                    SystemRoles = roles.Count(r => r.IsSystemRole),
                    CustomRoles = roles.Count(r => !r.IsSystemRole),
                    RolesWithoutPermissions = rolesWithoutPermissions,

                    // Permission statistics
                    TotalPermissions = permissions.Count(),
                    ActivePermissions = permissions.Count(p => p.IsActive),
                    SystemPermissions = permissions.Count(p => p.IsSystemPermission),
                    CustomPermissions = permissions.Count(p => !p.IsSystemPermission),
                    TotalPermissionTypes = permissionTypes,
                    ActivePermissionTypes = activePermissionTypes,

                    // Assignment statistics
                    TotalRoleAssignments = roleAssignments.Count,
                    TotalPermissionAssignments = permissionAssignments.Count,

                    // Dictionary properties
                    UsersWithRoles = usersWithRolesDict,
                    RolesWithPermissions = rolesWithPermissionsDict,
                    UsersByRole = usersByRole,

                    // Timestamp
                    LastUpdated = DateTime.UtcNow.AddHours(3)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting security statistics");
                return new SecurityStatisticsDto();
            }
        }

        public async Task<List<SecurityAuditDto>> GetRecentSecurityActivitiesAsync(int count = 10)
        {
            try
            {
                // This would typically come from a SecurityAuditLog table
                // For now, return empty list or mock data
                await Task.CompletedTask;
                return new List<SecurityAuditDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent security activities");
                return new List<SecurityAuditDto>();
            }
        }

        #endregion

        #region User Permission Checking

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            try
            {
                var userPermissions = await GetUserPermissionsAsync(userId);
                return userPermissions.Contains(permissionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user permission: {UserId}, {PermissionName}", userId, permissionName);
                return false;
            }
        }

        public async Task<bool> HasPermissionAsync(string userCode, string permissionName)
        {
            try
            {
                var user = await _userService.GetUserByCodeAsync(userCode);
                if (user == null) return false;

                return await HasPermissionAsync(user.Id, permissionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user permission: {UserCode}, {PermissionName}", userCode, permissionName);
                return false;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null) return new List<string>();

                // Super admin has all permissions
                if (user.IsSuperAdmin)
                {
                    var allPermissions = await _securityUnitOfWork.Repository<Permission>()
                        .GetQueryable()
                        .Where(p => p.IsActive)
                        .Select(p => p.Name)
                        .ToListAsync();
                    return allPermissions;
                }

                var userRoles = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.UserId == userId && ur.IsActive)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                var permissions = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => userRoles.Contains(rp.RoleId) && rp.IsActive && rp.IsGranted)
                    .Include(rp => rp.Permission)
                    .Select(rp => rp.Permission.Name)
                    .Distinct()
                    .ToListAsync();

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions: {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userCode)
        {
            try
            {
                var user = await _userService.GetUserByCodeAsync(userCode);
                if (user == null) return new List<string>();

                return await GetUserPermissionsAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions: {UserCode}", userCode);
                return new List<string>();
            }
        }

        #endregion

        #region Super Admin Functions

        public async Task<bool> IsSuperAdminAsync(int userId)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                return user?.IsSuperAdmin == true && user.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking super admin status: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> IsSuperAdminAsync(string userCode)
        {
            try
            {
                var user = await _userService.GetUserByCodeAsync(userCode);
                return user?.IsSuperAdmin == true && user.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking super admin status: {UserCode}", userCode);
                return false;
            }
        }

        public async Task<bool> IsProductionModeAsync()
        {
            try
            {
                await Task.CompletedTask;
                var environment = _configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production";
                return environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking production mode");
                return true; // Default to production for safety
            }
        }

        public async Task<List<SecurityUser>> GetSuperAdminUsersAsync()
        {
            try
            {
                var superAdminUsers = await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Where(u => u.IsSuperAdmin && u.IsActive)
                    .ToListAsync();

                return superAdminUsers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting super admin users");
                return new List<SecurityUser>();
            }
        }

        #endregion

        #region Permission Validation

        public bool IsValidPermissionName(string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName)) return false;

            // Permission names should follow pattern: Type_Action (e.g., Domain_Create, User_Edit)
            var pattern = @"^[A-Za-z][A-Za-z0-9]*_[A-Za-z][A-Za-z0-9]*$";
            return Regex.IsMatch(permissionName, pattern);
        }

        public string GeneratePermissionName(string permissionType, string action)
        {
            if (string.IsNullOrWhiteSpace(permissionType) || string.IsNullOrWhiteSpace(action))
                return string.Empty;

            // Clean and format the strings
            var cleanType = Regex.Replace(permissionType.Trim(), @"[^A-Za-z0-9]", "");
            var cleanAction = Regex.Replace(action.Trim(), @"[^A-Za-z0-9]", "");

            if (string.IsNullOrEmpty(cleanType) || string.IsNullOrEmpty(cleanAction))
                return string.Empty;

            return $"{cleanType}_{cleanAction}";
        }

        public async Task<bool> PermissionExistsAsync(string permissionName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(permissionName)) return false;

                var exists = await _securityUnitOfWork.Repository<Permission>()
                    .GetQueryable()
                    .AnyAsync(p => p.Name == permissionName);

                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if permission exists: {PermissionName}", permissionName);
                return false;
            }
        }

        #endregion
    }
}