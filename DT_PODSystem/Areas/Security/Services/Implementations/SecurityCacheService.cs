using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Services.Hubs;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Implementations
{
    /// <summary>
    /// Centralized security caching service for performance optimization
    /// Handles user sessions, permissions, roles, and security context caching
    /// </summary>
    public interface ISecurityCacheService
    {
        // User Permission Caching
        Task<bool> HasPermissionCachedAsync(string userCode, string permissionName);
        Task<List<string>> GetUserPermissionsCachedAsync(string userCode);
        Task<SecurityContextDto> GetUserSecurityContextCachedAsync(int userId);

        // Cache Invalidation
        Task InvalidateUserCacheAsync(string userCode);
        Task InvalidateRoleCacheAsync(string roleName);
        Task InvalidateAllSecurityCacheAsync();

        // Real-time Updates
        Task RefreshUserSessionAsync(string userCode, string reason = null);
        Task NotifyPermissionChangeAsync(string userCode, string permissionName, string action);
        Task NotifyRoleChangeAsync(string userCode, string roleName, string action);
        Task NotifyUserStatusChangeAsync(string userCode, bool isActive);

        // Background Maintenance
        Task CleanupExpiredCacheAsync();
        Task<CacheStatisticsDto> GetCacheStatisticsAsync();
    }

    public class SecurityCacheService : ISecurityCacheService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ISecurityService _securityService;
        private readonly ISecurityUserService _userService;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<SecurityCacheService> _logger;
        private readonly IServiceProvider _serviceProvider;

        // Cache configuration constants
        private static readonly TimeSpan USER_PERMISSION_CACHE_TTL = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan USER_CONTEXT_CACHE_TTL = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan ROLE_CACHE_TTL = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan SESSION_CACHE_TTL = TimeSpan.FromSeconds(30);

        // Cache key patterns
        private const string USER_PERMISSIONS_KEY = "sec:user_perms:{0}";
        private const string USER_CONTEXT_KEY = "sec:user_ctx:{0}";
        private const string ROLE_PERMISSIONS_KEY = "sec:role_perms:{0}";
        private const string USER_ROLES_KEY = "sec:user_roles:{0}";
        private const string CACHE_STATS_KEY = "sec:cache_stats";

        public SecurityCacheService(
            IMemoryCache memoryCache,
            ISecurityService securityService,
            ISecurityUserService userService,
            IHubContext<NotificationHub> hubContext,
            ILogger<SecurityCacheService> logger,
            IServiceProvider serviceProvider)
        {
            _memoryCache = memoryCache;
            _securityService = securityService;
            _userService = userService;
            _hubContext = hubContext;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        #region User Permission Caching

        /// <summary>
        /// Check if user has specific permission with smart caching
        /// </summary>
        public async Task<bool> HasPermissionCachedAsync(string userCode, string permissionName)
        {
            if (string.IsNullOrEmpty(userCode) || string.IsNullOrEmpty(permissionName))
                return false;

            try
            {
                var permissions = await GetUserPermissionsCachedAsync(userCode);
                return permissions.Contains(permissionName, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cached permission {Permission} for user {UserCode}", permissionName, userCode);

                // Fallback to direct database check
                return await _securityService.HasPermissionAsync(userCode, permissionName);
            }
        }

        /// <summary>
        /// Get user permissions with smart caching
        /// </summary>
        public async Task<List<string>> GetUserPermissionsCachedAsync(string userCode)
        {
            if (string.IsNullOrEmpty(userCode))
                return new List<string>();

            var cacheKey = string.Format(USER_PERMISSIONS_KEY, userCode);

            try
            {
                // Try cache first
                if (_memoryCache.TryGetValue(cacheKey, out List<string> cachedPermissions))
                {
                    IncrementCacheHit("user_permissions");
                    return cachedPermissions;
                }

                // Cache miss - load from database
                IncrementCacheMiss("user_permissions");
                var permissions = await _securityService.GetUserPermissionsAsync(userCode);

                // Cache the result
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = USER_PERMISSION_CACHE_TTL,
                    SlidingExpiration = TimeSpan.FromMinutes(2),
                    Priority = CacheItemPriority.High
                };

                _memoryCache.Set(cacheKey, permissions, cacheOptions);

                _logger.LogDebug("User permissions cached for {UserCode}: {Count} permissions", userCode, permissions.Count);
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached permissions for user {UserCode}", userCode);
                return new List<string>();
            }
        }

        /// <summary>
        /// Get user security context with caching
        /// </summary>
        public async Task<SecurityContextDto> GetUserSecurityContextCachedAsync(int userId)
        {
            if (userId <= 0)
                return null;

            var cacheKey = string.Format(USER_CONTEXT_KEY, userId);

            try
            {
                // Try cache first
                if (_memoryCache.TryGetValue(cacheKey, out SecurityContextDto cachedContext))
                {
                    IncrementCacheHit("user_context");
                    return cachedContext;
                }

                // Cache miss - load from database
                IncrementCacheMiss("user_context");
                var context = await _securityService.GetSecurityContextAsync(userId);

                if (context != null)
                {
                    // Cache the result
                    var cacheOptions = new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = USER_CONTEXT_CACHE_TTL,
                        Priority = CacheItemPriority.Normal
                    };

                    _memoryCache.Set(cacheKey, context, cacheOptions);
                    _logger.LogDebug("User security context cached for userId {UserId}", userId);
                }

                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cached security context for user {UserId}", userId);
                return null;
            }
        }

        #endregion

        #region Cache Invalidation

        /// <summary>
        /// Invalidate all cache entries for a specific user
        /// </summary>
        public async Task InvalidateUserCacheAsync(string userCode)
        {
            if (string.IsNullOrEmpty(userCode))
                return;

            try
            {
                // Get user ID for context cache
                var user = await _userService.GetUserByCodeAsync(userCode);

                // Remove all user-related cache entries
                var permissionsCacheKey = string.Format(USER_PERMISSIONS_KEY, userCode);
                var rolesCacheKey = string.Format(USER_ROLES_KEY, userCode);

                _memoryCache.Remove(permissionsCacheKey);
                _memoryCache.Remove(rolesCacheKey);

                if (user != null)
                {
                    var contextCacheKey = string.Format(USER_CONTEXT_KEY, user.Id);
                    _memoryCache.Remove(contextCacheKey);
                }

                _logger.LogInformation("Cache invalidated for user {UserCode}", userCode);

                // Update cache statistics
                IncrementCacheInvalidation("user_cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for user {UserCode}", userCode);
            }
        }

        /// <summary>
        /// Invalidate cache for all users with a specific role
        /// </summary>
        public async Task InvalidateRoleCacheAsync(string roleName)
        {
            if (string.IsNullOrEmpty(roleName))
                return;

            try
            {
                // For role changes, we need to invalidate all users with that role
                // This is a simplified approach - in production, consider tracking role-user relationships

                var roleCacheKey = string.Format(ROLE_PERMISSIONS_KEY, roleName);
                _memoryCache.Remove(roleCacheKey);

                _logger.LogInformation("Role cache invalidated for role {RoleName}", roleName);

                // Note: For role permission changes, we should ideally invalidate all users with that role
                // This would require additional tracking or a more sophisticated cache structure

                IncrementCacheInvalidation("role_cache");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache for role {RoleName}", roleName);
            }
        }

        /// <summary>
        /// Clear all security-related cache entries
        /// </summary>
        public async Task InvalidateAllSecurityCacheAsync()
        {
            try
            {
                // Note: IMemoryCache doesn't have a built-in "clear all" method
                // In production, consider using a distributed cache like Redis for better control

                _logger.LogWarning("All security cache invalidation requested - consider implementing distributed cache for better control");

                // Reset cache statistics
                _memoryCache.Remove(CACHE_STATS_KEY);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating all security cache");
            }
        }

        #endregion

        #region Real-time Updates via SignalR

        /// <summary>
        /// Force refresh user session and notify via SignalR
        /// </summary>
        public async Task RefreshUserSessionAsync(string userCode, string reason = null)
        {
            if (string.IsNullOrEmpty(userCode))
                return;

            try
            {
                // Invalidate user cache
                await InvalidateUserCacheAsync(userCode);

                // Notify via SignalR
                await _hubContext.Clients.All.SendAsync("RefreshUserSession", userCode);

                _logger.LogInformation("User session refresh triggered for {UserCode}: {Reason}", userCode, reason ?? "Security update");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user session for {UserCode}", userCode);
            }
        }

        /// <summary>
        /// Notify about permission changes
        /// </summary>
        public async Task NotifyPermissionChangeAsync(string userCode, string permissionName, string action)
        {
            try
            {
                await InvalidateUserCacheAsync(userCode);

                await _hubContext.Clients.All.SendAsync("UserPermissionChanged", new
                {
                    userCode,
                    permissionName,
                    action,
                    timestamp = DateTime.UtcNow.AddHours(3)
                });

                _logger.LogInformation("Permission change notification sent: {UserCode} - {Permission} {Action}", userCode, permissionName, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying permission change for {UserCode}", userCode);
            }
        }

        /// <summary>
        /// Notify about role changes
        /// </summary>
        public async Task NotifyRoleChangeAsync(string userCode, string roleName, string action)
        {
            try
            {
                await InvalidateUserCacheAsync(userCode);

                await _hubContext.Clients.All.SendAsync("UserRoleChanged", new
                {
                    userCode,
                    roleName,
                    action,
                    timestamp = DateTime.UtcNow.AddHours(3)
                });

                _logger.LogInformation("Role change notification sent: {UserCode} - {Role} {Action}", userCode, roleName, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying role change for {UserCode}", userCode);
            }
        }

        /// <summary>
        /// Notify about user status changes
        /// </summary>
        public async Task NotifyUserStatusChangeAsync(string userCode, bool isActive)
        {
            try
            {
                await InvalidateUserCacheAsync(userCode);

                await _hubContext.Clients.All.SendAsync("UserStatusChanged", new
                {
                    userCode,
                    isActive,
                    timestamp = DateTime.UtcNow.AddHours(3)
                });

                var status = isActive ? "activated" : "deactivated";
                _logger.LogInformation("User status change notification sent: {UserCode} - {Status}", userCode, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying user status change for {UserCode}", userCode);
            }
        }

        #endregion

        #region Background Maintenance

        /// <summary>
        /// Cleanup expired cache entries and optimize memory usage
        /// </summary>
        public async Task CleanupExpiredCacheAsync()
        {
            try
            {
                // IMemoryCache automatically removes expired entries
                // This method can be used for custom cleanup logic if needed

                _logger.LogDebug("Cache cleanup completed");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cache cleanup");
            }
        }

        /// <summary>
        /// Get cache performance statistics
        /// </summary>
        public async Task<CacheStatisticsDto> GetCacheStatisticsAsync()
        {
            try
            {
                var stats = _memoryCache.Get<CacheStatisticsDto>(CACHE_STATS_KEY);
                if (stats == null)
                {
                    stats = new CacheStatisticsDto
                    {
                        StartTime = DateTime.UtcNow.AddHours(3),
                        TotalHits = 0,
                        TotalMisses = 0,
                        TotalInvalidations = 0
                    };
                }

                stats.HitRatio = stats.TotalRequests > 0 ? (double)stats.TotalHits / stats.TotalRequests * 100 : 0;

                return await Task.FromResult(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cache statistics");
                return new CacheStatisticsDto();
            }
        }

        #endregion

        #region Private Helper Methods

        private void IncrementCacheHit(string cacheType)
        {
            var stats = GetOrCreateCacheStats();
            stats.TotalHits++;

            if (!stats.HitsByType.ContainsKey(cacheType))
                stats.HitsByType[cacheType] = 0;
            stats.HitsByType[cacheType]++;

            UpdateCacheStats(stats);
        }

        private void IncrementCacheMiss(string cacheType)
        {
            var stats = GetOrCreateCacheStats();
            stats.TotalMisses++;

            if (!stats.MissesByType.ContainsKey(cacheType))
                stats.MissesByType[cacheType] = 0;
            stats.MissesByType[cacheType]++;

            UpdateCacheStats(stats);
        }

        private void IncrementCacheInvalidation(string cacheType)
        {
            var stats = GetOrCreateCacheStats();
            stats.TotalInvalidations++;

            if (!stats.InvalidationsByType.ContainsKey(cacheType))
                stats.InvalidationsByType[cacheType] = 0;
            stats.InvalidationsByType[cacheType]++;

            UpdateCacheStats(stats);
        }

        private CacheStatisticsDto GetOrCreateCacheStats()
        {
            return _memoryCache.GetOrCreate(CACHE_STATS_KEY, factory =>
            {
                factory.SetAbsoluteExpiration(TimeSpan.FromHours(24));
                return new CacheStatisticsDto
                {
                    StartTime = DateTime.UtcNow.AddHours(3),
                    HitsByType = new Dictionary<string, int>(),
                    MissesByType = new Dictionary<string, int>(),
                    InvalidationsByType = new Dictionary<string, int>()
                };
            });
        }

        private void UpdateCacheStats(CacheStatisticsDto stats)
        {
            _memoryCache.Set(CACHE_STATS_KEY, stats, TimeSpan.FromHours(24));
        }

        #endregion
    }

}