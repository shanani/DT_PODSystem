// Areas/Security/Controllers/PermissionsController.cs (Complete with missing methods)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Filters;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    [Authorize]
    [RequireSuperAdmin]
    public class PermissionsController : Controller
    {
        private readonly IPermissionService _permissionService;
        private readonly SecurityDbContext _context;
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(
            IPermissionService permissionService,
            SecurityDbContext context,
            ILogger<PermissionsController> logger)
        {
            _permissionService = permissionService;
            _context = context;
            _logger = logger;
        }


        // Add this method to the PermissionsController in the "Data Retrieval Methods" region

        [HttpGet]
        public async Task<IActionResult> GetPermissionDetails(int id)
        {
            try
            {
                var permission = await _context.Permissions
                    .Include(p => p.PermissionType)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (permission == null)
                {
                    return Json(new { success = false, message = "Permission not found." });
                }

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = permission.Id,
                        name = permission.Name,
                        description = permission.Description,
                        permissionTypeId = permission.PermissionTypeId,
                        parentPermissionId = permission.ParentPermissionId,
                        scope = permission.Scope.ToString(),
                        action = permission.Action.ToString(),
                        icon = permission.Icon,
                        color = permission.Color,
                        canHaveChildren = permission.CanHaveChildren,
                        sortOrder = permission.SortOrder,
                        isActive = permission.IsActive,
                        isSystemPermission = permission.IsSystemPermission,
                        hierarchyPath = permission.HierarchyPath,
                        permissionTypeName = permission.PermissionType?.Name
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission details for ID: {Id}", id);
                return Json(new { success = false, message = "Error loading permission details." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
        {
            try
            {
                // 🔧 Validate basic input
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Permission name is required." });
                }

                if (request.PermissionTypeId <= 0)
                {
                    return Json(new { success = false, message = "Valid permission type is required." });
                }

                // 🔧 Check if permission type exists
                var permissionType = await _context.PermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Id == request.PermissionTypeId);

                if (permissionType == null)
                {
                    return Json(new { success = false, message = "Permission type not found." });
                }

                // Check if name is unique within the permission type
                var existingPermission = await _context.Permissions
                    .FirstOrDefaultAsync(p => p.PermissionTypeId == request.PermissionTypeId &&
                                             p.Name.ToLower() == request.Name.ToLower());

                if (existingPermission != null)
                {
                    return Json(new { success = false, message = "A permission with this name already exists in the selected type." });
                }

                // 🆕 Handle parent permission validation for hierarchy
                Permission? parentPermission = null;
                int level = 0;

                if (request.ParentPermissionId.HasValue && request.ParentPermissionId.Value > 0)
                {
                    parentPermission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.Id == request.ParentPermissionId.Value);

                    if (parentPermission == null)
                    {
                        return Json(new { success = false, message = "Parent permission not found." });
                    }

                    if (!parentPermission.CanHaveChildren)
                    {
                        return Json(new { success = false, message = "Selected parent permission cannot have children." });
                    }

                    if (parentPermission.PermissionTypeId != request.PermissionTypeId)
                    {
                        return Json(new { success = false, message = "Parent permission must be from the same permission type." });
                    }

                    level = parentPermission.Level + 1;
                }

                // 🆕 Create permission with proper hierarchy support
                var permission = new Permission
                {
                    Name = request.Name.Trim(),
                    DisplayName = !string.IsNullOrWhiteSpace(request.DisplayName) ? request.DisplayName.Trim() : request.Name.Trim(),
                    Description = request.Description?.Trim() ?? string.Empty,
                    PermissionTypeId = request.PermissionTypeId,
                    ParentPermissionId = request.ParentPermissionId, // 🔧 FIX: Use the actual ParentPermissionId
                    Level = level,
                    CanHaveChildren = request.CanHaveChildren ?? true,
                    Scope = Enum.TryParse<PermissionScope>(request.Scope, out var scopeEnum) ? scopeEnum : PermissionScope.Global,
                    Action = Enum.TryParse<PermissionAction>(request.Action, out var actionEnum) ? actionEnum : PermissionAction.Read,
                    SortOrder = await GetNextPermissionSortOrderAsync(request.ParentPermissionId, request.PermissionTypeId),
                    Icon = !string.IsNullOrWhiteSpace(request.Icon) ? request.Icon.Trim() : "fas fa-key",
                    Color = !string.IsNullOrWhiteSpace(request.Color) ? request.Color.Trim() : "primary",
                    IsActive = true,
                    IsSystemPermission = false,
                    HierarchyPath = string.Empty, // Will be updated after save
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = User.Identity?.Name ?? "System",
                    UpdatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedBy = User.Identity?.Name ?? "System"
                };

                // Add to context
                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                // 🔧 Update hierarchy path after getting the ID
                if (parentPermission != null)
                {
                    permission.HierarchyPath = string.IsNullOrEmpty(parentPermission.HierarchyPath)
                        ? $"{parentPermission.Id}/{permission.Id}"
                        : $"{parentPermission.HierarchyPath}/{permission.Id}";
                }
                else
                {
                    permission.HierarchyPath = permission.Id.ToString();
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Permission created successfully!",
                    data = new
                    {
                        id = permission.Id,
                        name = permission.Name,
                        displayName = permission.DisplayName,
                        parentPermissionId = permission.ParentPermissionId,
                        level = permission.Level
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission: {@Request}", request);
                return Json(new
                {
                    success = false,
                    message = "Error creating permission: " + ex.Message
                });
            }
        }

        // 🆕 Helper method to get next sort order
        private async Task<int> GetNextPermissionSortOrderAsync(int? parentPermissionId, int permissionTypeId)
        {
            try
            {
                if (parentPermissionId.HasValue)
                {
                    // Get max sort order for children of this parent
                    var maxSortOrder = await _context.Permissions
                        .Where(p => p.ParentPermissionId == parentPermissionId.Value)
                        .MaxAsync(p => (int?)p.SortOrder) ?? 0;
                    return maxSortOrder + 1;
                }
                else
                {
                    // Get max sort order for root permissions in this type
                    var maxSortOrder = await _context.Permissions
                        .Where(p => p.PermissionTypeId == permissionTypeId && p.ParentPermissionId == null)
                        .MaxAsync(p => (int?)p.SortOrder) ?? 0;
                    return maxSortOrder + 1;
                }
            }
            catch
            {
                return 1; // Default fallback
            }
        }


        #region Main Views

        // GET: /Security/Permissions
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new PermissionTreeViewModel
                {
                    Title = "Permission Management",
                    Description = "Manage permission types and their hierarchical permissions structure",
                    TreeData = await GetTreeDataAsync(),
                    Statistics = await GetStatisticsAsync()
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading permission manager");
                TempData["Error"] = "Error loading permission manager";
                return RedirectToAction("Index", "Home", new { area = "" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetParentPermissions(int permissionTypeId, int? excludePermissionId = null)
        {
            try
            {
                var permissions = await _context.Permissions
                    .Where(p => p.PermissionTypeId == permissionTypeId
                               && p.IsActive
                               && (!excludePermissionId.HasValue || p.Id != excludePermissionId.Value))
                    .OrderBy(p => p.HierarchyPath ?? p.Name)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.Name,
                        displayName = p.DisplayName ?? p.Name,
                        level = p.Level,
                        hierarchyPath = p.HierarchyPath,
                        parentPermissionId = p.ParentPermissionId
                    })
                    .ToListAsync();

                return Json(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting parent permissions for type {TypeId}", permissionTypeId);
                return Json(new List<object>());
            }
        }


        #endregion

        #region 🆕 Permission Type Management

        [HttpPost]
        public async Task<IActionResult> CreatePermissionType([FromBody] CreatePermissionTypeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Json(new { success = false, message = "Permission type name is required." });
                }

                // Check if name already exists
                var existingType = await _context.PermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Name.ToLower() == request.Name.ToLower());

                if (existingType != null)
                {
                    return Json(new { success = false, message = "A permission type with this name already exists." });
                }

                var permissionType = new PermissionType
                {
                    Name = request.Name.Trim(),
                    Description = request.Description?.Trim(),
                    Icon = request.Icon?.Trim() ?? "fas fa-folder",
                    Color = request.Color?.Trim() ?? "primary",
                    SortOrder = await GetNextPermissionTypeSortOrderAsync(),
                    IsActive = true,
                    IsSystemType = false,
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.PermissionTypes.Add(permissionType);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Permission type created successfully!",
                    permissionType = new
                    {
                        id = permissionType.Id,
                        name = permissionType.Name,
                        description = permissionType.Description,
                        icon = permissionType.Icon,
                        color = permissionType.Color
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating permission type");
                return Json(new { success = false, message = "Error creating permission type: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EditPermissionType([FromBody] EditPermissionTypeRequest request)
        {
            try
            {
                var permissionType = await _context.PermissionTypes.FindAsync(request.Id);
                if (permissionType == null)
                {
                    return Json(new { success = false, message = "Permission type not found." });
                }

                if (permissionType.IsSystemType)
                {
                    return Json(new { success = false, message = "Cannot edit system permission types." });
                }

                // Check if name already exists (excluding current)
                var existingType = await _context.PermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Name.ToLower() == request.Name.ToLower() && pt.Id != request.Id);

                if (existingType != null)
                {
                    return Json(new { success = false, message = "A permission type with this name already exists." });
                }

                permissionType.Name = request.Name.Trim();
                permissionType.Description = request.Description?.Trim();
                permissionType.Icon = request.Icon?.Trim() ?? "fas fa-folder";
                permissionType.Color = request.Color?.Trim() ?? "primary";
                permissionType.UpdatedAt = DateTime.UtcNow.AddHours(3);
                permissionType.UpdatedBy = User.Identity?.Name ?? "System";

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Permission type updated successfully!",
                    permissionType = new
                    {
                        id = permissionType.Id,
                        name = permissionType.Name,
                        description = permissionType.Description,
                        icon = permissionType.Icon,
                        color = permissionType.Color
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing permission type {Id}", request.Id);
                return Json(new { success = false, message = "Error updating permission type: " + ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeletePermissionType([FromBody] DeletePermissionTypeRequest request)
        {
            try
            {
                var permissionType = await _context.PermissionTypes
                    .Include(pt => pt.Permissions)
                    .FirstOrDefaultAsync(pt => pt.Id == request.Id);

                if (permissionType == null)
                {
                    return Json(new { success = false, message = "Permission type not found." });
                }

                if (permissionType.IsSystemType)
                {
                    return Json(new { success = false, message = "Cannot delete system permission types." });
                }

                // Check if has permissions
                if (permissionType.Permissions.Any())
                {
                    return Json(new { success = false, message = "Cannot delete permission type that contains permissions. Please delete all permissions first." });
                }

                _context.PermissionTypes.Remove(permissionType);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Permission type deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission type {Id}", request.Id);
                return Json(new { success = false, message = "Error deleting permission type: " + ex.Message });
            }
        }

        #endregion

        #region 🆕 Permission Management



        [HttpPost]
        public async Task<IActionResult> EditPermission([FromBody] EditPermissionRequest request)
        {
            try
            {
                var result = await _permissionService.UpdatePermissionFromRequestAsync(request, User.Identity?.Name ?? "System");

                if (result)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Permission updated successfully!",
                        permission = new
                        {
                            id = request.Id,
                            name = request.Name,
                            description = request.Description,
                            permissionTypeId = request.PermissionTypeId,
                            parentPermissionId = request.ParentPermissionId
                        }
                    });
                }

                return Json(new
                {
                    success = false,
                    message = "Failed to update permission. Please check the data and try again."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing permission {Id}", request.Id);
                return Json(new { success = false, message = "An unexpected error occurred while updating the permission." });
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeletePermission([FromBody] DeletePermissionRequest request)
        {
            try
            {
                var permission = await _permissionService.GetPermissionWithChildrenAsync(request.Id);
                if (permission == null)
                {
                    return Json(new { success = false, message = "Permission not found." });
                }

                if (permission.IsSystemPermission)
                {
                    return Json(new { success = false, message = "Cannot delete system permissions." });
                }

                if (permission.Children.Any())
                {
                    return Json(new { success = false, message = "Cannot delete permission that has child permissions. Please delete child permissions first." });
                }

                await _permissionService.DeletePermissionAsync(request.Id);

                return Json(new { success = true, message = "Permission deleted successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting permission {Id}", request.Id);
                return Json(new { success = false, message = "Error deleting permission: " + ex.Message });
            }
        }

        #endregion

        #region 🆕 Permission Movement Operations

        [HttpPost]
        public async Task<IActionResult> MovePermission([FromBody] MovePermissionRequest request)
        {
            try
            {
                bool result = false;

                switch (request.Direction.ToLower())
                {
                    case "up":
                        result = await _permissionService.MovePermissionUpAsync(request.PermissionId);
                        break;
                    case "down":
                        result = await _permissionService.MovePermissionDownAsync(request.PermissionId);
                        break;
                    case "to-parent":
                        result = await _permissionService.MovePermissionAsync(request.PermissionId, request.NewParentPermissionId, request.NewSortOrder);
                        break;
                }

                if (result)
                {
                    return Json(new { success = true, message = "Permission moved successfully!" });
                }
                else
                {
                    return Json(new { success = false, message = "Failed to move permission." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving permission {Id}", request.PermissionId);
                return Json(new { success = false, message = "Error moving permission: " + ex.Message });
            }
        }

        #endregion

        #region 🆕 Data Retrieval Methods

        [HttpGet]
        public async Task<IActionResult> GetTreeData()
        {
            try
            {
                var treeData = await GetTreeDataAsync();
                return Json(new { success = true, data = treeData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tree data");
                return Json(new { success = false, message = "Error loading tree data" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissionTypes()
        {
            try
            {
                var permissionTypes = await _context.PermissionTypes
                    .Where(pt => pt.IsActive)
                    .OrderBy(pt => pt.SortOrder)
                    .ThenBy(pt => pt.Name)
                    .Select(pt => new
                    {
                        id = pt.Id,
                        name = pt.Name,
                        icon = pt.Icon,
                        color = pt.Color
                    })
                    .ToListAsync();

                return Json(permissionTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission types");
                return Json(new List<object>());
            }
        }


        #endregion

        #region Private Helper Methods

        private async Task<List<TreeNodeViewModel>> GetTreeDataAsync()
        {
            return await _permissionService.GetPermissionTreeDataAsync();
        }

        private async Task<PermissionStatisticsViewModel> GetStatisticsAsync()
        {
            return await _permissionService.GetHierarchyStatisticsAsync();
        }

        private async Task<int> GetNextPermissionTypeSortOrderAsync()
        {
            var maxSortOrder = await _context.PermissionTypes
                .MaxAsync(pt => (int?)pt.SortOrder) ?? 0;
            return maxSortOrder + 1;
        }



        #endregion
    }

    #region 🆕 Request Models

    public class CreatePermissionTypeRequest
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Icon { get; set; } = "fas fa-folder";
        public string Color { get; set; } = "primary";
    }

    public class EditPermissionTypeRequest
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string Icon { get; set; } = "fas fa-folder";
        public string Color { get; set; } = "primary";
    }

    public class DeletePermissionTypeRequest
    {
        public int Id { get; set; }
    }



    public class DeletePermissionRequest
    {
        public int Id { get; set; }
    }

    #endregion
}