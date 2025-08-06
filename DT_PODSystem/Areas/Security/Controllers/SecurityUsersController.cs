using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using DT_PODSystem.Areas.Security.Filters;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.ViewModels;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using DT_PODSystem.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Controllers
{
    [Area("Security")]
    [Authorize]
    [RequireAdmin]
    public class SecurityUsersController : Controller
    {

        private readonly IMemoryCache _memoryCache;

        // Cache key constants (must match UserSessionMiddleware)
        private static readonly string CACHE_KEY_PREFIX = "user_session:";
        private static readonly string REFRESH_FLAG_PREFIX = "refresh_required:";
        private static readonly string PERMISSION_CACHE_PREFIX = "user_permissions:";
        private static readonly string ROLE_CACHE_PREFIX = "user_roles:";

        private readonly ILogger<SecurityUsersController> _logger;
        private readonly ISecurityUserService _userService;
        private readonly IApiADService _adService;
        private readonly ISecurityRoleService _roleService;

        public SecurityUsersController(
          ILogger<SecurityUsersController> logger,
          ISecurityUserService userService,
          IApiADService adService,
          ISecurityRoleService roleService,
          IMemoryCache memoryCache) // ADD THIS PARAMETER
        {
            _logger = logger;
            _userService = userService;
            _roleService = roleService;
            _adService = adService;
            _memoryCache = memoryCache; // ADD THIS LINE
        }


        // If you have ToggleStatus method, REPLACE it with this:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ToggleStatus(int id)
        {
            try
            {
                var currentUserId = Util.GetCurrentUser().Code;
                var targetUser = await _userService.GetUserByIdAsync(id);

                if (targetUser == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (targetUser.Code.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "You cannot modify your own status." });
                }

                var oldStatus = targetUser.IsActive;
                targetUser.IsActive = !targetUser.IsActive;
                targetUser.UpdatedAt = DateTime.UtcNow.AddHours(3);
                targetUser.UpdatedBy = currentUserId;

                var result = await _userService.UpdateUserAsync(targetUser, currentUserId);

                if (result != null)
                {
                    // 🚀 IMMEDIATE CACHE INVALIDATION
                    ClearUserCacheImmediately(targetUser.Code);

                    _logger.LogWarning("SECURITY: User {UserCode} status changed from {OldStatus} to {NewStatus} by admin {AdminCode}",
                        targetUser.Code, oldStatus, targetUser.IsActive, currentUserId);

                    var statusText = targetUser.IsActive ? "activated" : "deactivated";
                    return Json(new
                    {
                        success = true,
                        message = $"User {statusText} successfully! Changes take effect on their next request.",
                        newStatus = targetUser.IsActive
                    });
                }

                return Json(new { success = false, message = "Failed to update user status." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling status for user: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while updating user status." });
            }
        }

        // If you have ToggleAdminStatus method, REPLACE it with this:
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSuperAdmin]
        public async Task<JsonResult> ToggleAdminStatus(int id)
        {
            try
            {
                var currentUserId = Util.GetCurrentUser().Code;
                var currentUser = Util.GetCurrentUser();

                if (!currentUser.IsSuperAdmin)
                {
                    return Json(new { success = false, message = "Access denied. Super Admin privileges required." });
                }

                var targetUser = await _userService.GetUserByIdAsync(id);
                if (targetUser == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (targetUser.Code.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "You cannot modify your own admin status." });
                }

                var oldAdminStatus = targetUser.IsAdmin;
                targetUser.IsAdmin = !targetUser.IsAdmin;
                targetUser.UpdatedAt = DateTime.UtcNow.AddHours(3);
                targetUser.UpdatedBy = currentUserId;

                var result = await _userService.UpdateUserAsync(targetUser, currentUserId);

                if (result != null)
                {
                    // 🚀 CRITICAL CACHE INVALIDATION FOR ADMIN PRIVILEGE CHANGE
                    ClearUserCacheImmediately(targetUser.Code);

                    _logger.LogError("CRITICAL SECURITY: User {UserCode} admin status changed from {OldStatus} to {NewStatus} by Super Admin {AdminCode}",
                        targetUser.Code, oldAdminStatus, targetUser.IsAdmin, currentUserId);

                    var statusText = targetUser.IsAdmin ? "granted admin privileges" : "removed admin privileges";
                    return Json(new
                    {
                        success = true,
                        message = $"User {statusText} successfully! Changes take effect on their next request.",
                        newAdminStatus = targetUser.IsAdmin
                    });
                }

                return Json(new { success = false, message = "Failed to update admin status." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling admin status for user: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while updating admin status." });
            }
        }

        // If you have ToggleSuperAdminStatus method, REPLACE it with this:
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSuperAdmin]
        public async Task<JsonResult> ToggleSuperAdminStatus(int id)
        {
            try
            {
                var currentUserId = Util.GetCurrentUser().Code;
                var currentUser = Util.GetCurrentUser();

                if (!currentUser.IsSuperAdmin)
                {
                    return Json(new { success = false, message = "Access denied. Super Admin privileges required." });
                }

                var targetUser = await _userService.GetUserByIdAsync(id);
                if (targetUser == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (targetUser.Code.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "You cannot modify your own Super Admin status." });
                }

                var oldSuperAdminStatus = targetUser.IsSuperAdmin;
                var oldAdminStatus = targetUser.IsAdmin;

                var newSuperAdminStatus = !targetUser.IsSuperAdmin;
                targetUser.IsSuperAdmin = newSuperAdminStatus;

                if (newSuperAdminStatus && !targetUser.IsAdmin)
                {
                    targetUser.IsAdmin = true;
                }

                targetUser.UpdatedAt = DateTime.UtcNow.AddHours(3);
                targetUser.UpdatedBy = currentUserId;

                var result = await _userService.UpdateUserAsync(targetUser, currentUserId);

                if (result != null)
                {
                    // 🚨 HIGHEST PRIORITY CACHE INVALIDATION
                    ClearUserCacheImmediately(targetUser.Code);

                    _logger.LogError("🚨 HIGHEST SECURITY: User {UserCode} Super Admin status changed from {OldSuperAdmin} to {NewSuperAdmin} (Admin: {OldAdmin} to {NewAdmin}) by Super Admin {AdminCode}",
                        targetUser.Code, oldSuperAdminStatus, targetUser.IsSuperAdmin, oldAdminStatus, targetUser.IsAdmin, currentUserId);

                    var statusText = targetUser.IsSuperAdmin ? "granted Super Admin privileges" : "removed Super Admin privileges";
                    return Json(new
                    {
                        success = true,
                        message = $"User {statusText} successfully! Changes take effect on their next request.",
                        newSuperAdminStatus = targetUser.IsSuperAdmin,
                        newAdminStatus = targetUser.IsAdmin
                    });
                }

                return Json(new { success = false, message = "Failed to update Super Admin status." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling super admin status for user: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while updating Super Admin status." });
            }
        }

        // If you have DeleteUser method, REPLACE it with this:
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequireSuperAdmin]
        public async Task<JsonResult> DeleteUser(int id)
        {
            try
            {
                var currentUserId = Util.GetCurrentUser().Code;
                var currentUser = Util.GetCurrentUser();

                if (!currentUser.IsSuperAdmin)
                {
                    return Json(new { success = false, message = "Access denied. Super Admin privileges required." });
                }

                var targetUser = await _userService.GetUserByIdAsync(id);
                if (targetUser == null)
                {
                    return Json(new { success = false, message = "User not found." });
                }

                if (targetUser.Code.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    return Json(new { success = false, message = "You cannot delete your own account." });
                }

                var userName = targetUser.FullName ?? targetUser.Code;
                var userCode = targetUser.Code;

                var result = await _userService.DeleteUserAsync(id, currentUserId);

                if (result)
                {
                    // 🚨 USER DELETED - IMMEDIATE CACHE PURGE
                    ClearUserCacheImmediately(userCode);

                    _logger.LogError("🚨 USER DELETED: User {UserCode} ({UserName}) deleted by Super Admin {AdminCode}",
                        userCode, userName, currentUserId);

                    return Json(new
                    {
                        success = true,
                        message = $"User {userName} has been deleted successfully. All sessions invalidated immediately."
                    });
                }

                return Json(new { success = false, message = "Failed to delete user." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user: {UserId}", id);
                return Json(new { success = false, message = "An error occurred while deleting user." });
            }
        }

        // ====================================================================
        // ADD THIS NEW METHOD TO YOUR CONTROLLER (100% Compatible):
        // ====================================================================

        /// <summary>
        /// 🚀 CRITICAL: Immediately clear ALL cache layers for a user
        /// This ensures security changes take effect on their very next request
        /// </summary>
        private void ClearUserCacheImmediately(string userCode)
        {
            try
            {
                // 1. Clear UserSessionMiddleware L1 cache (30 seconds cache)
                var sessionCacheKey = $"{CACHE_KEY_PREFIX}{userCode}";
                _memoryCache.Remove(sessionCacheKey);

                // 2. Set force refresh flag for UserSessionMiddleware
                var refreshFlagKey = $"{REFRESH_FLAG_PREFIX}{userCode}";
                _memoryCache.Set(refreshFlagKey, true, TimeSpan.FromMinutes(2));

                // 3. Clear permission cache from SecurityExtensions (if it exists)
                var permissionCacheKey = $"{PERMISSION_CACHE_PREFIX}{userCode}";
                var roleCacheKey = $"{ROLE_CACHE_PREFIX}{userCode}";
                _memoryCache.Remove(permissionCacheKey);
                _memoryCache.Remove(roleCacheKey);

                _logger.LogInformation("✅ CACHE CLEARED: All cache layers invalidated for user {UserCode}", userCode);

                // 4. Console log for immediate feedback (you can remove this in production)
                Console.WriteLine($"🚀 [SECURITY] IMMEDIATE CACHE INVALIDATION: {userCode} at {DateTime.UtcNow.AddHours(3):yyyy-MM-dd HH:mm:ss}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR: Failed to clear cache for user {UserCode}", userCode);
            }
        }


        private string GetUserDisplayColumn(SecurityUser user)
        {
            // Get user initials
            var initials = GetUserInitials(user);

            // Check if user has photo
            var hasPhoto = user.Photo != null && user.Photo.Length > 0;
            var photoUrl = hasPhoto ? $"data:image/jpeg;base64,{Convert.ToBase64String(user.Photo)}" : null;

            // Create clickable avatar HTML
            var avatarHtml = hasPhoto
                ? $"<img src='{photoUrl}' class='user-table-thumb me-3 clickable-avatar' alt='{user.FullName}' onclick='viewUserDetails({user.Id})' style='cursor: pointer;' />"
                : $"<div class='user-table-initials me-3 clickable-avatar' onclick='viewUserDetails({user.Id})' style='cursor: pointer;'>{initials}</div>";

            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            var email = user.Email ?? "No Email";
            var userCode = !string.IsNullOrEmpty(user.Code) ? $"<small class='text-muted'>({user.Code})</small>" : "";

            // Make the entire user info clickable
            return $@"
        <div class='d-flex align-items-center'>
            {avatarHtml}
            <div class='clickable-user-info' onclick='viewUserDetails({user.Id})' style='cursor: pointer;'>
                <div class='fw-semibold text-primary hover-underline'>{fullName} {userCode}</div>
                <small class='text-muted'>{email}</small>
            </div>
        </div>";
        }

        // 2. UPDATE GetActionButtons method with role-based dropdown
        private string GetActionButtons(int userId, bool isActive, string userName, string userCode, string currentUserCode)
        {
            var currentUser = Util.GetCurrentUser();
            var isCurrentUser = userCode.Equals(currentUserCode, StringComparison.OrdinalIgnoreCase);
            var isSuperAdmin = currentUser.IsSuperAdmin;
            var isAdmin = currentUser.IsAdmin;

            if (isCurrentUser)
            {
                // Current user - only view details
                return $@"
            <div class='dropdown'>
                <button class='btn btn-sm btn-outline-secondary dropdown-toggle' type='button' data-bs-toggle='dropdown'>
                    <i class='fa fa-user-shield'></i>
                </button>
                <ul class='dropdown-menu'>
                    <li>
                        <a class='dropdown-item' href='{Url.Action("Details", new { id = userId })}'>
                            <i class='fa fa-eye text-info me-2'></i>View Details
                        </a>
                    </li>
                    <li><hr class='dropdown-divider'></li>
                    <li><span class='dropdown-item-text text-muted'><i class='fa fa-info-circle me-2'></i>You cannot modify your own account</span></li>
                </ul>
            </div>";
            }

            var dropdownItems = new List<string>();

            // Always show details
            dropdownItems.Add($@"
        <li>
            <a class='dropdown-item' href='{Url.Action("Details", new { id = userId })}'>
                <i class='fa fa-eye text-info me-2'></i>View Details
            </a>
        </li>");

            // Admin and Super Admin actions
            if (isAdmin || isSuperAdmin)
            {
                dropdownItems.Add("<li><hr class='dropdown-divider'></li>");

                // Edit - Available to Admin and Super Admin
                dropdownItems.Add($@"
            <li>
                <a class='dropdown-item' href='{Url.Action("Edit", new { id = userId })}'>
                    <i class='fa fa-edit text-primary me-2'></i>Edit User
                </a>
            </li>");

                // Toggle Status - Available to Admin and Super Admin  
                var statusIcon = isActive ? "fa-pause text-warning" : "fa-play text-success";
                var statusText = isActive ? "Deactivate User" : "Activate User";

                dropdownItems.Add($@"
            <li>
                <a class='dropdown-item' href='javascript:void(0)' onclick='toggleStatus({userId}, {isActive.ToString().ToLower()})'>
                    <i class='fa {statusIcon} me-2'></i>{statusText}
                </a>
            </li>");
            }

            // Super Admin only actions
            if (isSuperAdmin)
            {
                dropdownItems.Add("<li><hr class='dropdown-divider'></li>");
                dropdownItems.Add("<li><h6 class='dropdown-header text-danger'><i class='fa fa-crown me-2'></i>Super Admin Actions</h6></li>");

                // Toggle Admin Status
                dropdownItems.Add($@"
            <li>
                <a class='dropdown-item' href='javascript:void(0)' onclick='toggleAdminStatus({userId})'>
                    <i class='fa fa-shield-alt text-orange me-2'></i>Toggle Admin Status
                </a>
            </li>");

                // Toggle Super Admin Status  
                dropdownItems.Add($@"
            <li>
                <a class='dropdown-item' href='javascript:void(0)' onclick='toggleSuperAdminStatus({userId})'>
                    <i class='fa fa-crown text-purple me-2'></i>Toggle Super Admin
                </a>
            </li>");

                // Delete User
                dropdownItems.Add($@"
            <li>
                <a class='dropdown-item text-danger' href='javascript:void(0)' onclick='deleteUser({userId}, &quot;{System.Web.HttpUtility.HtmlAttributeEncode(userName ?? "User")}&quot;)'>
                    <i class='fa fa-trash text-danger me-2'></i>Delete User
                </a>
            </li>");
            }

            // No actions available
            if (dropdownItems.Count == 1) // Only "View Details"
            {
                return $@"
            <a href='{Url.Action("Details", new { id = userId })}' class='btn btn-sm btn-outline-info' title='View Details'>
                <i class='fa fa-eye'></i>
            </a>";
            }

            // Build dropdown
            var buttonClass = isSuperAdmin ? "btn-outline-danger" : (isAdmin ? "btn-outline-warning" : "btn-outline-info");
            var buttonIcon = isSuperAdmin ? "fa-crown" : (isAdmin ? "fa-shield-alt" : "fa-cog");

            return $@"
        <div class='dropdown'>
            <button class='btn btn-sm {buttonClass} dropdown-toggle' type='button' data-bs-toggle='dropdown'>
                <i class='fa {buttonIcon}'></i>
            </button>
            <ul class='dropdown-menu dropdown-menu-end'>
                {string.Join("", dropdownItems)}
            </ul>
        </div>";
        }





        [HttpPost]
        public async Task<JsonResult> GetSecurityUsersData()
        {
            try
            {
                var draw = int.Parse(Request.Form["draw"]);
                var start = int.Parse(Request.Form["start"]);
                var length = int.Parse(Request.Form["length"]);
                var searchValue = Request.Form["search[value]"].ToString();
                var roleFilter = Request.Form["roleFilter"].ToString();
                var statusFilter = Request.Form["statusFilter"].ToString();

                var currentUserCode = Util.GetCurrentUser().Code;
                var users = await _userService.GetAllUsersAsync();

                // 🔥 FIXED: Show ALL users by default, filter only when specifically requested
                if (!string.IsNullOrEmpty(statusFilter))
                {
                    switch (statusFilter.ToLowerInvariant())
                    {
                        case "active":
                            users = users.Where(u => u.IsActive);
                            break;
                        case "inactive":
                            users = users.Where(u => !u.IsActive);
                            break;
                            // "all" or any other value shows all users
                    }
                }
                // If statusFilter is empty, show ALL users (both active and inactive)

                // Apply other filters...
                if (!string.IsNullOrEmpty(roleFilter))
                {
                    users = users.Where(u => u.UserRoles != null && u.UserRoles.Any(ur => ur.IsActive
                        && ur.Role.IsActive
                        && ur.Role.Name == roleFilter));
                }

                if (!string.IsNullOrEmpty(searchValue))
                {
                    users = users.Where(u =>
                        u.Code.Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                        (u.FirstName != null && u.FirstName.Contains(searchValue, StringComparison.OrdinalIgnoreCase)) ||
                        (u.LastName != null && u.LastName.Contains(searchValue, StringComparison.OrdinalIgnoreCase)) ||
                        (u.Email != null && u.Email.Contains(searchValue, StringComparison.OrdinalIgnoreCase)) ||
                        (u.Department != null && u.Department.Contains(searchValue, StringComparison.OrdinalIgnoreCase))
                    );
                }

                var totalRecords = users.Count();
                var pagedUsers = users.Skip(start).Take(length).ToList();

                var response = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = totalRecords,
                    data = pagedUsers.Select(user => new
                    {
                        id = user.Id,
                        user = GetUserDisplayColumn(user), // Now clickable!
                        department = user.Department ?? "N/A",
                        userClass = GetUserClassBadge(user),
                        roles = user.UserRoles != null ? string.Join(", ", user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name)) : "",
                        isActive = user.IsActive,
                        lastLoginAt = user.LastLoginDate.HasValue ? user.LastLoginDate.Value.ToString("yyyy-MM-dd HH:mm") : "Never",
                        statusBadge = GetStatusBadge(user.IsActive),
                        actions = GetActionButtons(user.Id, user.IsActive, user.FullName, user.Code, currentUserCode) // Role-based dropdown!
                    })
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security users data");
                return Json(new { error = "Error loading data" });
            }
        }

        /// <summary>
        /// Get updated statistics for dashboard - NEW METHOD
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetStatistics()
        {
            try
            {
                var totalUsers = await _userService.GetTotalUsersAsync();
                var activeUsersCount = await _userService.GetActiveUsersCountAsync();
                var allUsers = await _userService.GetAllUsersAsync();
                var usersWithRoles = allUsers.Count(u => u.UserRoles != null && u.UserRoles.Any(ur => ur.IsActive));

                return Json(new
                {
                    totalUsers = totalUsers,
                    activeUsers = activeUsersCount,
                    inactiveUsers = totalUsers - activeUsersCount,
                    usersWithRoles = usersWithRoles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting statistics");
                return Json(new { error = "Error loading statistics" });
            }
        }





        private string GetUserClassBadge(SecurityUser user)
        {
            if (user.IsSuperAdmin)
            {
                return "<span class='badge bg-danger'><i class='fa fa-crown me-1'></i>Super Admin</span>";
            }
            else if (user.IsAdmin)
            {
                return "<span class='badge bg-warning'><i class='fa fa-shield-alt me-1'></i>Admin</span>";
            }
            else
            {
                return "<span class='badge bg-info'><i class='fa fa-user me-1'></i>Public</span>";
            }
        }

        private string GetUserInitials(SecurityUser user)
        {
            var firstInitial = !string.IsNullOrEmpty(user.FirstName) ? user.FirstName.Substring(0, 1).ToUpper() : "";
            var lastInitial = !string.IsNullOrEmpty(user.LastName) ? user.LastName.Substring(0, 1).ToUpper() : "";
            return $"{firstInitial}{lastInitial}";
        }



        /// <summary>
        /// Search for available users to add to the security system
        /// This searches Active Directory for users not yet in our security system
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> SearchAvailableUsers(string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                {
                    return Json(new List<object>());
                }

                // Search Active Directory for users
                var adUsers = await _adService.SearchADUsersAsync(query);
                if (!adUsers.Any())
                {
                    return Json(new List<object>());
                }

                var results = new List<object>();

                // Process AD users and check if they already exist in our security system
                foreach (var adUser in adUsers.Take(20)) // Limit to 20 results for performance
                {
                    // Check if user already exists in our security system
                    var existingUser = await _userService.GetUserByCodeAsync(adUser.LoginName) ??
                                      await _userService.GetUserByEmailAsync(adUser.EmailAddress);

                    var fullName = $"{adUser.FirstName} {adUser.MiddleName} {adUser.LastName}".Replace("  ", " ").Trim();
                    var photoUrl = adUser.Photo != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(adUser.Photo)}" : null;

                    results.Add(new
                    {
                        fullName = fullName,
                        email = adUser.EmailAddress,
                        department = adUser.Department,
                        manager = adUser.ManagerName,
                        employeeId = adUser.LoginName,
                        code = adUser.LoginName,
                        jobTitle = adUser.Title,
                        company = adUser.Company,
                        photoUrl = photoUrl,
                        enabled = adUser.Enabled,
                        alreadyExists = existingUser != null
                    });
                }

                return Json(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching for available users with query: {Query}", query);
                return Json(new { error = "Error searching users" });
            }
        }

        /// <summary>
        /// Add an existing user from Active Directory to our security system
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> AddExistingUser(string employeeId, string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(employeeId) && string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, message = "Employee ID or email is required" });
                }

                // Check if user already exists in our security system
                var existingUser = await _userService.GetUserByCodeAsync(employeeId) ??
                                  await _userService.GetUserByEmailAsync(email);

                if (existingUser != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "User already exists in the security system",
                        editUrl = Url.Action("Edit", new { id = existingUser.Id })
                    });
                }

                // Search AD again to get full user details for the selected user
                var searchResults = await _adService.SearchADUsersAsync(employeeId);
                if (!searchResults.Any())
                {
                    searchResults = await _adService.SearchADUsersAsync(email);
                }

                var adUser = searchResults.FirstOrDefault(u =>
                    u.LoginName == employeeId ||
                    u.EmailAddress.Equals(email, StringComparison.OrdinalIgnoreCase));

                if (adUser == null)
                {
                    return Json(new { success = false, message = "User not found in Active Directory" });
                }

                // REMOVED: AD Enabled status check - Accept any user regardless of AD status

                var currentUserId = Util.GetCurrentUser().Code;

                // Create new security user from AD data
                var newUser = new SecurityUser
                {
                    Code = adUser.LoginName,
                    FirstName = adUser.FirstName,
                    LastName = adUser.LastName,
                    Email = adUser.EmailAddress,
                    Department = adUser.Department,
                    IsActive = true, // Always set to active regardless of AD status
                    Photo = adUser.Photo,
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = currentUserId,
                    UpdatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedBy = currentUserId
                };

                var createdUser = await _userService.CreateUserAsync(newUser, currentUserId);

                if (createdUser != null)
                {
                    return Json(new
                    {
                        success = true,
                        message = "User added successfully from Active Directory",
                        editUrl = Url.Action("Edit", new { id = createdUser.Id })
                    });
                }

                return Json(new { success = false, message = "Failed to create user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding existing user: {EmployeeId}, {Email}", employeeId, email);
                return Json(new { success = false, message = "An error occurred while adding the user" });
            }
        }



        /// <summary>
        /// Security Users list with DataTables and Statistics
        /// </summary>
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new SecurityUsersIndexViewModel();

                // Get statistics using existing methods
                var activeUsers = await _userService.GetActiveUsersAsync();
                var totalUsers = await _userService.GetTotalUsersAsync();
                var activeUsersCount = await _userService.GetActiveUsersCountAsync();

                viewModel.Statistics = new SecurityUserStatisticsViewModel
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsersCount,
                    InactiveUsers = totalUsers - activeUsersCount,
                    UsersWithRoles = activeUsers.Count(u => u.UserRoles != null && u.UserRoles.Any(ur => ur.IsActive))
                };

                // Get roles for filter dropdown
                var roles = await _roleService.GetActiveRolesAsync();
                viewModel.RoleFilters = roles.Select(r => new SelectListItem
                {
                    Value = r.Name,
                    Text = r.Name
                }).ToList();

                ViewBag.Title = "Security Users Management";
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security users index");
                TempData.Error("Error loading users data.", popup: false);
                return View(new SecurityUsersIndexViewModel());
            }
        }



        /// <summary>
        /// Create Security User form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            try
            {
                var viewModel = new SecurityUserManagementViewModel();
                await PopulateViewModelDropdowns(viewModel);
                ViewBag.Title = "Create Security User";
                ViewBag.ShowBreadcrumb = true;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create user form");
                TempData.Error("Error loading form.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Create Security User
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SecurityUserManagementViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateViewModelDropdowns(model);
                    TempData.Warning("Please check the data entered.", popup: false);
                    ViewBag.Title = "Create Security User";
                    ViewBag.ShowBreadcrumb = true;
                    return View(model);
                }

                // Check if user code already exists
                var existingUser = await _userService.GetUserByCodeAsync(model.Code);
                if (existingUser != null)
                {
                    ModelState.AddModelError("Code", "User code already exists.");
                    await PopulateViewModelDropdowns(model);
                    TempData.Warning("User code already exists.", popup: false);
                    ViewBag.Title = "Create Security User";
                    ViewBag.ShowBreadcrumb = true;
                    return View(model);
                }

                var userId = Util.GetCurrentUser().Code;

                // Create user entity
                var user = new SecurityUser
                {
                    Code = model.Code,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    Department = model.Department,
                    IsActive = model.IsActive,
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = userId
                };

                var result = await _userService.CreateUserAsync(user, userId);

                if (result != null)
                {
                    // Assign roles
                    if (model.SelectedRoleIds != null && model.SelectedRoleIds.Any())
                    {
                        foreach (var roleId in model.SelectedRoleIds)
                        {
                            var role = await _roleService.GetRoleByIdAsync(roleId);
                            if (role != null)
                            {
                                await _userService.AddUserToRoleAsync(result.Id, role.Name, userId);
                            }
                        }
                    }

                    TempData.Success("Security User created successfully!", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                TempData.Error("Failed to create security user.", popup: false);
                await PopulateViewModelDropdowns(model);
                ViewBag.Title = "Create Security User";
                ViewBag.ShowBreadcrumb = true;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating security user");
                TempData.Error("An error occurred while creating security user.", popup: false);
                await PopulateViewModelDropdowns(model);
                ViewBag.Title = "Create Security User";
                ViewBag.ShowBreadcrumb = true;
                return View(model);
            }
        }

        /// <summary>
        /// Edit Security User form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData.Error("Security User not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                var viewModel = new SecurityUserManagementViewModel
                {
                    Id = user.Id,
                    Code = user.Code,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Department = user.Department,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    LastLoginAt = user.LastLoginDate,
                    SelectedRoleIds = user.UserRoles != null ? user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.RoleId).ToList() : new List<int>()
                };

                await PopulateViewModelDropdowns(viewModel);
                ViewBag.Title = $"Edit Security User - {user.FirstName} {user.LastName}";
                ViewBag.ShowBreadcrumb = true;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for user: {UserId}", id);
                TempData.Error("Error loading user data.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Edit Security User
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SecurityUserManagementViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateViewModelDropdowns(model);
                    TempData.Warning("Please check the data entered.", popup: false);
                    ViewBag.Title = $"Edit Security User - {model.FirstName} {model.LastName}";
                    ViewBag.ShowBreadcrumb = true;
                    return View(model);
                }

                var user = await _userService.GetUserByIdAsync(model.Id);
                if (user == null)
                {
                    TempData.Error("Security User not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                var userId = Util.GetCurrentUser().Code;

                // Update user properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.Email = model.Email;
                user.Department = model.Department;
                user.IsActive = model.IsActive;
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedBy = userId;

                var result = await _userService.UpdateUserAsync(user, userId);

                if (result != null)
                {
                    // Update roles - remove all existing and add selected ones
                    var currentRoles = await _userService.GetUserRolesAsync(user.Id);
                    foreach (var roleName in currentRoles)
                    {
                        await _userService.RemoveUserFromRoleAsync(user.Id, roleName);
                    }

                    // Add selected roles
                    if (model.SelectedRoleIds != null && model.SelectedRoleIds.Any())
                    {
                        foreach (var roleId in model.SelectedRoleIds)
                        {
                            var role = await _roleService.GetRoleByIdAsync(roleId);
                            if (role != null)
                            {
                                await _userService.AddUserToRoleAsync(user.Id, role.Name, userId);
                            }
                        }
                    }

                    TempData.Success("Security User updated successfully!", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                TempData.Error("Failed to update security user.", popup: false);
                await PopulateViewModelDropdowns(model);
                ViewBag.Title = $"Edit Security User - {model.FirstName} {model.LastName}";
                ViewBag.ShowBreadcrumb = true;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating security user: {UserId}", model.Id);
                TempData.Error("An error occurred while updating security user.", popup: false);
                await PopulateViewModelDropdowns(model);
                ViewBag.Title = $"Edit Security User - {model.FirstName} {model.LastName}";
                ViewBag.ShowBreadcrumb = true;
                return View(model);
            }
        }

        /// <summary>
        /// Security User details view
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var user = await _userService.GetUserByIdAsync(id);
                if (user == null)
                {
                    TempData.Error("Security User not found.", popup: false);
                    return RedirectToAction(nameof(Index));
                }

                // 🔥 FIX: Helper method to get user initials
                var initials = GetUserInitials(user);

                var viewModel = new SecurityUserDetailsViewModel
                {
                    Id = user.Id,
                    Code = user.Code,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Department = user.Department,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    CreatedBy = user.CreatedBy,
                    UpdatedBy = user.UpdatedBy,
                    LastLoginAt = user.LastLoginDate,
                    UserRoles = user.UserRoles != null ? user.UserRoles.Where(ur => ur.IsActive).ToList() : new List<SecurityUserRole>(),

                    // 🔥 FIX: Add missing photo properties
                    Photo = user.Photo,
                    Photo_base64 = user.Photo != null ? Convert.ToBase64String(user.Photo) : "",
                    HasPhoto = user.Photo != null && user.Photo.Length > 0,
                    PhotoDataUrl = user.Photo != null ? $"data:image/jpeg;base64,{Convert.ToBase64String(user.Photo)}" : null,
                    Initials = initials
                };

                ViewBag.Title = $"Security User Details - {user.FirstName} {user.LastName}";
                ViewBag.ShowBreadcrumb = true;
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading security user details: {UserId}", id);
                TempData.Error("Error loading security user details.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }



        /// <summary>
        /// Get roles filter for DataTables
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetRolesFilter()
        {
            try
            {
                var roles = await _roleService.GetActiveRolesAsync();
                var rolesList = roles.Select(r => new { value = r.Name, text = r.Name }).ToList();
                return Json(rolesList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading roles filter");
                return Json(new { error = "Error loading roles" });
            }
        }

        /// <summary>
        /// Export users to Excel
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportToExcel()
        {
            try
            {
                var activeUsers = await _userService.GetActiveUsersAsync();
                // Implementation for Excel export would go here
                TempData.Info("Excel export functionality coming soon!", popup: false);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users to Excel");
                TempData.Error("Error exporting data.", popup: false);
                return RedirectToAction(nameof(Index));
            }
        }

        #region Private Helper Methods

        private async Task PopulateViewModelDropdowns(SecurityUserManagementViewModel model)
        {
            try
            {
                var roles = await _roleService.GetActiveRolesAsync();
                model.AvailableRoles = roles.Select(r => new SelectListItem
                {
                    Value = r.Id.ToString(),
                    Text = r.Name,
                    Selected = model.SelectedRoleIds?.Contains(r.Id) ?? false
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating dropdown data");
                model.AvailableRoles = new List<SelectListItem>();
            }
        }

        private string GetActionButtons(int userId, bool isActive)
        {
            var buttons = new List<string>();

            // Details button
            buttons.Add($@"<a href=""{Url.Action("Details", new { id = userId })}"" 
                           class=""btn btn-sm btn-outline-info"" title=""View Details"">
                           <i class=""fa fa-eye""></i>
                       </a>");

            // Edit button
            buttons.Add($@"<a href=""{Url.Action("Edit", new { id = userId })}"" 
                           class=""btn btn-sm btn-outline-primary"" title=""Edit"">
                           <i class=""fa fa-edit""></i>
                       </a>");


            // Activate/Deactivate button
            if (isActive)
            {
                buttons.Add($@"<button type=""button"" class=""btn btn-sm btn-outline-secondary"" 
                               onclick=""toggleStatus({userId})"" title=""Deactivate"">
                               <i class=""fa fa-pause""></i>
                           </button>");
            }
            else
            {
                buttons.Add($@"<button type=""button"" class=""btn btn-sm btn-outline-success"" 
                               onclick=""toggleStatus({userId})"" title=""Activate"">
                               <i class=""fa fa-play""></i>
                           </button>");
            }

            // Delete button
            buttons.Add($@"<a href=""{Url.Action("Delete", new { id = userId })}"" 
                           class=""btn btn-sm btn-outline-danger"" title=""Delete"">
                           <i class=""fa fa-trash""></i>
                       </a>");

            return $@"<div class=""btn-group"" role=""group"">{string.Join("", buttons)}</div>";
        }

        private string GetStatusBadge(bool isActive)
        {
            if (isActive)
                return "<span class='badge bg-success'>Active</span>";
            return "<span class='badge bg-secondary'>Inactive</span>";
        }

        #endregion
    }
}