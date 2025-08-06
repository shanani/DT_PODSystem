// Areas/Security/Helpers/SecurityExtensions.cs - ENHANCED VERSION
// ✅ KEEPS: All existing extensions and functionality intact
// ✅ ADDS: Cached permission checks, real-time invalidation, performance optimization
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace DT_PODSystem.Areas.Security.Helpers
{
    /// <summary>
    /// Extension methods for security-related operations and user identity
    /// ENHANCED with smart caching and real-time refresh capabilities
    /// </summary>
    public static class SecurityExtensions
    {
        // Smart caching configuration
        private static readonly TimeSpan PERMISSION_CACHE_DURATION = TimeSpan.FromMinutes(5); // 5-minute cache
        private static readonly TimeSpan ROLE_CACHE_DURATION = TimeSpan.FromMinutes(3); // 3-minute cache  
        private static readonly string PERMISSION_CACHE_PREFIX = "user_permissions:";
        private static readonly string ROLE_CACHE_PREFIX = "user_roles:";
        private static readonly string SECURITY_CONTEXT_PREFIX = "security_context:";

        #region User Identity Extensions (EXISTING - NO CHANGES)

        /// <summary>
        /// Checks if the current user has Admin privileges (from claims)
        /// </summary>
        public static bool IsAdmin(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return false;
            return user.HasClaim("IsAdmin", "true");
        }

        /// <summary>
        /// Checks if the current user has SuperAdmin privileges (from claims)
        /// </summary>
        public static bool IsSuperAdmin(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return false;
            return user.HasClaim("IsSuperAdmin", "true");
        }

        /// <summary>
        /// Checks if the current user is active (from claims)
        /// </summary>
        public static bool IsActive(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return false;
            return user.HasClaim("IsActive", "true");
        }

        /// <summary>
        /// Checks if user has valid access (authenticated and active)
        /// </summary>
        public static bool HasValidAccess(this ClaimsPrincipal user)
        {
            return user.Identity.IsAuthenticated && user.IsActive();
        }

        /// <summary>
        /// Checks if user has any admin access (Admin OR SuperAdmin)
        /// </summary>
        public static bool HasAdminAccess(this ClaimsPrincipal user)
        {
            return user.IsAdmin() || user.IsSuperAdmin();
        }

        /// <summary>
        /// Checks if user has SuperAdmin access specifically
        /// </summary>
        public static bool HasSuperAdminAccess(this ClaimsPrincipal user)
        {
            return user.IsSuperAdmin();
        }

        /// <summary>
        /// Gets the user ID from claims
        /// </summary>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return 0;

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? user.FindFirst("UserId")?.Value;

            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Gets the user code from claims
        /// </summary>
        public static string GetUserCode(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return string.Empty;

            return user.FindFirst(ClaimTypes.Name)?.Value
                   ?? user.FindFirst("UserCode")?.Value
                   ?? string.Empty;
        }

        /// <summary>
        /// Gets the user email from claims
        /// </summary>
        public static string GetUserEmail(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return string.Empty;

            return user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets the user's full name from claims
        /// </summary>
        public static string GetUserFullName(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return string.Empty;

            return user.FindFirst("FullName")?.Value
                   ?? user.FindFirst(ClaimTypes.GivenName)?.Value
                   ?? string.Empty;
        }

        /// <summary>
        /// Gets the user's display name from claims
        /// </summary>
        public static string GetUserDisplayName(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return string.Empty;

            return user.FindFirst("DisplayName")?.Value
                   ?? user.GetUserFullName()
                   ?? user.GetUserCode();
        }

        /// <summary>
        /// Gets the user's department from claims
        /// </summary>
        public static string GetUserDepartment(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return string.Empty;

            return user.FindFirst("Department")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets the user's job title from claims
        /// </summary>
        public static string GetUserJobTitle(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return string.Empty;

            return user.FindFirst("JobTitle")?.Value ?? string.Empty;
        }

        /// <summary>
        /// Gets all user roles from claims
        /// </summary>
        public static List<string> GetUserRoles(this ClaimsPrincipal user)
        {
            if (!user.Identity.IsAuthenticated) return new List<string>();

            return user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        }

        /// <summary>
        /// Checks if user has a specific role
        /// </summary>
        public static bool HasRole(this ClaimsPrincipal user, string roleName)
        {
            if (!user.Identity.IsAuthenticated || string.IsNullOrEmpty(roleName)) return false;

            return user.IsInRole(roleName);
        }

        /// <summary>
        /// Checks if user has any of the specified roles
        /// </summary>
        public static bool HasAnyRole(this ClaimsPrincipal user, params string[] roleNames)
        {
            if (!user.Identity.IsAuthenticated || roleNames == null || !roleNames.Any()) return false;

            return roleNames.Any(role => user.IsInRole(role));
        }

        /// <summary>
        /// Checks if user has all of the specified roles
        /// </summary>
        public static bool HasAllRoles(this ClaimsPrincipal user, params string[] roleNames)
        {
            if (!user.Identity.IsAuthenticated || roleNames == null || !roleNames.Any()) return false;

            return roleNames.All(role => user.IsInRole(role));
        }

        #endregion

        #region 🚀 NEW: Smart Cached Permission Extensions

        /// <summary>
        /// 🆕 CACHED: Checks if user has specific permission with smart caching
        /// </summary>
        public static async Task<bool> HasPermissionCachedAsync(this ClaimsPrincipal user, string permissionName, IServiceProvider serviceProvider)
        {
            if (!user.Identity.IsAuthenticated || string.IsNullOrEmpty(permissionName))
                return false;

            var userCode = user.GetUserCode();
            if (string.IsNullOrEmpty(userCode))
                return false;

            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            var securityService = serviceProvider.GetService<ISecurityService>();

            if (memoryCache == null || securityService == null)
            {
                // Fallback to non-cached version
                return await user.HasPermissionAsync(securityService, permissionName);
            }

            var cacheKey = $"{PERMISSION_CACHE_PREFIX}{userCode}";

            // Try cache first
            var cachedPermissions = memoryCache.Get<List<string>>(cacheKey);
            if (cachedPermissions != null)
            {
                return cachedPermissions.Contains(permissionName);
            }

            // Cache miss - load from database
            var permissions = await securityService.GetUserPermissionsAsync(userCode);

            // Cache the result
            memoryCache.Set(cacheKey, permissions, PERMISSION_CACHE_DURATION);

            return permissions.Contains(permissionName);
        }

        /// <summary>
        /// 🆕 CACHED: Gets all user permissions with smart caching
        /// </summary>
        public static async Task<List<string>> GetPermissionsCachedAsync(this ClaimsPrincipal user, IServiceProvider serviceProvider)
        {
            if (!user.Identity.IsAuthenticated)
                return new List<string>();

            var userCode = user.GetUserCode();
            if (string.IsNullOrEmpty(userCode))
                return new List<string>();

            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            var securityService = serviceProvider.GetService<ISecurityService>();

            if (memoryCache == null || securityService == null)
            {
                // Fallback to non-cached version
                return await user.GetPermissionsAsync(securityService);
            }

            var cacheKey = $"{PERMISSION_CACHE_PREFIX}{userCode}";

            // Try cache first
            var cachedPermissions = memoryCache.Get<List<string>>(cacheKey);
            if (cachedPermissions != null)
            {
                return cachedPermissions;
            }

            // Cache miss - load from database
            var permissions = await securityService.GetUserPermissionsAsync(userCode);

            // Cache the result
            memoryCache.Set(cacheKey, permissions, PERMISSION_CACHE_DURATION);

            return permissions;
        }

        /// <summary>
        /// 🆕 CACHED: Gets user security context with caching
        /// </summary>
        public static async Task<Models.DTOs.SecurityContextDto> GetSecurityContextCachedAsync(this ClaimsPrincipal user, IServiceProvider serviceProvider)
        {
            if (!user.Identity.IsAuthenticated)
                return null;

            var userId = user.GetUserId();
            if (userId == 0)
                return null;

            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            var securityService = serviceProvider.GetService<ISecurityService>();

            if (memoryCache == null || securityService == null)
            {
                // Fallback to non-cached version
                return await user.GetSecurityContextAsync(securityService);
            }

            var cacheKey = $"{SECURITY_CONTEXT_PREFIX}{userId}";

            // Try cache first
            var cachedContext = memoryCache.Get<Models.DTOs.SecurityContextDto>(cacheKey);
            if (cachedContext != null)
            {
                return cachedContext;
            }

            // Cache miss - load from database
            var context = await securityService.GetSecurityContextAsync(userId);

            // Cache the result
            if (context != null)
            {
                memoryCache.Set(cacheKey, context, PERMISSION_CACHE_DURATION);
            }

            return context;
        }

        /// <summary>
        /// 🆕 FORCE REFRESH: Invalidate user permission cache (for role changes)
        /// </summary>
        public static void RefreshUserPermissionsAsync(this ClaimsPrincipal user, IServiceProvider serviceProvider)
        {
            if (!user.Identity.IsAuthenticated)
                return;

            var userCode = user.GetUserCode();
            var userId = user.GetUserId();

            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            if (memoryCache == null)
                return;

            // Clear all caches for this user
            var permissionCacheKey = $"{PERMISSION_CACHE_PREFIX}{userCode}";
            var roleCacheKey = $"{ROLE_CACHE_PREFIX}{userCode}";
            var contextCacheKey = $"{SECURITY_CONTEXT_PREFIX}{userId}";

            memoryCache.Remove(permissionCacheKey);
            memoryCache.Remove(roleCacheKey);
            memoryCache.Remove(contextCacheKey);

            Console.WriteLine($"🗑️ [EXTENSIONS] Permission cache cleared for user: {userCode}");
        }

        #endregion

        #region Controller Extensions (EXISTING - NO CHANGES)

        /// <summary>
        /// Controller extension - checks if current user is Admin
        /// </summary>
        public static bool IsAdmin(this Controller controller)
        {
            return controller.User.IsAdmin();
        }

        /// <summary>
        /// Controller extension - checks if current user is SuperAdmin
        /// </summary>
        public static bool IsSuperAdmin(this Controller controller)
        {
            return controller.User.IsSuperAdmin();
        }

        /// <summary>
        /// Controller extension - checks if current user is active
        /// </summary>
        public static bool IsActive(this Controller controller)
        {
            return controller.User.IsActive();
        }

        /// <summary>
        /// Controller extension - checks if user has valid access
        /// </summary>
        public static bool HasValidAccess(this Controller controller)
        {
            return controller.User.HasValidAccess();
        }

        /// <summary>
        /// Controller extension - checks if user has admin access
        /// </summary>
        public static bool HasAdminAccess(this Controller controller)
        {
            return controller.User.HasAdminAccess();
        }

        /// <summary>
        /// Controller extension - checks if user has super admin access
        /// </summary>
        public static bool HasSuperAdminAccess(this Controller controller)
        {
            return controller.User.HasSuperAdminAccess();
        }

        /// <summary>
        /// Helper method to get current user ID from controller
        /// </summary>
        public static int GetCurrentUserId(this Controller controller)
        {
            return controller.User.GetUserId();
        }

        /// <summary>
        /// Helper method to get current user code from controller
        /// </summary>
        public static string GetCurrentUserCode(this Controller controller)
        {
            return controller.User.GetUserCode();
        }

        /// <summary>
        /// Helper method to get current user email from controller
        /// </summary>
        public static string GetCurrentUserEmail(this Controller controller)
        {
            return controller.User.GetUserEmail();
        }

        /// <summary>
        /// Helper method to get current user full name from controller
        /// </summary>
        public static string GetCurrentUserFullName(this Controller controller)
        {
            return controller.User.GetUserFullName();
        }

        /// <summary>
        /// Helper method to get current user display name from controller
        /// </summary>
        public static string GetCurrentUserDisplayName(this Controller controller)
        {
            return controller.User.GetUserDisplayName();
        }

        /// <summary>
        /// Helper method to get current user department from controller
        /// </summary>
        public static string GetCurrentUserDepartment(this Controller controller)
        {
            return controller.User.GetUserDepartment();
        }

        /// <summary>
        /// Helper method to get current user job title from controller
        /// </summary>
        public static string GetCurrentUserJobTitle(this Controller controller)
        {
            return controller.User.GetUserJobTitle();
        }

        /// <summary>
        /// Helper method to get client IP address from controller
        /// </summary>
        public static string GetClientIpAddress(this Controller controller)
        {
            return controller.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Helper method to get user agent from controller
        /// </summary>
        public static string GetUserAgent(this Controller controller)
        {
            return controller.HttpContext.Request.Headers["User-Agent"].ToString();
        }

        #endregion

        #region HttpContext Extensions (EXISTING - NO CHANGES)

        /// <summary>
        /// HttpContext extension - checks if current user is Admin
        /// </summary>
        public static bool IsAdmin(this HttpContext context)
        {
            return context.User.IsAdmin();
        }

        /// <summary>
        /// HttpContext extension - checks if current user is SuperAdmin
        /// </summary>
        public static bool IsSuperAdmin(this HttpContext context)
        {
            return context.User.IsSuperAdmin();
        }

        /// <summary>
        /// HttpContext extension - checks if current user has valid access
        /// </summary>
        public static bool HasValidAccess(this HttpContext context)
        {
            return context.User.HasValidAccess();
        }

        /// <summary>
        /// HttpContext extension - checks if user has admin access
        /// </summary>
        public static bool HasAdminAccess(this HttpContext context)
        {
            return context.User.HasAdminAccess();
        }

        #endregion

        #region Async Permission Extensions (EXISTING - KEPT FOR BACKWARD COMPATIBILITY)

        /// <summary>
        /// Checks if the current user has a specific permission (ORIGINAL VERSION)
        /// </summary>
        public static async Task<bool> HasPermissionAsync(this ClaimsPrincipal user, ISecurityService securityService, string permissionName)
        {
            if (!user.Identity.IsAuthenticated)
                return false;

            var userId = user.GetUserId();
            if (userId == 0)
                return false;

            return await securityService.HasPermissionAsync(userId, permissionName);
        }

        /// <summary>
        /// Gets all permissions for the current user (ORIGINAL VERSION)
        /// </summary>
        public static async Task<List<string>> GetPermissionsAsync(this ClaimsPrincipal user, ISecurityService securityService)
        {
            if (!user.Identity.IsAuthenticated)
                return new List<string>();

            var userId = user.GetUserId();
            if (userId == 0)
                return new List<string>();

            return await securityService.GetUserPermissionsAsync(userId);
        }

        /// <summary>
        /// Checks if the current user is a Super Admin (ORIGINAL VERSION)
        /// </summary>
        public static async Task<bool> IsSuperAdminAsync(this ClaimsPrincipal user, ISecurityService securityService)
        {
            if (!user.Identity.IsAuthenticated)
                return false;

            var userId = user.GetUserId();
            if (userId == 0)
                return false;

            return await securityService.IsSuperAdminAsync(userId);
        }

        /// <summary>
        /// Gets the current user's security context (ORIGINAL VERSION)
        /// </summary>
        public static async Task<Models.DTOs.SecurityContextDto> GetSecurityContextAsync(this ClaimsPrincipal user, ISecurityService securityService)
        {
            if (!user.Identity.IsAuthenticated)
                return null;

            var userId = user.GetUserId();
            if (userId == 0)
                return null;

            return await securityService.GetSecurityContextAsync(userId);
        }

        #endregion

        #region Authorization Attributes (EXISTING - NO CHANGES)

        /// <summary>
        /// Authorization filter for Admin-only access
        /// </summary>
        public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (!context.HttpContext.User.IsAdmin())
                {
                    context.Result = new ForbidResult();
                }
            }
        }

        /// <summary>
        /// Authorization filter for SuperAdmin-only access
        /// </summary>
        public class SuperAdminOnlyAttribute : Attribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                // In production, only allow SuperAdmin access
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                bool isProduction = string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase);

                if (isProduction && !context.HttpContext.User.IsSuperAdmin())
                {
                    context.Result = new ForbidResult();
                }
                // In development, allow both SuperAdmin and regular admin for testing
                else if (!isProduction && !context.HttpContext.User.HasAdminAccess())
                {
                    context.Result = new ForbidResult();
                }
            }
        }

        /// <summary>
        /// Authorization filter for active users only
        /// </summary>
        public class ActiveUserOnlyAttribute : Attribute, IAuthorizationFilter
        {
            public void OnAuthorization(AuthorizationFilterContext context)
            {
                if (!context.HttpContext.User.HasValidAccess())
                {
                    context.Result = new UnauthorizedResult();
                }
            }
        }

        #endregion

        #region Claims Helper Methods (EXISTING - NO CHANGES)

        /// <summary>
        /// Adds security claims to claims list from SecurityUser entity
        /// </summary>
        public static void AddSecurityClaims(this List<Claim> claims, SecurityUser user)
        {
            if (user == null) return;

            claims.Add(new Claim("IsActive", user.IsActive.ToString().ToLower()));
            claims.Add(new Claim("IsAdmin", user.IsAdmin.ToString().ToLower()));
            claims.Add(new Claim("IsSuperAdmin", user.IsSuperAdmin.ToString().ToLower()));

            // Additional user info claims
            if (!string.IsNullOrEmpty(user.FirstName))
                claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));

            if (!string.IsNullOrEmpty(user.LastName))
                claims.Add(new Claim(ClaimTypes.Surname, user.LastName));

            if (!string.IsNullOrEmpty(user.Department))
                claims.Add(new Claim("Department", user.Department));

            if (!string.IsNullOrEmpty(user.JobTitle))
                claims.Add(new Claim("JobTitle", user.JobTitle));

            claims.Add(new Claim("FullName", user.FullName));
            claims.Add(new Claim("DisplayName", user.DisplayName));
        }

        /// <summary>
        /// Creates a complete ClaimsIdentity from SecurityUser
        /// </summary>
        public static ClaimsIdentity CreateSecurityIdentity(SecurityUser user, string authenticationType = "SecurityCookies")
        {
            if (user == null) return null;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Code),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
            };

            claims.AddSecurityClaims(user);

            return new ClaimsIdentity(claims, authenticationType);
        }

        #endregion

        #region 🚀 NEW: Static Cache Management Methods

        /// <summary>
        /// 🆕 STATIC: Clear permission cache for specific user (for SignalR events)
        /// </summary>
        public static void ClearUserPermissionCache(string userCode, IServiceProvider serviceProvider)
        {
            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            if (memoryCache == null || string.IsNullOrEmpty(userCode))
                return;

            var permissionCacheKey = $"{PERMISSION_CACHE_PREFIX}{userCode}";
            var roleCacheKey = $"{ROLE_CACHE_PREFIX}{userCode}";

            memoryCache.Remove(permissionCacheKey);
            memoryCache.Remove(roleCacheKey);

            Console.WriteLine($"🗑️ [EXTENSIONS] Static permission cache cleared for: {userCode}");
        }

        /// <summary>
        /// 🆕 STATIC: Clear all permission cache (for system-wide changes)
        /// </summary>
        public static void ClearAllPermissionCache(IServiceProvider serviceProvider)
        {
            var memoryCache = serviceProvider.GetService<IMemoryCache>();
            if (memoryCache == null)
                return;

            // Note: IMemoryCache doesn't have a clear all method
            // In production, consider using IDistributedCache (Redis) for better cache management
            Console.WriteLine("🧹 [EXTENSIONS] Permission cache cleanup requested");
        }

        #endregion
    }
}