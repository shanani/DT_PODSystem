
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Filters;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Implementations;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using DT_PODSystem.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    [Authorize]
    [RequireSuperAdmin]
    public class SecurityRolesController : Controller
    {
        private readonly ISecurityRoleService _roleService;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<SecurityRolesController> _logger;

        public SecurityRolesController(
            ISecurityRoleService roleService,
            IRolePermissionService rolePermissionService,
            IPermissionService permissionService,
            ILogger<SecurityRolesController> logger)
        {
            _roleService = roleService;
            _rolePermissionService = rolePermissionService;
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// Security Role details view - FIXED VERSION
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                _logger.LogInformation("Loading details for Security Role ID: {RoleId}", id);

                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    _logger.LogWarning("Security Role not found: {RoleId}", id);
                    TempData.Error("Security Role not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                // Get permissions for this role
                var assignedPermissions = await _rolePermissionService.GetRolePermissionsAsync(id);
                var permissionCount = assignedPermissions?.Count() ?? 0;

                // Get user count for this role
                var userCount = role.UserRoles?.Count(ur => ur.IsActive) ?? 0;

                var viewModel = new SecurityRoleManagementViewModel
                {
                    Id = role.Id,
                    Name = role.Name ?? "",
                    Description = role.Description ?? "",
                    IsActive = role.IsActive,
                    IsSystemRole = role.IsSystemRole,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt,
                    CreatedBy = role.CreatedBy ?? "System",
                    UpdatedBy = role.UpdatedBy ?? "System",
                    UserCount = userCount,
                    PermissionCount = permissionCount
                };

                ViewBag.Title = $"Security Role Details - {role.Name}";
                _logger.LogInformation("Successfully loaded details for role: {RoleName}", role.Name);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security role details: {RoleId}", id);
                TempData.Error("Error loading security role details.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// AJAX endpoint to get role statistics for Details page
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetRoleStatistics(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found." });
                }

                var permissionCount = await _rolePermissionService.GetRolePermissionsAsync(id);
                var userCount = role.UserRoles?.Count(ur => ur.IsActive) ?? 0;

                // Mock recent logins for demo - replace with actual data
                var recentLogins = userCount > 0 ? new Random().Next(1, userCount * 10) : 0;
                var activityScore = (userCount * 10) + (permissionCount.Count() * 5) + recentLogins;

                return Json(new
                {
                    success = true,
                    permissionCount = permissionCount.Count(),
                    userCount = userCount,
                    recentLogins = recentLogins,
                    activityScore = activityScore
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role statistics: {RoleId}", id);
                return Json(new { success = false, message = "Error loading statistics." });
            }
        }

        /// <summary>
        /// AJAX endpoint to get role permissions for Details page
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetRolePermissions(int id)
        {
            try
            {
                var permissions = await _rolePermissionService.GetRolePermissionsAsync(id);

                var permissionList = permissions.Select(p => new
                {
                    id = p.Id,
                    name = p.Name ?? "",
                    displayName = p.DisplayName ?? p.Name ?? "",
                    description = p.Description ?? "No description available",
                    icon = p.Icon ?? "fas fa-key",
                    color = p.Color ?? "primary",
                    permissionType = p.PermissionType?.Name ?? "General"
                }).ToList();

                return Json(new { success = true, data = permissionList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permissions: {RoleId}", id);
                return Json(new { success = false, message = "Error loading permissions." });
            }
        }

        /// <summary>
        /// AJAX endpoint to get users assigned to role for Details page
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetRoleUsers(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found." });
                }

                var users = role.UserRoles?
                    .Where(ur => ur.IsActive && ur.User != null)
                    .Select(ur => new
                    {
                        id = ur.User.Id,
                        code = ur.User.Code ?? "",
                        fullName = $"{ur.User.FirstName ?? ""} {ur.User.LastName ?? ""}".Trim(),
                        email = ur.User.Email ?? "",
                        department = ur.User.Department ?? "",
                        isActive = ur.User.IsActive,
                        assignedAt = ur.AssignedAt != default(DateTime) ? ur.AssignedAt.ToString("MMM dd, yyyy") : "Unknown"
                    })
                    .Take(10) // Limit to 10 for performance
                    .ToList();

                if (users == null)
                {
                    users = new List<object>().Select(x => new
                    {
                        id = 0,
                        code = "",
                        fullName = "",
                        email = "",
                        department = "",
                        isActive = false,
                        assignedAt = ""
                    }).ToList();
                }

                return Json(new { success = true, data = users });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role users: {RoleId}", id);
                return Json(new { success = false, message = "Error loading users." });
            }
        }

        /// <summary>
        /// AJAX endpoint to get recent activity for role
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetRoleActivity(int id)
        {
            try
            {
                // This is a mock implementation - replace with actual audit service
                var activities = new List<object>
                {
                    new { action = "User assigned to role", user = "admin", timeAgo = "2 hours ago", recent = true },
                    new { action = "Permission updated", user = "system", timeAgo = "1 day ago", recent = false },
                    new { action = "Role settings modified", user = "admin", timeAgo = "3 days ago", recent = false }
                };

                return Json(new { success = true, data = activities });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role activity: {RoleId}", id);
                return Json(new { success = false, message = "Error loading activity." });
            }
        }

        /// <summary>
        /// Export role report functionality
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportRoleReport(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    TempData.Error("Role not found.", popup: false);
                    return RedirectToAction(nameof(Details), new { id });
                }

                // This is a placeholder - implement actual report generation
                TempData.Success($"Report for role '{role.Name}' is being generated.", popup: false);
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting role report: {RoleId}", id);
                TempData.Error("Error generating report.", popup: false);
                return RedirectToAction(nameof(Details), new { id });
            }
        }
        /// <summary>
        /// Security Roles list with DataTables
        /// </summary>
        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Security Roles Management";
            return View();
        }


        /// <summary>
        /// AJAX endpoint to get hierarchical tree data for role permissions
        /// This method reuses the PermissionsController approach for full hierarchical support
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetPermissionTreeData(int roleId)
        {
            try
            {
                _logger.LogInformation("Loading hierarchical permission tree data for role {RoleId}", roleId);

                // Get assigned permissions for this role
                var assignedPermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
                var assignedPermissionIds = assignedPermissions.Select(p => p.Id).ToList();

                // 🎯 REUSE: Use the same hierarchical tree data approach as PermissionsController
                var treeData = await _permissionService.GetPermissionTreeDataAsync();

                // Mark assigned permissions in tree data
                MarkAssignedPermissionsInTree(treeData, assignedPermissionIds);

                _logger.LogInformation("Successfully loaded {NodeCount} tree nodes with {AssignedCount} assigned permissions",
                    CountTreeNodes(treeData), assignedPermissionIds.Count);

                return Json(new { success = true, data = treeData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission tree data for role {RoleId}", roleId);
                return Json(new { success = false, message = "Error loading permission tree data." });
            }
        }

        // Update your SecurityRolesController.SaveRolePermissions method with this debugging version

        [HttpPost]
        public async Task<JsonResult> SaveRolePermissions(int roleId, List<int> grantedPermissionIds)
        {
            try
            {
                _logger.LogInformation("📥 SaveRolePermissions called with roleId: {RoleId}, permissions: [{Permissions}]",
                    roleId, string.Join(",", grantedPermissionIds ?? new List<int>()));

                // Validate input
                if (roleId <= 0)
                {
                    _logger.LogWarning("❌ Invalid roleId: {RoleId}", roleId);
                    return Json(new { success = false, message = "Invalid role ID." });
                }

                // Check if role exists
                var role = await _roleService.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    _logger.LogWarning("❌ Role not found: {RoleId}", roleId);
                    return Json(new { success = false, message = "Role not found." });
                }

                _logger.LogInformation("✅ Role found: {RoleName}", role.Name);

                // Get current user
                var userId = Util.GetCurrentUser()?.Code;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("❌ No current user found");
                    return Json(new { success = false, message = "User authentication required." });
                }

                _logger.LogInformation("✅ Current user: {UserId}", userId);

                // Clean the permission list
                var cleanPermissionIds = (grantedPermissionIds ?? new List<int>())
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                _logger.LogInformation("🧹 Cleaned permission IDs: [{Permissions}]", string.Join(",", cleanPermissionIds));

                // Call the role permission service
                _logger.LogInformation("🔄 Calling SetRolePermissionsAsync...");

                var result = await _rolePermissionService.SetRolePermissionsAsync(
                    roleId,
                    cleanPermissionIds,
                    userId);

                _logger.LogInformation("📋 SetRolePermissionsAsync result: {Result}", result);

                if (result)
                {
                    _logger.LogInformation("✅ Role permissions updated successfully for role {RoleId}", roleId);
                    return Json(new
                    {
                        success = true,
                        message = "Role permissions updated successfully!",
                        roleId = roleId,
                        assignedCount = cleanPermissionIds.Count
                    });
                }
                else
                {
                    _logger.LogWarning("❌ SetRolePermissionsAsync returned false for role {RoleId}", roleId);
                    return Json(new { success = false, message = "Failed to update role permissions - service returned false." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Exception in SaveRolePermissions for role {RoleId}: {Message}", roleId, ex.Message);
                _logger.LogError("❌ Stack trace: {StackTrace}", ex.StackTrace);

                return Json(new
                {
                    success = false,
                    message = "An error occurred while saving permissions: " + ex.Message,
                    details = ex.ToString() // Remove this in production
                });
            }
        }

        // Also check your SetRolePermissionsAsync method - add similar logging there:

        public async Task<bool> SetRolePermissionsAsync(int roleId, List<int> permissionIds, string userId)
        {
            try
            {
                _logger.LogInformation("🔄 SetRolePermissionsAsync called with roleId: {RoleId}, permissions: [{Permissions}], userId: {UserId}",
                    roleId, string.Join(",", permissionIds ?? new List<int>()), userId);

                // Your existing implementation here...
                // Add logging at each step to see where it fails

                _logger.LogInformation("✅ SetRolePermissionsAsync completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in SetRolePermissionsAsync: {Message}", ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Mark assigned permissions in tree data recursively
        /// </summary>
        private void MarkAssignedPermissionsInTree(List<TreeNodeViewModel> treeNodes, List<int> assignedPermissionIds)
        {
            foreach (var node in treeNodes)
            {
                // Check if this is a permission node (not a permission type node)
                if ((node.Type == "permission" || node.Type == "permission_child") && node.Data != null)
                {
                    // Extract permission ID from the node data or ID
                    int permissionId = 0;

                    // Try to get permission ID from node data
                    if (node.Data is IDictionary<string, object> dataDict && dataDict.ContainsKey("permissionId"))
                    {
                        int.TryParse(dataDict["permissionId"].ToString(), out permissionId);
                    }
                    // Fallback: extract from node ID (format: "perm_123")
                    else if (node.Id.StartsWith("perm_"))
                    {
                        int.TryParse(node.Id.Substring(5), out permissionId);
                    }

                    // Mark as selected if assigned
                    if (permissionId > 0 && assignedPermissionIds.Contains(permissionId))
                    {
                        node.State = new { selected = true };
                        _logger.LogDebug("Marked permission {PermissionId} as selected in tree", permissionId);
                    }
                }

                // Recursively process children
                if (node.Children?.Any() == true)
                {
                    MarkAssignedPermissionsInTree(node.Children, assignedPermissionIds);
                }
            }
        }

        /// <summary>
        /// Count total tree nodes for logging
        /// </summary>
        private int CountTreeNodes(List<TreeNodeViewModel> treeNodes)
        {
            int count = treeNodes.Count;
            foreach (var node in treeNodes)
            {
                if (node.Children?.Any() == true)
                {
                    count += CountTreeNodes(node.Children);
                }
            }
            return count;
        }

        /// <summary>
        /// Extract permission ID from node ID string
        /// </summary>
        private int ExtractIdFromNodeId(string nodeId, string prefix)
        {
            if (string.IsNullOrEmpty(nodeId) || !nodeId.StartsWith(prefix))
                return 0;

            if (int.TryParse(nodeId.Substring(prefix.Length), out int id))
                return id;

            return 0;
        }

        /// <summary>
        /// Extract text content from HTML string
        /// </summary>
        private string ExtractTextFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            // Remove HTML tags using regex
            var textOnly = System.Text.RegularExpressions.Regex.Replace(html, @"<[^>]*>", "").Trim();
            return textOnly;
        }

        /// <summary>
        /// Extract icon class from HTML string
        /// </summary>
        private string ExtractIconFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return "fas fa-folder";

            var match = System.Text.RegularExpressions.Regex.Match(html, @"<i[^>]*class=""([^""]*(?:fa-[\w-]+)[^""]*)""");
            return match.Success ? match.Groups[1].Value : "fas fa-folder";
        }

        /// <summary>
        /// Extract color class from HTML string
        /// </summary>
        private string ExtractColorFromHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return "primary";

            var match = System.Text.RegularExpressions.Regex.Match(html, @"text-(\w+)");
            return match.Success ? match.Groups[1].Value : "primary";
        }


        /// <summary>
        /// SIMPLE UPDATE: Role Permissions - Keep existing model, reuse PermissionsController tree approach
        /// Just replace your existing RolePermissions method with this one
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RolePermissions(int roleId)
        {
            try
            {
                // Get the role
                var role = await _roleService.GetRoleByIdAsync(roleId);
                if (role == null)
                {
                    TempData.Error("Security Role not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                // Get assigned permissions for this role
                var assignedPermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
                var assignedPermissionIds = assignedPermissions.Select(p => p.Id).ToList();

                // 🎯 REUSE: Get tree data like PermissionsController, then convert to your existing model format
                var treeData = await _permissionService.GetPermissionTreeDataAsync();
                var permissionGroups = ConvertTreeDataToPermissionGroups(treeData, assignedPermissionIds);

                var viewModel = new RolePermissionAssignmentViewModel
                {
                    RoleId = roleId,
                    RoleName = role.Name,
                    RoleDescription = role.Description,
                    Permissions = permissionGroups,
                    GrantedPermissionIds = assignedPermissionIds
                };

                ViewBag.Title = $"Manage Permissions - {role.Name}";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading role permissions: {RoleId}", roleId);
                TempData.Error("Error loading role permissions.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Convert tree data from PermissionsController to your existing model format
        /// </summary>
        private List<PermissionTypeGroupViewModel> ConvertTreeDataToPermissionGroups(List<TreeNodeViewModel> treeData, List<int> assignedPermissionIds)
        {
            var permissionGroups = new List<PermissionTypeGroupViewModel>();

            foreach (var typeNode in treeData.Where(n => n.Type == "permission_type"))
            {
                var permissionType = new PermissionTypeDto
                {
                    Id = ExtractIdFromNodeId(typeNode.Id, "type_"),
                    Name = ExtractTextFromHtml(typeNode.Text),
                    Description = "",
                    Icon = ExtractIconFromHtml(typeNode.Text),
                    Color = ExtractColorFromHtml(typeNode.Text),
                    IsActive = true
                };

                var permissions = new List<PermissionAssignmentViewModel>();
                CollectPermissionsFromTree(typeNode.Children, permissions, assignedPermissionIds);

                var permissionGroup = new PermissionTypeGroupViewModel
                {
                    PermissionType = permissionType,
                    Permissions = permissions
                };

                permissionGroups.Add(permissionGroup);
            }

            return permissionGroups;
        }

        /// <summary>
        /// Recursively collect permissions from tree structure
        /// </summary>
        private void CollectPermissionsFromTree(List<TreeNodeViewModel> nodes, List<PermissionAssignmentViewModel> permissions, List<int> assignedPermissionIds)
        {
            foreach (var node in nodes)
            {
                if (node.Type == "permission" || node.Type == "permission_child")
                {
                    var permissionId = ExtractIdFromNodeId(node.Id, "perm_");
                    var permissionData = node.Data as dynamic;

                    var permission = new PermissionAssignmentViewModel
                    {
                        PermissionId = permissionId,
                        Name = permissionData?.name?.ToString() ?? ExtractTextFromHtml(node.Text),
                        DisplayName = permissionData?.displayName?.ToString() ?? ExtractTextFromHtml(node.Text),
                        Description = permissionData?.description?.ToString() ?? "",
                        IsAssigned = assignedPermissionIds.Contains(permissionId)
                    };

                    permissions.Add(permission);
                }

                // Recursively process children
                if (node.Children?.Any() == true)
                {
                    CollectPermissionsFromTree(node.Children, permissions, assignedPermissionIds);
                }
            }
        }




        /// <summary>
        /// Get tree data for AJAX - REUSING PermissionsController approach
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRolePermissionTreeData(int roleId)
        {
            try
            {
                // Get assigned permissions for this role
                var assignedPermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
                var assignedPermissionIds = assignedPermissions.Select(p => p.Id).ToList();

                // 🎯 REUSE: Use the same tree data approach as PermissionsController
                var treeData = await _permissionService.GetPermissionTreeDataAsync();

                // Mark assigned permissions in tree data
                MarkAssignedPermissionsInTree(treeData, assignedPermissionIds);

                return Json(new { success = true, data = treeData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role permission tree data: {RoleId}", roleId);
                return Json(new { success = false, message = "Error loading permission tree." });
            }
        }


        /// <summary>
        /// Get role permission statistics
        /// </summary>
        private async Task<object> GetRolePermissionStatistics(int roleId)
        {
            var assignedPermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
            var totalPermissions = await _permissionService.GetAllPermissionsAsync();

            return new
            {
                TotalPermissions = totalPermissions.Count(p => p.IsActive),
                AssignedPermissions = assignedPermissions.Count(),
                UnassignedPermissions = totalPermissions.Count(p => p.IsActive) - assignedPermissions.Count()
            };
        }


        /// <summary>
        /// Save permission changes via AJAX
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> SavePermissions(int roleId, List<int> selectedPermissionIds)
        {
            try
            {
                var userId = Util.GetCurrentUser().Code;

                var result = await _rolePermissionService.SetRolePermissionsAsync(
                    roleId,
                    selectedPermissionIds ?? new List<int>(),
                    userId);

                if (result)
                {
                    return Json(new { success = true, message = "Permissions updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update permissions." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving permissions: {RoleId}", roleId);
                return Json(new { success = false, message = "An error occurred while saving permissions." });
            }
        }

        /// <summary>
        /// Get security roles data for DataTables (AJAX)
        /// FIXED: Method name matches JavaScript call
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> GetRolesData()
        {
            try
            {
                var draw = int.Parse(Request.Form["draw"]);
                var start = int.Parse(Request.Form["start"]);
                var length = int.Parse(Request.Form["length"]);
                var searchValue = Request.Form["search[value]"];

                var roles = await _roleService.GetAllRolesAsync();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchValue))
                {
                    roles = roles.Where(r =>
                        r.Name.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                        (r.Description != null && r.Description.Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var totalRecords = roles.Count();

                // Apply pagination
                var pagedRoles = roles
                    .Skip(start)
                    .Take(length)
                    .ToList();

                var response = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedRoles.Select(role => new
                    {
                        id = role.Id,
                        name = role.Name,
                        description = role.Description ?? "No description",
                        isActive = role.IsActive,
                        isSystemRole = role.IsSystemRole,
                        userCount = role.UserRoles?.Count(ur => ur.IsActive) ?? 0,
                        createdAt = role.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        actions = "" // Will be generated by JavaScript
                    })
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security roles data");
                return Json(new
                {
                    error = "Error loading data",
                    message = ex.Message,
                    draw = Request.Form["draw"],
                    recordsTotal = 0,
                    recordsFiltered = 0,
                    data = new object[0]
                });
            }
        }

        /// <summary>
        /// Get role statistics for dashboard widgets
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetStatistics()
        {
            try
            {
                var statistics = await _roleService.GetRoleStatisticsAsync();

                // Get user count with roles
                var usersWithRoles = await GetUsersWithRolesCount();

                return Json(new
                {
                    totalRoles = statistics.GetValueOrDefault("Total", 0),
                    activeRoles = statistics.GetValueOrDefault("Active", 0),
                    systemRoles = statistics.GetValueOrDefault("System", 0),
                    usersWithRoles = usersWithRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading role statistics");
                return Json(new
                {
                    totalRoles = 0,
                    activeRoles = 0,
                    systemRoles = 0,
                    usersWithRoles = 0
                });
            }
        }

        /// <summary>
        /// Helper method to get count of users with roles
        /// </summary>
        private async Task<int> GetUsersWithRolesCount()
        {
            try
            {
                // This would need to be implemented in your service layer
                // For now, return a basic count
                var allRoles = await _roleService.GetAllRolesAsync();
                return allRoles.SelectMany(r => r.UserRoles ?? new List<SecurityUserRole>())
                              .Where(ur => ur.IsActive)
                              .Select(ur => ur.UserId)
                              .Distinct()
                              .Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users with roles count");
                return 0;
            }
        }



        /// <summary>
        /// Helper method to generate action buttons for each role
        /// </summary>
        private string GetActionButtons(int roleId, bool isActive)
        {
            var buttons = "<div class='btn-group btn-group-sm' role='group'>";

            // Details button
            buttons += $"<a href='{Url.Action("Details", new { id = roleId })}' class='btn btn-outline-info' title='View Details'>";
            buttons += "<i class='fa fa-eye'></i></a>";

            // Edit button
            buttons += $"<a href='{Url.Action("Edit", new { id = roleId })}' class='btn btn-outline-primary' title='Edit Role'>";
            buttons += "<i class='fa fa-edit'></i></a>";

            // Toggle status button
            var statusIcon = isActive ? "fa-toggle-on" : "fa-toggle-off";
            var statusTitle = isActive ? "Deactivate Role" : "Activate Role";
            buttons += $"<button type='button' class='btn btn-outline-warning' onclick='toggleStatus({roleId})' title='{statusTitle}'>";
            buttons += $"<i class='fa {statusIcon}'></i></button>";

            // Permissions button
            buttons += $"<button type='button' class='btn btn-outline-success' onclick='managePermissions({roleId})' title='Manage Permissions'>";
            buttons += "<i class='fa fa-key'></i></button>";

            buttons += "</div>";
            return buttons;
        }


        // Areas/Security/Controllers/SecurityRolesController.cs
        // UPDATE the existing Edit actions:

        /// <summary>
        /// Edit Security Role form with permission tree
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    TempData.Error("Security Role not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                // Get assigned permissions for this role
                var assignedPermissions = await _rolePermissionService.GetRolePermissionsAsync(id);
                var assignedPermissionIds = assignedPermissions.Select(p => p.Id).ToList();

                var viewModel = new SecurityRoleManagementViewModel
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    IsActive = role.IsActive,
                    IsSystemRole = role.IsSystemRole,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt,
                    CreatedBy = role.CreatedBy,
                    UpdatedBy = role.UpdatedBy,
                    SelectedPermissionIds = assignedPermissionIds,

                    // Load permission tree
                    PermissionTree = await BuildPermissionTreeForRole(id, assignedPermissionIds)
                };

                ViewBag.Title = $"Edit Security Role - {role.Name}";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security role for edit: {RoleId}", id);
                TempData.Error("Error loading security role.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Edit Security Role with permission assignments
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SecurityRoleManagementViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload permission tree on validation error
                    model.PermissionTree = await BuildPermissionTreeForRole(model.Id, model.SelectedPermissionIds ?? new List<int>());
                    TempData.Warning("Please check the data entered.", popup: false);
                    ViewBag.Title = $"Edit Security Role - {model.Name}";
                    return View(model);
                }

                var userId = Util.GetCurrentUser().Code;
                var role = await _roleService.GetRoleByIdAsync(model.Id);
                if (role == null)
                {
                    TempData.Error("Security Role not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                // Update role basic info
                role.Name = model.Name;
                role.Description = model.Description;
                role.IsActive = model.IsActive;
                var updatedRole = await _roleService.UpdateRoleAsync(role, userId);

                if (updatedRole != null)
                {
                    // Update permission assignments
                    await _rolePermissionService.SetRolePermissionsAsync(
                        model.Id,
                        model.SelectedPermissionIds ?? new List<int>(),
                        userId);

                    TempData.Success("Security Role and permissions updated successfully!", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                TempData.Error("Failed to update security role.", popup: false);
                model.PermissionTree = await BuildPermissionTreeForRole(model.Id, model.SelectedPermissionIds ?? new List<int>());
                ViewBag.Title = $"Edit Security Role - {model.Name}";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating security role: {RoleId}", model.Id);
                TempData.Error("An error occurred while updating security role.", popup: false);
                model.PermissionTree = await BuildPermissionTreeForRole(model.Id, model.SelectedPermissionIds ?? new List<int>());
                ViewBag.Title = $"Edit Security Role - {model.Name}";
                return View(model);
            }
        }




        /// <summary>
        /// Helper method to build permission tree for role assignment using existing services
        /// </summary>
        private async Task<PermissionTreeAssignmentViewModel> BuildPermissionTreeForRole(int roleId, List<int> assignedPermissionIds)
        {
            try
            {
                // Use existing permission service to get all permissions
                var allPermissions = await _permissionService.GetAllPermissionsAsync();

                // Group by permission type
                var permissionsByType = allPermissions
                    .Where(p => p.IsActive)
                    .GroupBy(p => p.PermissionType)
                    .OrderBy(g => g.Key.SortOrder)
                    .ThenBy(g => g.Key.Name);

                var permissionTreeTypes = new List<PermissionTypeAssignmentViewModel>();

                foreach (var group in permissionsByType)
                {
                    var permissionType = group.Key;
                    var permissions = group.OrderBy(p => p.Level)
                                          .ThenBy(p => p.SortOrder)
                                          .ThenBy(p => p.Name);

                    var permissionTypeViewModel = new PermissionTypeAssignmentViewModel
                    {
                        Id = permissionType.Id,
                        Name = permissionType.Name,
                        Description = permissionType.Description,
                        Icon = permissionType.Icon,
                        Color = permissionType.Color,
                        IsActive = permissionType.IsActive,
                        Permissions = permissions.Select(p => new PermissionAssignmentItemViewModel
                        {
                            Id = p.Id,
                            Name = p.Name,
                            DisplayName = p.DisplayName,
                            Description = p.Description,
                            Icon = p.Icon,
                            Color = p.Color,
                            ParentPermissionId = p.ParentPermissionId,
                            Level = p.Level,
                            HierarchyPath = p.HierarchyPath,
                            IsAssigned = assignedPermissionIds.Contains(p.Id),
                            IsSystemPermission = p.IsSystemPermission,
                            IsActive = p.IsActive
                        }).ToList()
                    };

                    permissionTreeTypes.Add(permissionTypeViewModel);
                }

                return new PermissionTreeAssignmentViewModel
                {
                    EntityId = roleId,
                    EntityType = "Role",
                    PermissionTypes = permissionTreeTypes,
                    SelectedPermissionIds = assignedPermissionIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building permission tree for role {RoleId}", roleId);
                return new PermissionTreeAssignmentViewModel
                {
                    EntityId = roleId,
                    EntityType = "Role"
                };
            }
        }

        // UPDATE existing Create action to include permission tree:
        /// <summary>
        /// Create Security Role form with permission tree
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new SecurityRoleManagementViewModel
                {
                    IsActive = true,
                    PermissionTree = await BuildPermissionTreeForRole(0, new List<int>()) // Empty for new role
                };

                ViewBag.Title = "Create Security Role";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create role form");
                TempData.Error("Error loading form.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        // UPDATE existing Create POST action to handle permissions:
        /// <summary>
        /// Create Security Role with permission assignments
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SecurityRoleManagementViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Reload permission tree on validation error
                    model.PermissionTree = await BuildPermissionTreeForRole(0, model.SelectedPermissionIds ?? new List<int>());
                    TempData.Warning("Please check the data entered.", popup: false);
                    ViewBag.Title = "Create Security Role";
                    return View(model);
                }

                var userId = Util.GetCurrentUser().Code;
                var newRole = new SecurityRole
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsActive = model.IsActive
                };

                var result = await _roleService.CreateRoleAsync(newRole, userId);

                if (result != null)
                {
                    // Assign permissions if any selected
                    if (model.SelectedPermissionIds?.Any() == true)
                    {
                        await _rolePermissionService.SetRolePermissionsAsync(
                            result.Id,
                            model.SelectedPermissionIds,
                            userId);
                    }

                    TempData.Success("Security Role created successfully!", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                TempData.Error("Failed to create security role.", popup: false);
                model.PermissionTree = await BuildPermissionTreeForRole(0, model.SelectedPermissionIds ?? new List<int>());
                ViewBag.Title = "Create Security Role";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating security role");
                TempData.Error("An error occurred while creating security role.", popup: false);
                model.PermissionTree = await BuildPermissionTreeForRole(0, model.SelectedPermissionIds ?? new List<int>());
                ViewBag.Title = "Create Security Role";
                return View(model);
            }
        }



        /// <summary>
        /// Delete confirmation
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    TempData.Error("Security Role not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new SecurityRoleManagementViewModel
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    UserCount = role.UserRoles?.Count(ur => ur.IsActive) ?? 0
                };

                ViewBag.Title = $"Delete Security Role - {role.Name}";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security role for delete: {RoleId}", id);
                TempData.Error("Error loading security role.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handle delete security role
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var result = await _roleService.DeleteRoleAsync(id);

                if (result)
                {
                    TempData.Success("Security Role deleted successfully!", popup: false);
                }
                else
                {
                    TempData.Error("Failed to delete security role.", popup: false);
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting security role: {RoleId}", id);
                TempData.Error("An error occurred while deleting security role.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Toggle role status (AJAX)
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> ToggleStatus(int id)
        {
            try
            {
                var role = await _roleService.GetRoleByIdAsync(id);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found." });
                }

                var userId = Util.GetCurrentUser().Code;
                role.IsActive = !role.IsActive;
                var result = await _roleService.UpdateRoleAsync(role, userId);

                if (result != null)
                {
                    return Json(new { success = true, message = "Role status updated successfully!" });
                }

                return Json(new { success = false, message = "Failed to update role status." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling role status: {RoleId}", id);
                return Json(new { success = false, message = "An error occurred while updating role status." });
            }
        }


    }
}