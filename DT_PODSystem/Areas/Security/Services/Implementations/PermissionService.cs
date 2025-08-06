// Areas/Security/Services/Implementations/PermissionService.cs (Updated - Part 1)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Implementations
{
    public class PermissionService : IPermissionService
    {
        private readonly SecurityDbContext _context;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(SecurityDbContext context, ILogger<PermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region Existing Methods (Keep your current implementations)

        public async Task<List<Permission>> GetAllPermissionsAsync()
        {
            return await _context.Permissions
                .Include(p => p.PermissionType)
                .Include(p => p.ParentPermission)
                .Include(p => p.Children)
                .OrderBy(p => p.PermissionTypeId)
                .ThenBy(p => p.Level)
                .ThenBy(p => p.SortOrder)
                .ToListAsync();
        }

        public async Task<List<Permission>> GetPermissionsByTypeAsync(string permissionTypeName)
        {
            return await _context.Permissions
                .Include(p => p.PermissionType)
                .Include(p => p.ParentPermission)
                .Include(p => p.Children)
                .Where(p => p.PermissionType.Name == permissionTypeName)
                .OrderBy(p => p.Level)
                .ThenBy(p => p.SortOrder)
                .ToListAsync();
        }

        public async Task<Permission?> GetPermissionByIdAsync(int id)
        {
            return await _context.Permissions
                .Include(p => p.PermissionType)
                .Include(p => p.ParentPermission)
                .Include(p => p.Children)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        #endregion

        #region 🆕 Hierarchical Permission Methods

        public async Task<List<Permission>> GetRootPermissionsAsync(int permissionTypeId)
        {
            return await _context.Permissions
                .Include(p => p.Children)
                .Where(p => p.PermissionTypeId == permissionTypeId && p.ParentPermissionId == null && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Permission>> GetChildPermissionsAsync(int parentPermissionId)
        {
            return await _context.Permissions
                .Include(p => p.Children)
                .Where(p => p.ParentPermissionId == parentPermissionId && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<List<Permission>> GetPermissionHierarchyAsync(int permissionTypeId)
        {
            // Get all permissions for the type
            var allPermissions = await _context.Permissions
                .Where(p => p.PermissionTypeId == permissionTypeId && p.IsActive)
                .OrderBy(p => p.Level)
                .ThenBy(p => p.SortOrder)
                .ToListAsync();

            // Build hierarchy
            var permissionDict = allPermissions.ToDictionary(p => p.Id);

            foreach (var permission in allPermissions)
            {
                if (permission.ParentPermissionId.HasValue &&
                    permissionDict.TryGetValue(permission.ParentPermissionId.Value, out var parent))
                {
                    parent.Children.Add(permission);
                    permission.ParentPermission = parent;
                }
            }

            return allPermissions.Where(p => p.ParentPermissionId == null).ToList();
        }

        public async Task<Permission?> GetPermissionWithChildrenAsync(int id)
        {
            return await _context.Permissions
                .Include(p => p.PermissionType)
                .Include(p => p.ParentPermission)
                .Include(p => p.Children.Where(c => c.IsActive))
                .ThenInclude(c => c.Children.Where(cc => cc.IsActive))
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<List<Permission>> GetPermissionAncestorsAsync(int permissionId)
        {
            var permission = await GetPermissionByIdAsync(permissionId);
            if (permission == null) return new List<Permission>();

            var ancestors = new List<Permission>();
            var current = permission.ParentPermission;

            while (current != null)
            {
                ancestors.Insert(0, current);
                current = await _context.Permissions
                    .Include(p => p.ParentPermission)
                    .FirstOrDefaultAsync(p => p.Id == current.ParentPermissionId);
            }

            return ancestors;
        }

        public async Task<List<Permission>> GetPermissionDescendantsAsync(int permissionId)
        {
            var descendants = new List<Permission>();

            var children = await GetChildPermissionsAsync(permissionId);
            foreach (var child in children)
            {
                descendants.Add(child);
                var childDescendants = await GetPermissionDescendantsAsync(child.Id);
                descendants.AddRange(childDescendants);
            }

            return descendants;
        }

        #endregion

        #region 🆕 Hierarchy Validation

        public async Task<bool> CanHaveParentAsync(int permissionId, int? parentPermissionId)
        {
            if (!parentPermissionId.HasValue) return true;

            var permission = await GetPermissionByIdAsync(permissionId);
            var parentPermission = await GetPermissionByIdAsync(parentPermissionId.Value);

            if (permission == null || parentPermission == null) return false;

            // Check same permission type
            if (permission.PermissionTypeId != parentPermission.PermissionTypeId) return false;

            // Check if parent can have children
            if (!parentPermission.CanHaveChildren) return false;

            // Check for circular reference
            return !await WouldCreateCircularReferenceAsync(permissionId, parentPermissionId);
        }

        public async Task<bool> WouldCreateCircularReferenceAsync(int permissionId, int? parentPermissionId)
        {
            if (!parentPermissionId.HasValue) return false;

            // Check if the new parent is a descendant of the current permission
            var descendants = await GetPermissionDescendantsAsync(permissionId);
            return descendants.Any(d => d.Id == parentPermissionId.Value);
        }

        public async Task<PermissionHierarchyValidationResult> ValidateHierarchyAsync()
        {
            var result = new PermissionHierarchyValidationResult { IsValid = true };

            try
            {
                // Check for orphaned permissions (parent doesn't exist)
                var orphanedPermissions = await _context.Permissions
                    .Where(p => p.ParentPermissionId.HasValue)
                    .Where(p => !_context.Permissions.Any(parent => parent.Id == p.ParentPermissionId))
                    .Include(p => p.PermissionType)
                    .ToListAsync();

                foreach (var orphaned in orphanedPermissions)
                {
                    result.OrphanedPermissions.Add(new OrphanedPermissionViewModel
                    {
                        Id = orphaned.Id,
                        Name = orphaned.Name,
                        DisplayName = orphaned.DisplayName ?? orphaned.Name,
                        PermissionTypeId = orphaned.PermissionTypeId,
                        PermissionTypeName = orphaned.PermissionType.Name,
                        ParentPermissionId = orphaned.ParentPermissionId,
                        Issue = "Parent permission does not exist"
                    });
                }

                // Check for incorrect hierarchy paths
                var invalidPathPermissions = await _context.Permissions
                    .Where(p => p.HierarchyPath == null || p.HierarchyPath == "")
                    .ToListAsync();

                foreach (var invalid in invalidPathPermissions)
                {
                    result.Warnings.Add($"Permission '{invalid.Name}' has invalid hierarchy path");
                }

                // Check for incorrect levels
                var allPermissions = await _context.Permissions
                    .Include(p => p.ParentPermission)
                    .ToListAsync();

                foreach (var permission in allPermissions)
                {
                    var expectedLevel = await CalculateExpectedLevel(permission.Id);
                    if (permission.Level != expectedLevel)
                    {
                        result.Warnings.Add($"Permission '{permission.Name}' has incorrect level. Expected: {expectedLevel}, Actual: {permission.Level}");
                    }
                }

                // Detect circular references
                await DetectCircularReferencesAsync(result);

                result.IsValid = result.Errors.Count == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating permission hierarchy");
                result.Errors.Add("An error occurred during validation");
                result.IsValid = false;
            }

            return result;
        }

        private async Task<int> CalculateExpectedLevel(int permissionId)
        {
            int level = 0;
            var currentId = permissionId;

            while (true)
            {
                var permission = await _context.Permissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == currentId);

                if (permission?.ParentPermissionId == null) break;

                level++;
                currentId = permission.ParentPermissionId.Value;

                // Prevent infinite loops
                if (level > 10) break;
            }

            return level;
        }

        private async Task DetectCircularReferencesAsync(PermissionHierarchyValidationResult result)
        {
            var visited = new HashSet<int>();
            var recursionStack = new HashSet<int>();

            var allPermissions = await _context.Permissions
                .Where(p => p.ParentPermissionId.HasValue)
                .ToListAsync();

            foreach (var permission in allPermissions)
            {
                if (!visited.Contains(permission.Id))
                {
                    await DetectCircularReferenceRecursive(permission.Id, visited, recursionStack, result);
                }
            }
        }

        private async Task DetectCircularReferenceRecursive(int permissionId, HashSet<int> visited,
            HashSet<int> recursionStack, PermissionHierarchyValidationResult result)
        {
            visited.Add(permissionId);
            recursionStack.Add(permissionId);

            var permission = await _context.Permissions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == permissionId);

            if (permission?.ParentPermissionId.HasValue == true)
            {
                var parentId = permission.ParentPermissionId.Value;

                if (recursionStack.Contains(parentId))
                {
                    // Found circular reference
                    var circularPath = string.Join(" → ", recursionStack.Select(id => $"Permission {id}"));
                    result.CircularReferences.Add(new CircularReferenceViewModel
                    {
                        PermissionIds = recursionStack.ToList(),
                        CircularPath = circularPath
                    });
                    result.Errors.Add($"Circular reference detected: {circularPath}");
                }
                else if (!visited.Contains(parentId))
                {
                    await DetectCircularReferenceRecursive(parentId, visited, recursionStack, result);
                }
            }

            recursionStack.Remove(permissionId);
        }

        public async Task<bool> IsValidHierarchyMoveAsync(int permissionId, int? newParentId)
        {
            if (!newParentId.HasValue) return true;

            var permission = await GetPermissionByIdAsync(permissionId);
            var newParent = await GetPermissionByIdAsync(newParentId.Value);

            if (permission == null || newParent == null) return false;

            // Same permission type check
            if (permission.PermissionTypeId != newParent.PermissionTypeId) return false;

            // Can't move to itself
            if (permissionId == newParentId.Value) return false;

            // Check if new parent can have children
            if (!newParent.CanHaveChildren) return false;

            // Check circular reference
            return !await WouldCreateCircularReferenceAsync(permissionId, newParentId);
        }

        #endregion




        #region 🆕 Tree Data Generation

        public async Task<List<TreeNodeViewModel>> GetPermissionTreeDataAsync()
        {
            try
            {
                var permissionTypes = await _context.PermissionTypes
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.SortOrder)
                    .ThenBy(pt => pt.Name)
                    .ToListAsync();

                var treeNodes = new List<TreeNodeViewModel>();

                foreach (var permissionType in permissionTypes)
                {
                    var typeNode = new TreeNodeViewModel
                    {
                        Id = $"type_{permissionType.Id}",
                        Text = permissionType.Name,
                        Type = "permission_type",
                        Icon = permissionType.Icon ?? "fas fa-folder",
                        Color = permissionType.Color ?? "primary",
                        Level = 0,
                        CanHaveChildren = true,
                        Data = new
                        {
                            id = permissionType.Id,
                            name = permissionType.Name,
                            description = permissionType.Description,
                            icon = permissionType.Icon,
                            color = permissionType.Color,
                            isSystemType = permissionType.IsSystemType,
                            sortOrder = permissionType.SortOrder
                        },
                        State = new { opened = true },
                        Children = new List<TreeNodeViewModel>()
                    };

                    // Get root permissions for this type
                    var rootPermissions = await GetRootPermissionsAsync(permissionType.Id);

                    foreach (var permission in rootPermissions)
                    {
                        var permissionNode = await BuildPermissionNodeAsync(permission);
                        typeNode.Children.Add(permissionNode);
                    }

                    treeNodes.Add(typeNode);
                }

                return treeNodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission tree data");
                return new List<TreeNodeViewModel>();
            }
        }

        public async Task<List<TreeNodeViewModel>> GetPermissionTypeTreeAsync(int permissionTypeId)
        {
            try
            {
                var rootPermissions = await GetRootPermissionsAsync(permissionTypeId);
                var treeNodes = new List<TreeNodeViewModel>();

                foreach (var permission in rootPermissions)
                {
                    var node = await BuildPermissionNodeAsync(permission);
                    treeNodes.Add(node);
                }

                return treeNodes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission type tree for type {TypeId}", permissionTypeId);
                return new List<TreeNodeViewModel>();
            }
        }

        private async Task<TreeNodeViewModel> BuildPermissionNodeAsync(Permission permission)
        {
            var node = new TreeNodeViewModel
            {
                Id = $"perm_{permission.Id}",
                Text = permission.DisplayName ?? permission.Name,
                Type = permission.Level == 0 ? "permission" : "permission_child",
                Icon = permission.Icon ?? "fas fa-key",
                Color = permission.Color ?? "success",
                Level = permission.Level + 1, // +1 because PermissionType is level 0
                CanHaveChildren = permission.CanHaveChildren,
                ParentId = permission.ParentPermissionId?.ToString(),
                HierarchyPath = permission.HierarchyPath,
                Data = new
                {
                    id = permission.Id,
                    name = permission.Name,
                    displayName = permission.DisplayName,
                    description = permission.Description,
                    scope = permission.Scope.ToString(),
                    action = permission.Action.ToString(),
                    permissionTypeId = permission.PermissionTypeId,
                    parentPermissionId = permission.ParentPermissionId,
                    level = permission.Level,
                    isSystemPermission = permission.IsSystemPermission,
                    canHaveChildren = permission.CanHaveChildren,
                    sortOrder = permission.SortOrder,
                    hierarchyPath = permission.HierarchyPath
                },
                Children = new List<TreeNodeViewModel>()
            };

            // Recursively load child permissions
            var childPermissions = await GetChildPermissionsAsync(permission.Id);
            foreach (var child in childPermissions)
            {
                var childNode = await BuildPermissionNodeAsync(child);
                node.Children.Add(childNode);
            }

            return node;
        }

        #endregion

        #region 🆕 Search and Filtering

        public async Task<List<Permission>> SearchPermissionsAsync(string searchTerm, int? permissionTypeId = null, bool includeInactive = false)
        {
            try
            {
                var query = _context.Permissions
                    .Include(p => p.PermissionType)
                    .Include(p => p.ParentPermission)
                    .AsQueryable();

                if (!includeInactive)
                {
                    query = query.Where(p => p.IsActive);
                }

                if (permissionTypeId.HasValue)
                {
                    query = query.Where(p => p.PermissionTypeId == permissionTypeId.Value);
                }

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    query = query.Where(p =>
                        p.Name.ToLower().Contains(term) ||
                        (p.DisplayName != null && p.DisplayName.ToLower().Contains(term)) ||
                        (p.Description != null && p.Description.ToLower().Contains(term)));
                }

                return await query
                    .OrderBy(p => p.PermissionTypeId)
                    .ThenBy(p => p.Level)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching permissions with term: {SearchTerm}", searchTerm);
                return new List<Permission>();
            }
        }

        public async Task<List<Permission>> GetPermissionsByLevelAsync(int level, int? permissionTypeId = null)
        {
            try
            {
                var query = _context.Permissions
                    .Include(p => p.PermissionType)
                    .Where(p => p.Level == level && p.IsActive);

                if (permissionTypeId.HasValue)
                {
                    query = query.Where(p => p.PermissionTypeId == permissionTypeId.Value);
                }

                return await query
                    .OrderBy(p => p.PermissionTypeId)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions by level {Level}", level);
                return new List<Permission>();
            }
        }

        public async Task<List<Permission>> GetPermissionsAtDepthAsync(int depth, int? permissionTypeId = null)
        {
            // This is the same as GetPermissionsByLevelAsync since Level represents depth
            return await GetPermissionsByLevelAsync(depth, permissionTypeId);
        }

        #endregion

        #region 🆕 Export and Import

        public async Task<string> ExportHierarchyToJsonAsync(int? permissionTypeId = null)
        {
            try
            {
                var query = _context.Permissions
                    .Include(p => p.PermissionType)
                    .Include(p => p.ParentPermission)
                    .AsQueryable();

                if (permissionTypeId.HasValue)
                {
                    query = query.Where(p => p.PermissionTypeId == permissionTypeId.Value);
                }

                var permissions = await query
                    .OrderBy(p => p.Level)
                    .ThenBy(p => p.SortOrder)
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.DisplayName,
                        p.Description,
                        p.PermissionTypeId,
                        PermissionTypeName = p.PermissionType.Name,
                        p.ParentPermissionId,
                        ParentPermissionName = p.ParentPermission != null ? p.ParentPermission.Name : null,
                        p.Level,
                        p.HierarchyPath,
                        p.SortOrder,
                        p.Icon,
                        p.Color,
                        p.Scope,
                        p.Action,
                        p.CanHaveChildren,
                        p.IsActive,
                        p.IsSystemPermission,
                        p.CreatedAt,
                        p.CreatedBy
                    })
                    .ToListAsync();

                return JsonSerializer.Serialize(permissions, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting hierarchy to JSON");
                return "[]";
            }
        }

        public async Task<string> ExportHierarchyToCsvAsync(int? permissionTypeId = null)
        {
            try
            {
                var query = _context.Permissions
                    .Include(p => p.PermissionType)
                    .Include(p => p.ParentPermission)
                    .AsQueryable();

                if (permissionTypeId.HasValue)
                {
                    query = query.Where(p => p.PermissionTypeId == permissionTypeId.Value);
                }

                var permissions = await query
                    .OrderBy(p => p.Level)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync();

                var csv = new System.Text.StringBuilder();

                // CSV Header
                csv.AppendLine("Id,Name,DisplayName,Description,PermissionType,ParentPermission,Level,HierarchyPath,SortOrder,Scope,Action,CanHaveChildren,IsActive,IsSystemPermission");

                // CSV Data
                foreach (var permission in permissions)
                {
                    csv.AppendLine($"{permission.Id}," +
                                 $"\"{permission.Name}\"," +
                                 $"\"{permission.DisplayName ?? ""}\"," +
                                 $"\"{permission.Description ?? ""}\"," +
                                 $"\"{permission.PermissionType.Name}\"," +
                                 $"\"{permission.ParentPermission?.Name ?? ""}\"," +
                                 $"{permission.Level}," +
                                 $"\"{permission.HierarchyPath ?? ""}\"," +
                                 $"{permission.SortOrder}," +
                                 $"{permission.Scope}," +
                                 $"{permission.Action}," +
                                 $"{permission.CanHaveChildren}," +
                                 $"{permission.IsActive}," +
                                 $"{permission.IsSystemPermission}");
                }

                return csv.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting hierarchy to CSV");
                return "";
            }
        }

        public async Task<bool> ImportHierarchyFromJsonAsync(string jsonData)
        {
            try
            {
                // This is a complex operation that would need careful implementation
                // to handle validation, conflicts, and maintain referential integrity
                // For now, return a placeholder
                _logger.LogWarning("Import hierarchy from JSON not yet implemented");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing hierarchy from JSON");
                return false;
            }
        }

        #endregion

        #region 🆕 Permission Cloning

        public async Task<Permission?> ClonePermissionAsync(int sourcePermissionId, int? newParentId = null, string? newName = null)
        {
            try
            {
                var sourcePermission = await GetPermissionByIdAsync(sourcePermissionId);
                if (sourcePermission == null) return null;

                var clonedPermission = new Permission
                {
                    Name = newName ?? $"{sourcePermission.Name}_Copy",
                    DisplayName = sourcePermission.DisplayName != null ? $"{sourcePermission.DisplayName} (Copy)" : null,
                    Description = sourcePermission.Description,
                    PermissionTypeId = sourcePermission.PermissionTypeId,
                    ParentPermissionId = newParentId ?? sourcePermission.ParentPermissionId,
                    Scope = sourcePermission.Scope,
                    Action = sourcePermission.Action,
                    SortOrder = await GetNextSortOrderAsync(newParentId, sourcePermission.PermissionTypeId),
                    Icon = sourcePermission.Icon,
                    Color = sourcePermission.Color,
                    CanHaveChildren = sourcePermission.CanHaveChildren,
                    IsActive = true,
                    IsSystemPermission = false, // Cloned permissions are never system permissions
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = "System" // You might want to get the current user
                };

                await UpdatePermissionLevelAndPathAsync(clonedPermission);

                _context.Permissions.Add(clonedPermission);
                await _context.SaveChangesAsync();

                // Update hierarchy path with the actual ID
                clonedPermission.UpdateHierarchyPath();
                await _context.SaveChangesAsync();

                return clonedPermission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning permission {SourceId}", sourcePermissionId);
                return null;
            }
        }

        public async Task<bool> ClonePermissionTreeAsync(int sourcePermissionId, int? newParentId = null)
        {
            try
            {
                var sourcePermission = await GetPermissionWithChildrenAsync(sourcePermissionId);
                if (sourcePermission == null) return false;

                // Clone the root permission
                var clonedRoot = await ClonePermissionAsync(sourcePermissionId, newParentId);
                if (clonedRoot == null) return false;

                // Recursively clone children
                await CloneChildrenRecursiveAsync(sourcePermission.Children, clonedRoot.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cloning permission tree {SourceId}", sourcePermissionId);
                return false;
            }
        }

        private async Task CloneChildrenRecursiveAsync(ICollection<Permission> children, int newParentId)
        {
            foreach (var child in children)
            {
                var clonedChild = await ClonePermissionAsync(child.Id, newParentId);
                if (clonedChild != null && child.Children.Any())
                {
                    await CloneChildrenRecursiveAsync(child.Children, clonedChild.Id);
                }
            }
        }

        #endregion

        // Areas/Security/Services/Implementations/PermissionService.cs
        // Add these missing methods to your existing PermissionService class

        #region 🆕 Hierarchy Operations (Missing Methods)

        public async Task<bool> MovePermissionAsync(int permissionId, int? newParentPermissionId, int newSortOrder)
        {
            try
            {
                if (!await IsValidHierarchyMoveAsync(permissionId, newParentPermissionId))
                    return false;

                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission == null) return false;

                var oldParentId = permission.ParentPermissionId;

                // Update permission
                permission.ParentPermissionId = newParentPermissionId;
                permission.SortOrder = newSortOrder;
                permission.UpdatedAt = DateTime.UtcNow.AddHours(3);

                // Recalculate level and hierarchy path
                await UpdatePermissionLevelAndPathAsync(permission);

                // Update children levels and paths recursively
                await UpdateChildrenHierarchyAsync(permission.Id);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Permission {PermissionId} moved from parent {OldParent} to {NewParent}",
                    permissionId, oldParentId, newParentPermissionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving permission {PermissionId}", permissionId);
                return false;
            }
        }

        public async Task<bool> MovePermissionUpAsync(int permissionId)
        {
            try
            {
                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission == null) return false;

                // Get sibling above
                var siblingAbove = await _context.Permissions
                    .Where(p => p.ParentPermissionId == permission.ParentPermissionId &&
                               p.SortOrder < permission.SortOrder)
                    .OrderByDescending(p => p.SortOrder)
                    .FirstOrDefaultAsync();

                if (siblingAbove == null) return false;

                // Swap sort orders
                var tempOrder = permission.SortOrder;
                permission.SortOrder = siblingAbove.SortOrder;
                siblingAbove.SortOrder = tempOrder;

                permission.UpdatedAt = DateTime.UtcNow.AddHours(3);
                siblingAbove.UpdatedAt = DateTime.UtcNow.AddHours(3);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving permission {PermissionId} up", permissionId);
                return false;
            }
        }

        public async Task<bool> MovePermissionDownAsync(int permissionId)
        {
            try
            {
                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission == null) return false;

                // Get sibling below
                var siblingBelow = await _context.Permissions
                    .Where(p => p.ParentPermissionId == permission.ParentPermissionId &&
                               p.SortOrder > permission.SortOrder)
                    .OrderBy(p => p.SortOrder)
                    .FirstOrDefaultAsync();

                if (siblingBelow == null) return false;

                // Swap sort orders
                var tempOrder = permission.SortOrder;
                permission.SortOrder = siblingBelow.SortOrder;
                siblingBelow.SortOrder = tempOrder;

                permission.UpdatedAt = DateTime.UtcNow.AddHours(3);
                siblingBelow.UpdatedAt = DateTime.UtcNow.AddHours(3);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving permission {PermissionId} down", permissionId);
                return false;
            }
        }

        public async Task<bool> UpdateSortOrderAsync(int permissionId, int newSortOrder)
        {
            try
            {
                var permission = await GetPermissionByIdAsync(permissionId);
                if (permission == null) return false;

                permission.SortOrder = newSortOrder;
                permission.UpdatedAt = DateTime.UtcNow.AddHours(3);

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating sort order for permission {PermissionId}", permissionId);
                return false;
            }
        }

        public async Task<bool> ReorderChildrenAsync(int parentPermissionId, List<int> childIds)
        {
            try
            {
                for (int i = 0; i < childIds.Count; i++)
                {
                    var child = await _context.Permissions.FindAsync(childIds[i]);
                    if (child != null && child.ParentPermissionId == parentPermissionId)
                    {
                        child.SortOrder = i;
                        child.UpdatedAt = DateTime.UtcNow.AddHours(3);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reordering children for parent {ParentId}", parentPermissionId);
                return false;
            }
        }

        #endregion

        #region 🆕 Hierarchy Utilities (Missing Methods)

        public async Task<int> GetMaxDepthAsync(int permissionTypeId)
        {
            return await _context.Permissions
                .Where(p => p.PermissionTypeId == permissionTypeId)
                .MaxAsync(p => (int?)p.Level) ?? 0;
        }

        public async Task<int> GetPermissionDepthAsync(int permissionId)
        {
            var permission = await GetPermissionByIdAsync(permissionId);
            return permission?.Level ?? 0;
        }

        public async Task<string> GenerateHierarchyPathAsync(int permissionId)
        {
            var ancestors = await GetPermissionAncestorsAsync(permissionId);
            var path = string.Join("/", ancestors.Select(a => a.Id.ToString()));

            if (!string.IsNullOrEmpty(path))
                path += "/" + permissionId;
            else
                path = permissionId.ToString();

            return path;
        }

        public async Task<bool> UpdateAllHierarchyPathsAsync()
        {
            try
            {
                var allPermissions = await _context.Permissions
                    .OrderBy(p => p.Level)
                    .ToListAsync();

                foreach (var permission in allPermissions)
                {
                    await UpdatePermissionLevelAndPathAsync(permission);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating all hierarchy paths");
                return false;
            }
        }

        public async Task<List<Permission>> GetOrphanedPermissionsAsync()
        {
            return await _context.Permissions
                .Where(p => p.ParentPermissionId.HasValue)
                .Where(p => !_context.Permissions.Any(parent => parent.Id == p.ParentPermissionId))
                .Include(p => p.PermissionType)
                .ToListAsync();
        }

        #endregion

        #region 🆕 Bulk Operations (Missing Methods)

        public async Task<bool> BulkMovePermissionsAsync(List<int> permissionIds, int? newParentId)
        {
            try
            {
                var permissions = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    if (await IsValidHierarchyMoveAsync(permission.Id, newParentId))
                    {
                        permission.ParentPermissionId = newParentId;
                        await UpdatePermissionLevelAndPathAsync(permission);
                        await UpdateChildrenHierarchyAsync(permission.Id);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk moving permissions");
                return false;
            }
        }

        public async Task<bool> BulkDeletePermissionsAsync(List<int> permissionIds, bool includeChildren = false)
        {
            try
            {
                var allIdsToDelete = new HashSet<int>(permissionIds);

                if (includeChildren)
                {
                    foreach (var id in permissionIds)
                    {
                        var descendants = await GetPermissionDescendantsAsync(id);
                        foreach (var descendant in descendants)
                        {
                            allIdsToDelete.Add(descendant.Id);
                        }
                    }
                }

                var permissionsToDelete = await _context.Permissions
                    .Where(p => allIdsToDelete.Contains(p.Id))
                    .ToListAsync();

                _context.Permissions.RemoveRange(permissionsToDelete);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk deleting permissions");
                return false;
            }
        }

        public async Task<bool> BulkActivatePermissionsAsync(List<int> permissionIds, bool includeChildren = false)
        {
            return await BulkUpdateStatusAsync(permissionIds, true, includeChildren);
        }

        public async Task<bool> BulkDeactivatePermissionsAsync(List<int> permissionIds, bool includeChildren = false)
        {
            return await BulkUpdateStatusAsync(permissionIds, false, includeChildren);
        }

        #endregion

        #region 🆕 Statistics and Analytics (Missing Methods)

        public async Task<PermissionStatisticsViewModel> GetHierarchyStatisticsAsync()
        {
            try
            {
                var allPermissions = await _context.Permissions.ToListAsync();
                var allTypes = await _context.PermissionTypes.ToListAsync();

                var rootPermissions = allPermissions.Where(p => p.ParentPermissionId == null).ToList();
                var childPermissions = allPermissions.Where(p => p.ParentPermissionId != null).ToList();

                return new PermissionStatisticsViewModel
                {
                    TotalPermissionTypes = allTypes.Count,
                    TotalPermissions = allPermissions.Count,
                    SystemPermissionTypes = allTypes.Count(t => t.IsSystemType),
                    SystemPermissions = allPermissions.Count(p => p.IsSystemPermission),
                    CustomPermissionTypes = allTypes.Count(t => !t.IsSystemType),
                    CustomPermissions = allPermissions.Count(p => !p.IsSystemPermission),
                    RootPermissions = rootPermissions.Count,
                    ChildPermissions = childPermissions.Count,
                    MaxDepth = allPermissions.Any() ? allPermissions.Max(p => p.Level) : 0,
                    OrphanedPermissions = (await GetOrphanedPermissionsAsync()).Count,
                    PermissionsWithChildren = allPermissions.Count(p => allPermissions.Any(c => c.ParentPermissionId == p.Id)),
                    LeafPermissions = allPermissions.Count(p => !allPermissions.Any(c => c.ParentPermissionId == p.Id)),
                    AverageChildrenPerPermission = allPermissions.Any()
                        ? Math.Round(childPermissions.Count / (double)rootPermissions.Count, 2)
                        : 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hierarchy statistics");
                return new PermissionStatisticsViewModel();
            }
        }

        public async Task<HierarchyAnalyticsViewModel> GetHierarchyAnalyticsAsync()
        {
            try
            {
                var validation = await ValidateHierarchyAsync();
                var stats = await GetHierarchyStatisticsAsync();
                var typeDepths = await GetPermissionTypeDepthsAsync();

                return new HierarchyAnalyticsViewModel
                {
                    DeepestLevel = stats.MaxDepth,
                    OrphanedCount = stats.OrphanedPermissions,
                    ChildlessCount = stats.LeafPermissions,
                    TypeDepths = typeDepths,
                    ValidationWarnings = validation.Warnings,
                    ValidationErrors = validation.Errors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hierarchy analytics");
                return new HierarchyAnalyticsViewModel();
            }
        }

        public async Task<List<PermissionTypeDepthViewModel>> GetPermissionTypeDepthsAsync()
        {
            try
            {
                var result = new List<PermissionTypeDepthViewModel>();
                var permissionTypes = await _context.PermissionTypes.ToListAsync();

                foreach (var type in permissionTypes)
                {
                    var permissions = await _context.Permissions
                        .Where(p => p.PermissionTypeId == type.Id)
                        .ToListAsync();

                    var rootCount = permissions.Count(p => p.ParentPermissionId == null);
                    var childCount = permissions.Count(p => p.ParentPermissionId != null);
                    var maxDepth = permissions.Any() ? permissions.Max(p => p.Level) : 0;

                    result.Add(new PermissionTypeDepthViewModel
                    {
                        PermissionTypeId = type.Id,
                        PermissionTypeName = type.Name,
                        MaxDepth = maxDepth,
                        TotalPermissions = permissions.Count,
                        RootPermissions = rootCount,
                        ChildPermissions = childCount
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission type depths");
                return new List<PermissionTypeDepthViewModel>();
            }
        }

        #endregion

        #region 🆕 Private Helper Methods (Missing)

        private async Task UpdatePermissionLevelAndPathAsync(Permission permission)
        {
            if (permission.ParentPermissionId == null)
            {
                permission.Level = 0;
                permission.HierarchyPath = permission.Id.ToString();
            }
            else
            {
                var parent = await _context.Permissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == permission.ParentPermissionId);

                if (parent != null)
                {
                    permission.Level = parent.Level + 1;
                    permission.HierarchyPath = string.IsNullOrEmpty(parent.HierarchyPath)
                        ? $"{parent.Id}/{permission.Id}"
                        : $"{parent.HierarchyPath}/{permission.Id}";
                }
            }
        }

        private async Task UpdateChildrenHierarchyAsync(int parentId)
        {
            var children = await _context.Permissions
                .Where(p => p.ParentPermissionId == parentId)
                .ToListAsync();

            foreach (var child in children)
            {
                await UpdatePermissionLevelAndPathAsync(child);
                await UpdateChildrenHierarchyAsync(child.Id);
            }
        }

        private async Task<bool> BulkUpdateStatusAsync(List<int> permissionIds, bool isActive, bool includeChildren)
        {
            try
            {
                var allIdsToUpdate = new HashSet<int>(permissionIds);

                if (includeChildren)
                {
                    foreach (var id in permissionIds)
                    {
                        var descendants = await GetPermissionDescendantsAsync(id);
                        foreach (var descendant in descendants)
                        {
                            allIdsToUpdate.Add(descendant.Id);
                        }
                    }
                }

                var permissionsToUpdate = await _context.Permissions
                    .Where(p => allIdsToUpdate.Contains(p.Id))
                    .ToListAsync();

                foreach (var permission in permissionsToUpdate)
                {
                    permission.IsActive = isActive;
                    permission.UpdatedAt = DateTime.UtcNow.AddHours(3);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk updating permission status");
                return false;
            }
        }

        private async Task<int> GetNextSortOrderAsync(int? parentPermissionId, int permissionTypeId)
        {
            var query = _context.Permissions
                .Where(p => p.PermissionTypeId == permissionTypeId && p.ParentPermissionId == parentPermissionId);

            var maxSortOrder = await query.MaxAsync(p => (int?)p.SortOrder) ?? 0;
            return maxSortOrder + 1;
        }

        #endregion

        #region Existing Methods - Keep your current implementations

        public async Task<Permission?> GetPermissionByNameAsync(string name)
        {
            return await _context.Permissions
                .Include(p => p.PermissionType)
                .Include(p => p.ParentPermission)
                .FirstOrDefaultAsync(p => p.Name == name);
        }

        public async Task<bool> CreatePermissionAsync(Permission permission)
        {
            try
            {
                // Calculate level and hierarchy path before saving
                await UpdatePermissionLevelAndPathAsync(permission);

                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                // Update hierarchy path with actual ID
                permission.UpdateHierarchyPath();
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission {PermissionName}", permission.Name);
                return false;
            }
        }



        public async Task<bool> UpdatePermissionAsync(Permission permission)
        {
            try
            {
                permission.UpdatedAt = DateTime.UtcNow.AddHours(3);

                // Recalculate hierarchy if parent changed
                await UpdatePermissionLevelAndPathAsync(permission);

                _context.Permissions.Update(permission);
                await _context.SaveChangesAsync();

                // Update children if necessary
                await UpdateChildrenHierarchyAsync(permission.Id);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission {PermissionId}", permission.Id);
                return false;
            }
        }



        public async Task<bool> UpdatePermissionFromRequestAsync(EditPermissionRequest request, string updatedBy)
        {
            try
            {
                // 1. Get existing permission
                var permission = await GetPermissionByIdAsync(request.Id);
                if (permission == null)
                {
                    _logger.LogWarning("Permission with ID {Id} not found", request.Id);
                    return false;
                }

                if (permission.IsSystemPermission)
                {
                    _logger.LogWarning("Attempted to edit system permission {Id}", request.Id);
                    return false;
                }

                // 2. Validate permission type
                if (request.PermissionTypeId > 0 && request.PermissionTypeId != permission.PermissionTypeId)
                {
                    var permissionType = await _context.PermissionTypes
                        .FirstOrDefaultAsync(pt => pt.Id == request.PermissionTypeId);

                    if (permissionType == null)
                    {
                        _logger.LogWarning("Permission type with ID {TypeId} not found", request.PermissionTypeId);
                        return false;
                    }
                }

                // 3. Validate parent permission
                if (request.ParentPermissionId.HasValue)
                {
                    var parentPermission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.Id == request.ParentPermissionId.Value);

                    if (parentPermission == null)
                    {
                        _logger.LogWarning("Parent permission with ID {ParentId} not found", request.ParentPermissionId.Value);
                        return false;
                    }

                    if (parentPermission.PermissionTypeId != request.PermissionTypeId)
                    {
                        _logger.LogWarning("Parent permission type mismatch. Parent: {ParentType}, Requested: {RequestType}",
                            parentPermission.PermissionTypeId, request.PermissionTypeId);
                        return false;
                    }

                    if (!parentPermission.CanHaveChildren)
                    {
                        _logger.LogWarning("Parent permission {ParentId} cannot have children", request.ParentPermissionId.Value);
                        return false;
                    }

                    if (parentPermission.Id == request.Id)
                    {
                        _logger.LogWarning("Permission {Id} cannot be its own parent", request.Id);
                        return false;
                    }

                    // Check for circular references
                    if (await WouldCreateCircularReferenceAsync(request.Id, request.ParentPermissionId.Value))
                    {
                        _logger.LogWarning("Moving permission {Id} to parent {ParentId} would create circular reference",
                            request.Id, request.ParentPermissionId.Value);
                        return false;
                    }
                }

                // 4. Validate name uniqueness within permission type
                var typeIdToCheck = request.PermissionTypeId > 0 ? request.PermissionTypeId : permission.PermissionTypeId;
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.PermissionTypeId == typeIdToCheck &&
                                             p.Name.ToLower() == request.Name.ToLower() &&
                                             p.Id != request.Id);

                if (existingPermission != null)
                {
                    _logger.LogWarning("Permission name '{Name}' already exists in type {TypeId}", request.Name, typeIdToCheck);
                    return false;
                }

                // 5. Apply changes
                permission.Name = request.Name.Trim();
                permission.DisplayName = request.DisplayName?.Trim();
                permission.Description = request.Description?.Trim();
                permission.Icon = request.Icon?.Trim() ?? "fas fa-key";
                permission.Color = request.Color?.Trim() ?? "primary";
                permission.CanHaveChildren = request.CanHaveChildren;

                // Update PermissionTypeId if provided
                if (request.PermissionTypeId > 0)
                {
                    permission.PermissionTypeId = request.PermissionTypeId;
                }

                // Update ParentPermissionId (can be null for root permissions)
                permission.ParentPermissionId = request.ParentPermissionId;

                // Update enums
                if (Enum.TryParse<PermissionScope>(request.Scope, out var scopeEnum))
                    permission.Scope = scopeEnum;

                if (Enum.TryParse<PermissionAction>(request.Action, out var actionEnum))
                    permission.Action = actionEnum;

                // Update hierarchy information
                await UpdatePermissionHierarchy(permission);

                // Update audit fields
                permission.UpdatedAt = DateTime.UtcNow.AddHours(3);
                permission.UpdatedBy = updatedBy;

                // 6. Save using your existing method
                return await UpdatePermissionAsync(permission);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permission from request {Id}", request.Id);
                return false;
            }
        }




        private async Task UpdatePermissionHierarchy(Permission permission)
        {
            try
            {
                if (permission.ParentPermissionId.HasValue)
                {
                    var parentPermission = await _context.Permissions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == permission.ParentPermissionId.Value);

                    if (parentPermission != null)
                    {
                        permission.Level = parentPermission.Level + 1;
                        // Update hierarchy path if your system uses it
                        if (!string.IsNullOrEmpty(parentPermission.HierarchyPath))
                        {
                            permission.HierarchyPath = $"{parentPermission.HierarchyPath}/{permission.Name}";
                        }
                        else
                        {
                            permission.HierarchyPath = permission.Name;
                        }
                    }
                }
                else
                {
                    // Root permission
                    permission.Level = 0;
                    permission.ParentPermissionId = null;
                    permission.HierarchyPath = permission.Name;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating hierarchy for permission {Id}", permission.Id);
                // Don't throw - let the update continue
            }
        }




        public async Task<bool> DeletePermissionAsync(int id)
        {
            try
            {
                var permission = await GetPermissionWithChildrenAsync(id);
                if (permission == null) return false;

                // Check if permission has children
                if (permission.Children.Any())
                {
                    _logger.LogWarning("Cannot delete permission {PermissionId} because it has children", id);
                    return false;
                }

                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission {PermissionId}", id);
                return false;
            }
        }

        public async Task<bool> PermissionExistsAsync(string name, int permissionTypeId, int? excludeId = null)
        {
            var query = _context.Permissions
                .Where(p => p.Name.ToLower() == name.ToLower() && p.PermissionTypeId == permissionTypeId);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }

        #endregion
    }
}