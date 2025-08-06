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
    public class RolePermissionService : IRolePermissionService
    {
        private readonly ISecurityUnitOfWork _securityUnitOfWork;
        private readonly ILogger<RolePermissionService> _logger;

        public RolePermissionService(
            ISecurityUnitOfWork securityUnitOfWork,
            ILogger<RolePermissionService> logger)
        {
            _securityUnitOfWork = securityUnitOfWork;
            _logger = logger;
        }

        #region Role Permission Assignment

        public async Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId, string assignedBy = null)
        {
            try
            {
                var role = await _securityUnitOfWork.Repository<SecurityRole>().GetByIdAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("Role not found: {RoleId}", roleId);
                    return false;
                }

                var permission = await _securityUnitOfWork.Repository<Permission>().GetByIdAsync(permissionId);
                if (permission == null)
                {
                    _logger.LogWarning("Permission not found: {PermissionId}", permissionId);
                    return false;
                }

                // Check if assignment already exists
                var existingAssignment = await _securityUnitOfWork.Repository<RolePermission>()
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (existingAssignment != null)
                {
                    if (existingAssignment.IsActive && existingAssignment.IsGranted)
                    {
                        _logger.LogInformation("Permission already assigned to role: {RoleName} - {PermissionName}", role.Name, permission.Name);
                        return true; // Already assigned
                    }
                    else
                    {
                        // Reactivate and grant the assignment
                        existingAssignment.IsActive = true;
                        existingAssignment.IsGranted = true;
                        existingAssignment.GrantedAt = DateTime.UtcNow.AddHours(3);
                        existingAssignment.GrantedBy = assignedBy ?? "System";
                        existingAssignment.RevokedAt = null;
                        existingAssignment.RevokedBy = null;
                        _securityUnitOfWork.Repository<RolePermission>().Update(existingAssignment);
                    }
                }
                else
                {
                    // Create new assignment
                    var newAssignment = new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        IsGranted = true,
                        IsActive = true,
                        GrantedAt = DateTime.UtcNow.AddHours(3),
                        GrantedBy = assignedBy ?? "System"
                    };

                    await _securityUnitOfWork.Repository<RolePermission>().AddAsync(newAssignment);
                }

                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Permission assigned to role: {RoleName} - {PermissionName} by {AssignedBy}",
                    role.Name, permission.Name, assignedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permission to role: {RoleId} - {PermissionId}", roleId, permissionId);
                return false;
            }
        }

        public async Task<bool> RevokePermissionFromRoleAsync(int roleId, int permissionId, string revokedBy = null)
        {
            try
            {
                var assignment = await _securityUnitOfWork.Repository<RolePermission>()
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && rp.IsActive && rp.IsGranted);

                if (assignment == null)
                {
                    _logger.LogWarning("Permission assignment not found: {RoleId} - {PermissionId}", roleId, permissionId);
                    return false;
                }

                // Revoke the assignment
                assignment.IsActive = false;
                assignment.IsGranted = false;
                assignment.RevokedAt = DateTime.UtcNow.AddHours(3);
                assignment.RevokedBy = revokedBy ?? "System";

                _securityUnitOfWork.Repository<RolePermission>().Update(assignment);
                await _securityUnitOfWork.SaveChangesAsync();

                var role = await _securityUnitOfWork.Repository<SecurityRole>().GetByIdAsync(roleId);
                var permission = await _securityUnitOfWork.Repository<Permission>().GetByIdAsync(permissionId);

                _logger.LogInformation("Permission revoked from role: {RoleName} - {PermissionName} by {RevokedBy}",
                    role?.Name, permission?.Name, revokedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking permission from role: {RoleId} - {PermissionId}", roleId, permissionId);
                return false;
            }
        }

        public async Task<bool> RoleHasPermissionAsync(int roleId, int permissionId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId && rp.IsActive && rp.IsGranted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if role has permission: {RoleId} - {PermissionId}", roleId, permissionId);
                return false;
            }
        }

        public async Task<bool> RoleHasPermissionAsync(int roleId, string permissionName)
        {
            try
            {
                return await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted)
                    .Include(rp => rp.Permission)
                    .AnyAsync(rp => rp.Permission.Name.ToLower() == permissionName.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if role has permission: {RoleId} - {PermissionName}", roleId, permissionName);
                return false;
            }
        }

        #endregion

        #region Additional Bulk Operations

        public async Task<bool> AssignPermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, string assignedBy = null)
        {
            return await AssignMultiplePermissionsToRoleAsync(roleId, permissionIds, assignedBy);
        }

        public async Task<bool> BulkAssignPermissionsAsync(int roleId, IEnumerable<int> permissionIds, string assignedBy = null)
        {
            return await AssignMultiplePermissionsToRoleAsync(roleId, permissionIds, assignedBy);
        }

        public async Task<bool> BulkRevokePermissionsAsync(int roleId, IEnumerable<int> permissionIds, string revokedBy = null)
        {
            return await RevokePermissionsFromRoleAsync(roleId, permissionIds, revokedBy);
        }

        public async Task<bool> RevokePermissionsFromRoleAsync(int roleId, IEnumerable<int> permissionIds, string revokedBy = null)
        {
            try
            {
                if (permissionIds == null || !permissionIds.Any())
                    return false;

                var successCount = 0;
                foreach (var permissionId in permissionIds.Distinct())
                {
                    if (await RevokePermissionFromRoleAsync(roleId, permissionId, revokedBy))
                        successCount++;
                }

                var role = await _securityUnitOfWork.Repository<SecurityRole>().GetByIdAsync(roleId);
                _logger.LogInformation("Revoked {SuccessCount} of {TotalCount} permissions from role: {RoleName}",
                    successCount, permissionIds.Count(), role?.Name);
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking multiple permissions from role: {RoleId}", roleId);
                return false;
            }
        }

        #endregion

        #region Additional Query Methods

        public async Task<RolePermission> GetRolePermissionAssignmentAsync(int roleId, int permissionId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.PermissionId == permissionId)
                    .Include(rp => rp.Permission)
                    .Include(rp => rp.Role)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permission assignment: {RoleId} - {PermissionId}", roleId, permissionId);
                return null;
            }
        }

        public async Task<IEnumerable<int>> GetGrantedPermissionIdsAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting granted permission IDs: {RoleId}", roleId);
                return new List<int>();
            }
        }

        public async Task<IEnumerable<RolePermission>> SearchRolePermissionsAsync(int roleId, string searchTerm)
        {
            try
            {
                var query = _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId);

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var lowerSearchTerm = searchTerm.ToLower();
                    query = query.Where(rp =>
                        rp.Permission.Name.ToLower().Contains(lowerSearchTerm) ||
                        rp.Permission.DisplayName.ToLower().Contains(lowerSearchTerm) ||
                        rp.Permission.Description.ToLower().Contains(lowerSearchTerm));
                }

                return await query
                    .Include(rp => rp.Permission)
                    .Include(rp => rp.Role)
                    .OrderBy(rp => rp.Permission.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching role permissions: {RoleId} - {SearchTerm}", roleId, searchTerm);
                return new List<RolePermission>();
            }
        }

        public async Task<bool> CopyRolePermissionsAsync(int sourceRoleId, int targetRoleId, string copiedBy = null)
        {
            return await CopyPermissionsFromRoleAsync(sourceRoleId, targetRoleId, copiedBy);
        }

        #endregion

        #region Bulk Role Permission Operations

        public async Task<bool> AssignMultiplePermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, string assignedBy = null)
        {
            try
            {
                if (permissionIds == null || !permissionIds.Any())
                    return false;

                var role = await _securityUnitOfWork.Repository<SecurityRole>().GetByIdAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("Role not found: {RoleId}", roleId);
                    return false;
                }

                var successCount = 0;
                foreach (var permissionId in permissionIds.Distinct())
                {
                    if (await AssignPermissionToRoleAsync(roleId, permissionId, assignedBy))
                        successCount++;
                }

                _logger.LogInformation("Assigned {SuccessCount} of {TotalCount} permissions to role: {RoleName}",
                    successCount, permissionIds.Count(), role.Name);
                return successCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning multiple permissions to role: {RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> RevokeAllPermissionsFromRoleAsync(int roleId, string revokedBy = null)
        {
            try
            {
                var assignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted)
                    .ToListAsync();

                if (!assignments.Any())
                    return true;

                foreach (var assignment in assignments)
                {
                    assignment.IsActive = false;
                    assignment.IsGranted = false;
                    assignment.RevokedAt = DateTime.UtcNow.AddHours(3);
                    assignment.RevokedBy = revokedBy ?? "System";
                    _securityUnitOfWork.Repository<RolePermission>().Update(assignment);
                }

                await _securityUnitOfWork.SaveChangesAsync();

                var role = await _securityUnitOfWork.Repository<SecurityRole>().GetByIdAsync(roleId);
                _logger.LogInformation("All permissions revoked from role: {RoleName} by {RevokedBy}", role?.Name, revokedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking all permissions from role: {RoleId}", roleId);
                return false;
            }
        }

        public async Task<bool> SetRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, string updatedBy = null)
        {
            try
            {
                if (permissionIds == null)
                    permissionIds = new List<int>();

                var role = await _securityUnitOfWork.Repository<SecurityRole>().GetByIdAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("Role not found: {RoleId}", roleId);
                    return false;
                }

                // Get current assignments
                var currentAssignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted)
                    .ToListAsync();

                var currentPermissionIds = currentAssignments.Select(rp => rp.PermissionId).ToHashSet();
                var newPermissionIds = permissionIds.ToHashSet();

                // Revoke permissions that are no longer needed
                var toRevoke = currentPermissionIds.Except(newPermissionIds);
                foreach (var permissionId in toRevoke)
                {
                    await RevokePermissionFromRoleAsync(roleId, permissionId, updatedBy);
                }

                // Assign new permissions
                var toAssign = newPermissionIds.Except(currentPermissionIds);
                foreach (var permissionId in toAssign)
                {
                    await AssignPermissionToRoleAsync(roleId, permissionId, updatedBy);
                }

                _logger.LogInformation("Role permissions updated: {RoleName} - Added: {AddedCount}, Removed: {RemovedCount}",
                    role.Name, toAssign.Count(), toRevoke.Count());
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting role permissions: {RoleId}", roleId);
                return false;
            }
        }

        #endregion

        #region Role Permission Queries

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

        public async Task<IEnumerable<SecurityRole>> GetPermissionRolesAsync(int permissionId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Where(r => r.IsActive && r.RolePermissions.Any(rp => rp.PermissionId == permissionId && rp.IsActive && rp.IsGranted))
                    .OrderBy(r => r.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission roles: {PermissionId}", permissionId);
                return new List<SecurityRole>();
            }
        }

        public async Task<IEnumerable<RolePermission>> GetRolePermissionAssignmentsAsync(int roleId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.RoleId == roleId)
                    .Include(rp => rp.Permission)
                    .Include(rp => rp.Role)
                    .OrderBy(rp => rp.Permission.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permission assignments: {RoleId}", roleId);
                return new List<RolePermission>();
            }
        }

        #endregion

        #region Permission Assignment Statistics

        public async Task<Dictionary<string, int>> GetRolePermissionStatisticsAsync(int roleId)
        {
            try
            {
                var totalAssignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .CountAsync(rp => rp.RoleId == roleId);

                var activeAssignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .CountAsync(rp => rp.RoleId == roleId && rp.IsActive && rp.IsGranted);

                var revokedAssignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .CountAsync(rp => rp.RoleId == roleId && (!rp.IsActive || !rp.IsGranted));

                return new Dictionary<string, int>
                {
                    ["Total"] = totalAssignments,
                    ["Active"] = activeAssignments,
                    ["Revoked"] = revokedAssignments
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permission statistics: {RoleId}", roleId);
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetGlobalPermissionStatisticsAsync()
        {
            try
            {
                var totalAssignments = await _securityUnitOfWork.Repository<RolePermission>().CountAsync();
                var activeAssignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .CountAsync(rp => rp.IsActive && rp.IsGranted);
                var revokedAssignments = totalAssignments - activeAssignments;

                var rolesWithPermissions = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.IsActive && rp.IsGranted)
                    .Select(rp => rp.RoleId)
                    .Distinct()
                    .CountAsync();

                var permissionsInUse = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.IsActive && rp.IsGranted)
                    .Select(rp => rp.PermissionId)
                    .Distinct()
                    .CountAsync();

                return new Dictionary<string, int>
                {
                    ["TotalAssignments"] = totalAssignments,
                    ["ActiveAssignments"] = activeAssignments,
                    ["RevokedAssignments"] = revokedAssignments,
                    ["RolesWithPermissions"] = rolesWithPermissions,
                    ["PermissionsInUse"] = permissionsInUse
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting global permission statistics");
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Permission Tree and Hierarchy

        public async Task<RolePermissionTreeDto> GetRolePermissionTreeAsync(int roleId)
        {
            try
            {
                var role = await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(r => r.Id == roleId);

                if (role == null)
                    return null;

                var allPermissions = await _securityUnitOfWork.Repository<Permission>()
                    .GetQueryable()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var assignedPermissionIds = role.RolePermissions
                    .Where(rp => rp.IsActive && rp.IsGranted)
                    .Select(rp => rp.PermissionId)
                    .ToHashSet();

                var tree = new RolePermissionTreeDto
                {
                    RoleId = roleId,
                    RoleName = role.Name,
                    Permissions = allPermissions.Select(p => new PermissionNodeDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DisplayName = p.DisplayName,
                        Description = p.Description,
                        IsAssigned = assignedPermissionIds.Contains(p.Id),
                        IsSystemPermission = p.IsSystemPermission
                    }).ToList()
                };

                return tree;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permission tree: {RoleId}", roleId);
                return null;
            }
        }

        #endregion

        #region Permission Matrix

        public async Task<PermissionMatrixDto> GetPermissionMatrixAsync()
        {
            try
            {
                var roles = await _securityUnitOfWork.Repository<SecurityRole>()
                    .GetQueryable()
                    .Where(r => r.IsActive)
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                var permissions = await _securityUnitOfWork.Repository<Permission>()
                    .GetQueryable()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var assignments = await _securityUnitOfWork.Repository<RolePermission>()
                    .GetQueryable()
                    .Where(rp => rp.IsActive && rp.IsGranted)
                    .ToListAsync();

                var matrix = new PermissionMatrixDto
                {
                    Roles = roles.Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        IsSystemRole = r.IsSystemRole
                    }).ToList(),
                    Permissions = permissions.Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DisplayName = p.DisplayName,
                        Description = p.Description,
                        IsSystemPermission = p.IsSystemPermission
                    }).ToList(),
                    Assignments = assignments.ToDictionary(
                        a => $"{a.RoleId}_{a.PermissionId}",
                        a => true
                    )
                };

                return matrix;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission matrix");
                return null;
            }
        }

        #endregion

        #region Copy and Clone Operations

        public async Task<bool> CopyPermissionsFromRoleAsync(int sourceRoleId, int targetRoleId, string copiedBy = null)
        {
            try
            {
                var sourcePermissions = await GetRolePermissionsAsync(sourceRoleId);
                var sourcePermissionIds = sourcePermissions.Select(p => p.Id);

                return await AssignMultiplePermissionsToRoleAsync(targetRoleId, sourcePermissionIds, copiedBy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying permissions from role {SourceRoleId} to {TargetRoleId}", sourceRoleId, targetRoleId);
                return false;
            }
        }

        #endregion
    }
}