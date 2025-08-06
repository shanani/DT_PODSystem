using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Filters;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    [Authorize]
    [RequireSuperAdmin]
    public class SuperAdminController : Controller
    {
        private readonly SecurityDbContext _securityContext;

        public SuperAdminController(SecurityDbContext securityContext)
        {
            _securityContext = securityContext;
        }

        // GET: Security/SuperAdmin
        public async Task<IActionResult> Index()
        {
            try
            {
                var model = new SuperAdminDashboardViewModel
                {
                    IsProductionMode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production",
                    SystemStatistics = await GetSystemStatistics(),
                    PermissionTypes = await GetPermissionTypeSummary(),
                    RecentSystemActions = await GetRecentSystemActions(),
                    CanManagePermissionTypes = true,
                    CanManagePermissions = true,
                    CanAccessSystemSettings = true,
                    CanViewAuditLogs = true
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData.Error("Error loading super admin dashboard: " + ex.Message);
                return View(new SuperAdminDashboardViewModel
                {
                    IsProductionMode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production",
                    SystemStatistics = new SecurityStatisticsDto(),
                    PermissionTypes = new List<PermissionTypeDto>(),
                    RecentSystemActions = new List<SecurityAuditDto>(),
                    CanManagePermissionTypes = true,
                    CanManagePermissions = true,
                    CanAccessSystemSettings = true,
                    CanViewAuditLogs = true
                });
            }
        }


        // GET: Security/SuperAdmin/CreatePermissionType
        public IActionResult CreatePermissionType()
        {
            var model = new PermissionTypeViewModel
            {
                Icon = "fas fa-cog",
                Color = "primary",
                SortOrder = 1,
                IsActive = true
            };
            return View(model);
        }




        // AJAX Actions
        [HttpPost]
        public async Task<IActionResult> TogglePermissionTypeStatus(int id)
        {
            try
            {
                var permissionType = await _securityContext.PermissionTypes
                    .FirstOrDefaultAsync(pt => pt.Id == id);

                if (permissionType == null)
                {
                    return Json(new { success = false, message = "Permission type not found." });
                }

                if (permissionType.IsSystemType)
                {
                    return Json(new { success = false, message = "Cannot modify system permission types." });
                }

                permissionType.IsActive = !permissionType.IsActive;
                permissionType.UpdatedAt = DateTime.UtcNow.AddHours(3);
                permissionType.UpdatedBy = User.Identity.Name ?? "SuperAdmin";

                await _securityContext.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Permission type {(permissionType.IsActive ? "activated" : "deactivated")} successfully!",
                    isActive = permissionType.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error updating permission type: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSystemHealth()
        {
            try
            {
                var health = new
                {
                    DatabaseConnection = await CheckDatabaseConnection(),
                    PermissionTypesCount = await _securityContext.PermissionTypes.CountAsync(),
                    PermissionsCount = await _securityContext.Permissions.CountAsync(),
                    SystemUptime = DateTime.UtcNow.AddHours(3).Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime),
                    LastCheck = DateTime.UtcNow.AddHours(3)
                };

                return Json(new { success = true, data = health });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper Methods
        private async Task<SecurityStatisticsDto> GetSystemStatistics()
        {
            try
            {
                // Get all statistics with separate queries for better performance and error handling
                var statistics = new SecurityStatisticsDto();

                // Users statistics
                try
                {
                    if (_securityContext.SecurityUsers != null)
                    {
                        statistics.TotalUsers = await _securityContext.SecurityUsers.CountAsync();
                        statistics.ActiveUsers = await _securityContext.SecurityUsers.CountAsync(u => u.IsActive);
                    }
                }
                catch { /* Ignore if SecurityUsers table doesn't exist */ }

                // Roles statistics
                try
                {
                    if (_securityContext.SecurityRoles != null)
                    {
                        statistics.TotalRoles = await _securityContext.SecurityRoles.CountAsync();
                        statistics.ActiveRoles = await _securityContext.SecurityRoles.CountAsync(r => r.IsActive);
                    }
                }
                catch { /* Ignore if SecurityRoles table doesn't exist */ }

                // Permission Types statistics
                statistics.TotalPermissionTypes = await _securityContext.PermissionTypes.CountAsync();
                statistics.ActivePermissionTypes = await _securityContext.PermissionTypes.CountAsync(pt => pt.IsActive);
                statistics.SystemPermissionTypes = await _securityContext.PermissionTypes.CountAsync(pt => pt.IsSystemType);
                statistics.CustomPermissionTypes = await _securityContext.PermissionTypes.CountAsync(pt => !pt.IsSystemType);

                // Permissions statistics
                statistics.TotalPermissions = await _securityContext.Permissions.CountAsync();
                statistics.ActivePermissions = await _securityContext.Permissions.CountAsync(p => p.IsActive);
                statistics.SystemPermissions = await _securityContext.Permissions.CountAsync(p => p.IsSystemPermission);
                statistics.CustomPermissions = await _securityContext.Permissions.CountAsync(p => !p.IsSystemPermission);

                return statistics;
            }
            catch (Exception ex)
            {
                // Return default statistics if database queries fail
                return new SecurityStatisticsDto
                {
                    TotalUsers = 0,
                    ActiveUsers = 0,
                    TotalRoles = 0,
                    ActiveRoles = 0,
                    TotalPermissionTypes = 0,
                    ActivePermissionTypes = 0,
                    SystemPermissionTypes = 0,
                    CustomPermissionTypes = 0,
                    TotalPermissions = 0,
                    ActivePermissions = 0,
                    SystemPermissions = 0,
                    CustomPermissions = 0
                };
            }
        }

        private async Task<List<PermissionTypeDto>> GetPermissionTypeSummary()
        {
            try
            {
                // Step 1: Get raw data from database with includes - ORDER FIRST
                var permissionTypesData = await _securityContext.PermissionTypes
                    .Include(pt => pt.Permissions)
                    .OrderBy(pt => pt.SortOrder)
                    .ThenBy(pt => pt.Name)
                    .ToListAsync();

                // Step 2: Project to DTO in memory (client-side evaluation)
                var result = permissionTypesData.Select(pt => new PermissionTypeDto
                {
                    Id = pt.Id,
                    Name = pt.Name,
                    Description = pt.Description,
                    Icon = pt.Icon,
                    Color = pt.Color,
                    PermissionCount = pt.Permissions?.Count ?? 0,
                    PermissionsCount = pt.Permissions?.Count ?? 0, // Both properties for compatibility
                    IsActive = pt.IsActive,
                    IsSystemType = pt.IsSystemType,
                    SortOrder = pt.SortOrder,
                    CreatedAt = pt.CreatedAt,
                    UpdatedAt = pt.UpdatedAt,
                    CreatedBy = pt.CreatedBy,
                    UpdatedBy = pt.UpdatedBy
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                // Log error and return empty list as fallback
                return new List<PermissionTypeDto>();
            }
        }

        private async Task<List<SecurityAuditDto>> GetRecentSystemActions()
        {
            // Since SecurityAuditLogs table doesn't exist in current SecurityDbContext,
            // we'll return mock data for now. In a real scenario, you would:
            // 1. Add SecurityAuditLog entity to SecurityDbContext
            // 2. Implement audit logging throughout the application
            // 3. Then use real data here

            return GetMockAuditData();
        }

        private List<SecurityAuditDto> GetMockAuditData()
        {
            return new List<SecurityAuditDto>
            {
                new SecurityAuditDto
                {
                    Id = 1,
                    ActionType = "Create",
                    Description = "Created new permission type 'Portal Management'",
                    EntityType = "PermissionType",
                    EntityId = 1,
                    UserId = "admin",
                    UserName = "Administrator",
                    Timestamp = DateTime.UtcNow.AddHours(3).AddMinutes(-15),
                    IsSuccessful = true
                },
                new SecurityAuditDto
                {
                    Id = 2,
                    ActionType = "Update",
                    Description = "Updated user role assignments",
                    EntityType = "User",
                    EntityId = 2,
                    UserId = "admin",
                    UserName = "Administrator",
                    Timestamp = DateTime.UtcNow.AddHours(3).AddHours(-2),
                    IsSuccessful = true
                },
                new SecurityAuditDto
                {
                    Id = 3,
                    ActionType = "Delete",
                    Description = "Removed inactive permission",
                    EntityType = "Permission",
                    EntityId = 15,
                    UserId = "admin",
                    UserName = "Administrator",
                    Timestamp = DateTime.UtcNow.AddHours(3).AddHours(-4),
                    IsSuccessful = true
                },
                new SecurityAuditDto
                {
                    Id = 4,
                    ActionType = "Login",
                    Description = "User login successful",
                    EntityType = "Authentication",
                    EntityId = null,
                    UserId = "user123",
                    UserName = "John Doe",
                    Timestamp = DateTime.UtcNow.AddHours(3).AddMinutes(-30),
                    IsSuccessful = true
                },
                new SecurityAuditDto
                {
                    Id = 5,
                    ActionType = "Create",
                    Description = "Created new security role",
                    EntityType = "Role",
                    EntityId = 3,
                    UserId = "admin",
                    UserName = "Administrator",
                    Timestamp = DateTime.UtcNow.AddHours(3).AddDays(-1),
                    IsSuccessful = true
                }
            };
        }

        private async Task PopulatePermissionDropdowns(PermissionFormViewModel model)
        {
            var permissionTypes = await _securityContext.PermissionTypes
                .Where(pt => pt.IsActive)
                .OrderBy(pt => pt.Name)
                .ToListAsync();

            ViewBag.PermissionTypes = new SelectList(permissionTypes, "Id", "Name", model.PermissionTypeId);
        }

        private async Task<bool> CheckDatabaseConnection()
        {
            try
            {
                await _securityContext.Database.CanConnectAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}