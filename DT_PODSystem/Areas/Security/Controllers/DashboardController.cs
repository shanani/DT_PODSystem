// Areas/Security/Controllers/DashboardController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Filters;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    [Authorize]
    [RequireSuperAdmin]
    public class DashboardController : Controller
    {
        private readonly SecurityDbContext _securityContext;

        public DashboardController(SecurityDbContext securityContext)
        {
            _securityContext = securityContext;
        }

        // GET: Security/Dashboard
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = await CreateDashboardViewModel();
                ViewData["Title"] = "Security Dashboard";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading security dashboard: " + ex.Message;
                return View(new SecurityDashboardViewModel());
            }
        }

        // AJAX: Get statistics data for charts
        [HttpGet]
        public async Task<IActionResult> GetStatisticsData()
        {
            try
            {
                var stats = await GetSecurityStatistics();
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        permissionTypes = new
                        {
                            total = stats.TotalPermissionTypes,
                            active = stats.ActivePermissionTypes,
                            utilization = stats.PermissionTypesUtilization
                        },
                        permissions = new
                        {
                            total = stats.TotalPermissions,
                            active = stats.ActivePermissions,
                            utilization = stats.PermissionsUtilization
                        },
                        roles = new
                        {
                            total = stats.TotalRoles,
                            active = stats.ActiveRoles,
                            utilization = stats.RolesUtilization
                        },
                        users = new
                        {
                            total = stats.TotalUsers,
                            withRoles = stats.UsersWithRoles,
                            percentage = stats.UsersWithRolesPercentage
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // AJAX: Get permission distribution chart data
        [HttpGet]
        public async Task<IActionResult> GetPermissionDistribution()
        {
            try
            {
                var distribution = await _securityContext.PermissionTypes
                    .Include(pt => pt.Permissions)
                    .Where(pt => pt.IsActive)
                    .Select(pt => new
                    {
                        name = pt.Name,
                        count = pt.Permissions.Count(p => p.IsActive),
                        color = pt.Color ?? "#6c757d",
                        percentage = _securityContext.Permissions.Count(p => p.IsActive) > 0 ?
                            Math.Round((double)pt.Permissions.Count(p => p.IsActive) /
                                      _securityContext.Permissions.Count(p => p.IsActive) * 100, 1) : 0
                    })
                    .OrderByDescending(x => x.count)
                    .ToListAsync();

                return Json(new { success = true, data = distribution });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // AJAX: Get role distribution data
        [HttpGet]
        public async Task<IActionResult> GetRoleDistribution()
        {
            try
            {
                var roleDistribution = await _securityContext.SecurityRoles
                    .Where(r => r.IsActive)
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        description = r.Description,
                        userCount = r.UserRoles.Count(ur => ur.IsActive),
                        permissionCount = r.RolePermissions.Count(rp => rp.IsActive && rp.IsGranted),
                        isSystemRole = r.IsSystemRole,
                        color = r.IsSystemRole ? "#dc3545" : "#007bff"
                    })
                    .OrderByDescending(x => x.userCount)
                    .ToListAsync();

                return Json(new { success = true, data = roleDistribution });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // AJAX: Get recent activity data
        [HttpGet]
        public async Task<IActionResult> GetRecentActivity()
        {
            try
            {
                var recentActivities = new List<object>();

                // Get recent permissions
                var recentPermissions = await _securityContext.Permissions
                    .Include(p => p.PermissionType)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(10)
                    .Select(p => new
                    {
                        date = p.CreatedAt,
                        activity = $"Permission '{p.DisplayName}' created",
                        type = "Permission",
                        user = p.CreatedBy ?? "System",
                        icon = "fa-key",
                        color = "#28a745"
                    })
                    .ToListAsync();

                // Get recent permission types
                var recentTypes = await _securityContext.PermissionTypes
                    .OrderByDescending(pt => pt.CreatedAt)
                    .Take(5)
                    .Select(pt => new
                    {
                        date = pt.CreatedAt,
                        activity = $"Permission Type '{pt.Name}' created",
                        type = "Type",
                        user = pt.CreatedBy ?? "System",
                        icon = "fa-folder",
                        color = "#17a2b8"
                    })
                    .ToListAsync();

                // Get recent roles
                var recentRoles = await _securityContext.SecurityRoles
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(5)
                    .Select(r => new
                    {
                        date = r.CreatedAt,
                        activity = $"Role '{r.Name}' created",
                        type = "Role",
                        user = r.CreatedBy ?? "System",
                        icon = "fa-user-shield",
                        color = "#ffc107"
                    })
                    .ToListAsync();

                // Combine and sort
                recentActivities.AddRange(recentPermissions);
                recentActivities.AddRange(recentTypes);
                recentActivities.AddRange(recentRoles);

                var sortedActivities = recentActivities
                    .OrderByDescending(x => ((dynamic)x).date)
                    .Take(15)
                    .Select(x => new
                    {
                        date = ((DateTime)((dynamic)x).date).ToString("yyyy-MM-dd HH:mm"),
                        activity = ((dynamic)x).activity,
                        type = ((dynamic)x).type,
                        user = ((dynamic)x).user,
                        icon = ((dynamic)x).icon,
                        color = ((dynamic)x).color,
                        timeAgo = GetTimeAgo((DateTime)((dynamic)x).date)
                    })
                    .ToList();

                return Json(new { success = true, data = sortedActivities });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // AJAX: Get role permissions summary
        [HttpGet]
        public async Task<IActionResult> GetRolePermissionsSummary()
        {
            try
            {
                var roleSummary = await _securityContext.SecurityRoles
                    .Where(r => r.IsActive)
                    .Select(r => new
                    {
                        roleId = r.Id,
                        roleName = r.Name,
                        description = r.Description,
                        permissionsCount = r.RolePermissions.Count(rp => rp.IsActive && rp.IsGranted),
                        usersCount = r.UserRoles.Count(ur => ur.IsActive),
                        isSystemRole = r.IsSystemRole,
                        createdAt = r.CreatedAt.ToString("MMM dd, yyyy")
                    })
                    .OrderByDescending(x => x.permissionsCount)
                    .ToListAsync();

                return Json(new { success = true, data = roleSummary });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // AJAX: Get system health metrics
        [HttpGet]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var totalEntities = await _securityContext.PermissionTypes.CountAsync() +
                                   await _securityContext.Permissions.CountAsync() +
                                   await _securityContext.SecurityRoles.CountAsync() +
                                   await _securityContext.SecurityUsers.CountAsync();

                var inactiveEntities = await _securityContext.PermissionTypes.CountAsync(pt => !pt.IsActive) +
                                      await _securityContext.Permissions.CountAsync(p => !p.IsActive) +
                                      await _securityContext.SecurityRoles.CountAsync(r => !r.IsActive);

                var usersWithoutRoles = await _securityContext.SecurityUsers.CountAsync() -
                                       await _securityContext.SecurityUserRoles
                                           .Select(ur => ur.UserId)
                                           .Distinct()
                                           .CountAsync();

                var healthScore = totalEntities > 0 ?
                    Math.Round((double)(totalEntities - inactiveEntities - usersWithoutRoles) / totalEntities * 100, 1) : 100;

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        healthScore = healthScore,
                        status = healthScore >= 90 ? "Excellent" :
                                healthScore >= 75 ? "Good" :
                                healthScore >= 60 ? "Fair" : "Poor",
                        color = healthScore >= 90 ? "#28a745" :
                               healthScore >= 75 ? "#17a2b8" :
                               healthScore >= 60 ? "#ffc107" : "#dc3545",
                        totalEntities = totalEntities,
                        activeEntities = totalEntities - inactiveEntities,
                        usersWithoutRoles = usersWithoutRoles,
                        recommendations = GetHealthRecommendations(healthScore, inactiveEntities, usersWithoutRoles)
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        // Helper method to create dashboard view model
        private async Task<SecurityDashboardViewModel> CreateDashboardViewModel()
        {
            var statistics = await GetSecurityStatistics();
            var permissionTypes = await GetActivePermissionTypes();
            var recentActivity = await GetSimpleRecentActivity();

            return new SecurityDashboardViewModel
            {
                Statistics = statistics,
                PermissionTypes = permissionTypes,
                RecentActivity = recentActivity,
                LastUpdated = DateTime.UtcNow.AddHours(3),
                CanManagePermissionTypes = true, // Set based on user permissions
                CanManagePermissions = true,
                CanManageRoles = true,
                CanManageUsers = true,
                IsSuperAdmin = Util.GetCurrentUser().IsSuperAdmin,
                IsProductionMode = true // Set based on environment
            };
        }

        // Helper method to get security statistics
        private async Task<SecurityStatisticsViewModel> GetSecurityStatistics()
        {
            try
            {
                var totalPermissionTypes = await _securityContext.PermissionTypes.CountAsync();
                var activePermissionTypes = await _securityContext.PermissionTypes.CountAsync(pt => pt.IsActive);
                var totalPermissions = await _securityContext.Permissions.CountAsync();
                var activePermissions = await _securityContext.Permissions.CountAsync(p => p.IsActive);
                var totalRoles = await _securityContext.SecurityRoles.CountAsync();
                var activeRoles = await _securityContext.SecurityRoles.CountAsync(r => r.IsActive);
                var totalUsers = await _securityContext.SecurityUsers.CountAsync();
                var usersWithRoles = await _securityContext.SecurityUserRoles
                    .Select(ur => ur.UserId)
                    .Distinct()
                    .CountAsync();

                return new SecurityStatisticsViewModel
                {
                    TotalPermissionTypes = totalPermissionTypes,
                    ActivePermissionTypes = activePermissionTypes,
                    TotalPermissions = totalPermissions,
                    ActivePermissions = activePermissions,
                    TotalRoles = totalRoles,
                    ActiveRoles = activeRoles,
                    TotalUsers = totalUsers,
                    UsersWithRoles = usersWithRoles,

                    // Calculate utilization percentages
                    PermissionTypesUtilization = totalPermissionTypes > 0 ?
                        Math.Round((double)activePermissionTypes / totalPermissionTypes * 100, 1) : 0,
                    PermissionsUtilization = totalPermissions > 0 ?
                        Math.Round((double)activePermissions / totalPermissions * 100, 1) : 0,
                    RolesUtilization = totalRoles > 0 ?
                        Math.Round((double)activeRoles / totalRoles * 100, 1) : 0,
                    UsersWithRolesPercentage = totalUsers > 0 ?
                        Math.Round((double)usersWithRoles / totalUsers * 100, 1) : 0
                };
            }
            catch (Exception ex)
            {
                // Return empty statistics in case of error
                return new SecurityStatisticsViewModel();
            }
        }

        // Helper method to get active permission types
        private async Task<List<PermissionTypeViewModel>> GetActivePermissionTypes()
        {
            try
            {
                return await _securityContext.PermissionTypes
                    .Where(pt => pt.IsActive)
                    .Select(pt => new PermissionTypeViewModel
                    {
                        Id = pt.Id,
                        Name = pt.Name,
                        Description = pt.Description,
                        Icon = pt.Icon,
                        Color = pt.Color,
                        SortOrder = pt.SortOrder,
                        IsActive = pt.IsActive,
                        PermissionCount = pt.Permissions.Count(p => p.IsActive),
                        CreatedAt = pt.CreatedAt,
                        UpdatedAt = pt.UpdatedAt,
                        CreatedBy = pt.CreatedBy,
                        UpdatedBy = pt.UpdatedBy
                    })
                    .OrderBy(pt => pt.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                return new List<PermissionTypeViewModel>();
            }
        }

        // Helper method to get simple recent activity
        private async Task<List<ActivityViewModel>> GetSimpleRecentActivity()
        {
            try
            {
                var activities = new List<ActivityViewModel>();

                // Get recent permissions
                var recentPermissions = await _securityContext.Permissions
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .Select(p => new ActivityViewModel
                    {
                        Date = p.CreatedAt.ToString("yyyy-MM-dd"),
                        Activity = $"Permission '{p.DisplayName}' created",
                        Type = "Permission",
                        User = p.CreatedBy ?? "System",
                        Timestamp = p.CreatedAt
                    })
                    .ToListAsync();

                activities.AddRange(recentPermissions);

                return activities.OrderByDescending(a => a.Timestamp).Take(10).ToList();
            }
            catch (Exception ex)
            {
                return new List<ActivityViewModel>();
            }
        }

        // Helper method to calculate time ago
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow.AddHours(3) - dateTime;

            if (timeSpan.TotalMinutes < 1) return "Just now";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes}m ago";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours}h ago";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays}d ago";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)}w ago";

            return dateTime.ToString("MMM dd, yyyy");
        }

        // Helper method to get health recommendations
        private List<string> GetHealthRecommendations(double healthScore, int inactiveEntities, int usersWithoutRoles)
        {
            var recommendations = new List<string>();

            if (healthScore < 90)
            {
                if (inactiveEntities > 0)
                    recommendations.Add($"Review and activate {inactiveEntities} inactive security entities");

                if (usersWithoutRoles > 0)
                    recommendations.Add($"Assign roles to {usersWithoutRoles} users without role assignments");

                if (healthScore < 60)
                    recommendations.Add("Consider a comprehensive security audit");
            }
            else
            {
                recommendations.Add("Security configuration is in excellent condition");
            }

            return recommendations;
        }
    }
}