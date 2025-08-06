// Areas/Security/Middleware/UserSessionMiddleware.cs - ENHANCED VERSION
// ✅ KEEPS: Util.GetCurrentUser(), existing sessions, AD APIs intact
// ✅ ADDS: Smart caching, real-time refresh, performance optimization
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Middleware
{
    public class UserSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserSessionMiddleware> _logger;
        private readonly IMemoryCache _memoryCache;

        // Smart caching configuration
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(30); // L1 Cache: 30 seconds
        private static readonly TimeSpan SESSION_REFRESH_THRESHOLD = TimeSpan.FromMinutes(5); // Force refresh after 5 mins
        private static readonly string CACHE_KEY_PREFIX = "user_session:";
        private static readonly string REFRESH_FLAG_PREFIX = "refresh_required:";

        public UserSessionMiddleware(RequestDelegate next, ILogger<UserSessionMiddleware> logger, IMemoryCache memoryCache)
        {
            _next = next;
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task InvokeAsync(HttpContext context, ISecurityUserService userService)
        {
            // Only process authenticated users
            if (context.User.Identity.IsAuthenticated)
            {
                await EnsureUserSessionAsync(context, userService);
            }

            await _next(context);
        }

        private async Task EnsureUserSessionAsync(HttpContext context, ISecurityUserService userService)
        {
            var userName = context.User.Identity.Name ?? "UNKNOWN";

            try
            {
                // Check if user context is already set this request
                if (context.Items.ContainsKey("CurrentUser"))
                {
                    return;
                }

                // 🚀 NEW: Smart Cache Strategy - L1 Memory Cache (30 seconds)
                var cacheKey = $"{CACHE_KEY_PREFIX}{userName}";
                var refreshFlagKey = $"{REFRESH_FLAG_PREFIX}{userName}";

                // Check if forced refresh is required (from SignalR events)
                var forceRefresh = _memoryCache.Get<bool?>(refreshFlagKey) ?? false;
                if (forceRefresh)
                {
                    _memoryCache.Remove(cacheKey);
                    _memoryCache.Remove(refreshFlagKey);
                    Console.WriteLine($"🔄 [MIDDLEWARE] Force refresh triggered for: {userName}");
                }

                // Try L1 Cache first (fastest)
                var cachedUser = _memoryCache.Get<UserDto>(cacheKey);
                if (cachedUser != null && !forceRefresh)
                {
                    context.Items["CurrentUser"] = cachedUser;
                    Console.WriteLine($"⚡ [MIDDLEWARE] L1 Cache hit for: {userName}");
                    return;
                }

                // Fallback to session (existing behavior)
                var sessionUser = GetObjectFromJson<UserDto>(context.Session, "User");
                if (sessionUser != null && !forceRefresh)
                {
                    // Check if session needs refresh (time-based)
                    var lastRefreshStr = context.Session.GetString("LastRefresh");
                    if (DateTime.TryParse(lastRefreshStr, out var lastRefresh))
                    {
                        var timeSinceRefresh = DateTime.UtcNow.AddHours(3) - lastRefresh;
                        if (timeSinceRefresh < SESSION_REFRESH_THRESHOLD)
                        {
                            // Session is fresh, use it and cache it
                            _memoryCache.Set(cacheKey, sessionUser, CACHE_DURATION);
                            context.Items["CurrentUser"] = sessionUser;
                            Console.WriteLine($"✅ [MIDDLEWARE] Session valid, cached for: {userName}");
                            return;
                        }
                    }
                }

                Console.WriteLine($"🔄 [MIDDLEWARE] Refreshing from database for: {userName}");

                // Refresh from database (existing logic enhanced)
                await RefreshUserSessionAsync(context, userService);

                // Get the refreshed session and cache it
                var refreshedUser = GetObjectFromJson<UserDto>(context.Session, "User");
                if (refreshedUser != null)
                {
                    _memoryCache.Set(cacheKey, refreshedUser, CACHE_DURATION);
                    context.Items["CurrentUser"] = refreshedUser;
                    Console.WriteLine($"✅ [MIDDLEWARE] Database refresh completed and cached for: {userName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 [MIDDLEWARE] ERROR for {userName}: {ex.Message}");
                _logger.LogError(ex, "Error ensuring user session for {Username}", userName);
            }
        }

        /// <summary>
        /// 🆕 NEW: Force refresh user session (called by SignalR events)
        /// </summary>
        public void ForceRefreshUserSession(string userCode)
        {
            var refreshFlagKey = $"{REFRESH_FLAG_PREFIX}{userCode}";
            var cacheKey = $"{CACHE_KEY_PREFIX}{userCode}";

            // Set refresh flag and clear cache
            _memoryCache.Set(refreshFlagKey, true, TimeSpan.FromMinutes(1));
            _memoryCache.Remove(cacheKey);

            Console.WriteLine($"🚨 [MIDDLEWARE] Force refresh flag set for: {userCode}");
        }

        /// <summary>
        /// 🆕 NEW: Invalidate user cache (for role/permission changes)
        /// </summary>
        public void InvalidateUserCache(string userCode)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{userCode}";
            _memoryCache.Remove(cacheKey);

            Console.WriteLine($"🗑️ [MIDDLEWARE] Cache invalidated for: {userCode}");
        }

        /// <summary>
        /// 🆕 STATIC: Get middleware instance from service provider (for SignalR access)
        /// </summary>
        public static UserSessionMiddleware GetInstance(IServiceProvider serviceProvider)
        {
            // This is a workaround for SignalR to access middleware methods
            // In production, consider using a shared service instead
            return serviceProvider.GetService<UserSessionMiddleware>();
        }

        /// <summary>
        /// Refresh user session from database - ENHANCED but KEEPS existing logic
        /// </summary>
        private async Task RefreshUserSessionAsync(HttpContext context, ISecurityUserService userService)
        {
            try
            {
                var loginName = context.User.Identity.Name;
                if (string.IsNullOrEmpty(loginName))
                    return;

                // Get user from database (EXISTING LOGIC - NO CHANGES)
                var applicationUser = await userService.GetUserByCodeAsync(loginName);
                if (applicationUser == null)
                {
                    _logger.LogWarning("User not found in database: {Username}", loginName);
                    return;
                }

                // Get user roles (EXISTING LOGIC - NO CHANGES)
                var userRoles = await userService.GetUserRolesAsync(applicationUser.Id);

                // Create UserDto (EXISTING LOGIC - NO CHANGES)
                var userDto = new UserDto
                {
                    ID = applicationUser.Id,
                    Code = applicationUser.Code,
                    Email = applicationUser.Email,
                    FirstName = applicationUser.FirstName,
                    LastName = applicationUser.LastName,
                    Department = applicationUser.Department,
                    Title = applicationUser.JobTitle,

                    // ALL FLAGS properly set from entity (EXISTING LOGIC)
                    IsActive = applicationUser.IsActive,
                    IsAdmin = applicationUser.IsAdmin,
                    IsSuperAdmin = applicationUser.IsSuperAdmin,

                    LastLoginDate = applicationUser.LastLoginDate,
                    CreatedAt = applicationUser.CreatedAt,
                    ExpirationDate = applicationUser.ExpirationDate,
                    LastADInfoUpdateTime = applicationUser.LastADInfoUpdateTime,
                    UpdatedAt = applicationUser.UpdatedAt,

                    Roles = userRoles,
                    Photo = applicationUser.Photo,
                    Photo_base64 = applicationUser.Photo == null ? "" : Convert.ToBase64String(applicationUser.Photo),
                    FullName = applicationUser.FullName,
                    DisplayName = applicationUser.DisplayName,
                    Mobile = applicationUser.Mobile,
                    Tel = applicationUser.Phone
                };

                // Store in session (EXISTING LOGIC - NO CHANGES)
                SetObjectAsJson(context.Session, "User", userDto);
                context.Session.SetString("LastRefresh", DateTime.UtcNow.AddHours(3).ToString("O"));

                _logger.LogDebug("User session refreshed: {Username}", loginName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user session: {Username}", context.User.Identity?.Name);
            }
        }

        #region Session Extension Methods (EXISTING - NO CHANGES)

        private void SetObjectAsJson(ISession session, string key, object value)
        {
            if (value == null)
            {
                session.Remove(key);
                return;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            session.SetString(key, JsonSerializer.Serialize(value, options));
        }

        private T GetObjectFromJson<T>(ISession session, string key)
        {
            var value = session.GetString(key);
            if (string.IsNullOrEmpty(value))
                return default;

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                };

                return JsonSerializer.Deserialize<T>(value, options);
            }
            catch
            {
                // If deserialization fails, remove the corrupted session data
                session.Remove(key);
                return default;
            }
        }

        #endregion

        #region Background Cleanup (NEW)

        /// <summary>
        /// 🆕 NEW: Cleanup stale cache entries (called by background service)
        /// </summary>
        public void CleanupStaleCache()
        {
            // Memory cache automatically expires based on TTL
            // This method can be extended for manual cleanup if needed
            Console.WriteLine("🧹 [MIDDLEWARE] Cache cleanup completed");
        }

        #endregion
    }
}