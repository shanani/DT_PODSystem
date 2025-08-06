// Areas/Security/Services/Interfaces/IPermissionService.cs (Updated)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.ViewModels;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface IPermissionService
    {
        // Existing methods
        Task<List<Permission>> GetAllPermissionsAsync();
        Task<List<Permission>> GetPermissionsByTypeAsync(string permissionTypeName);
        Task<Permission?> GetPermissionByIdAsync(int id);
        Task<Permission?> GetPermissionByNameAsync(string name);
        Task<bool> CreatePermissionAsync(Permission permission);
        Task<bool> UpdatePermissionAsync(Permission request);
        Task<bool> UpdatePermissionFromRequestAsync(EditPermissionRequest request, string updatedBy);

        Task<bool> DeletePermissionAsync(int id);
        Task<bool> PermissionExistsAsync(string name, int permissionTypeId, int? excludeId = null);

        // 🆕 HIERARCHICAL PERMISSION METHODS
        Task<List<Permission>> GetRootPermissionsAsync(int permissionTypeId);
        Task<List<Permission>> GetChildPermissionsAsync(int parentPermissionId);
        Task<List<Permission>> GetPermissionHierarchyAsync(int permissionTypeId);
        Task<Permission?> GetPermissionWithChildrenAsync(int id);
        Task<List<Permission>> GetPermissionAncestorsAsync(int permissionId);
        Task<List<Permission>> GetPermissionDescendantsAsync(int permissionId);

        // 🆕 HIERARCHY VALIDATION
        Task<bool> CanHaveParentAsync(int permissionId, int? parentPermissionId);
        Task<bool> WouldCreateCircularReferenceAsync(int permissionId, int? parentPermissionId);
        Task<PermissionHierarchyValidationResult> ValidateHierarchyAsync();
        Task<bool> IsValidHierarchyMoveAsync(int permissionId, int? newParentId);

        // 🆕 HIERARCHY OPERATIONS
        Task<bool> MovePermissionAsync(int permissionId, int? newParentPermissionId, int newSortOrder);
        Task<bool> MovePermissionUpAsync(int permissionId);
        Task<bool> MovePermissionDownAsync(int permissionId);
        Task<bool> UpdateSortOrderAsync(int permissionId, int newSortOrder);
        Task<bool> ReorderChildrenAsync(int parentPermissionId, List<int> childIds);

        // 🆕 HIERARCHY UTILITIES
        Task<int> GetMaxDepthAsync(int permissionTypeId);
        Task<int> GetPermissionDepthAsync(int permissionId);
        Task<string> GenerateHierarchyPathAsync(int permissionId);
        Task<bool> UpdateAllHierarchyPathsAsync();
        Task<List<Permission>> GetOrphanedPermissionsAsync();

        // 🆕 BULK OPERATIONS
        Task<bool> BulkMovePermissionsAsync(List<int> permissionIds, int? newParentId);
        Task<bool> BulkDeletePermissionsAsync(List<int> permissionIds, bool includeChildren = false);
        Task<bool> BulkActivatePermissionsAsync(List<int> permissionIds, bool includeChildren = false);
        Task<bool> BulkDeactivatePermissionsAsync(List<int> permissionIds, bool includeChildren = false);

        // 🆕 STATISTICS AND ANALYTICS
        Task<PermissionStatisticsViewModel> GetHierarchyStatisticsAsync();
        Task<HierarchyAnalyticsViewModel> GetHierarchyAnalyticsAsync();
        Task<List<PermissionTypeDepthViewModel>> GetPermissionTypeDepthsAsync();

        // 🆕 TREE DATA GENERATION
        Task<List<TreeNodeViewModel>> GetPermissionTreeDataAsync();
        Task<List<TreeNodeViewModel>> GetPermissionTypeTreeAsync(int permissionTypeId);

        // 🆕 SEARCH AND FILTERING
        Task<List<Permission>> SearchPermissionsAsync(string searchTerm, int? permissionTypeId = null, bool includeInactive = false);
        Task<List<Permission>> GetPermissionsByLevelAsync(int level, int? permissionTypeId = null);
        Task<List<Permission>> GetPermissionsAtDepthAsync(int depth, int? permissionTypeId = null);

        // 🆕 EXPORT AND IMPORT
        Task<string> ExportHierarchyToJsonAsync(int? permissionTypeId = null);
        Task<string> ExportHierarchyToCsvAsync(int? permissionTypeId = null);
        Task<bool> ImportHierarchyFromJsonAsync(string jsonData);

        // 🆕 PERMISSION CLONING
        Task<Permission?> ClonePermissionAsync(int sourcePermissionId, int? newParentId = null, string? newName = null);
        Task<bool> ClonePermissionTreeAsync(int sourcePermissionId, int? newParentId = null);
    }
}