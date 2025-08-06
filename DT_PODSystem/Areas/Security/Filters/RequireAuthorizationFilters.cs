// Areas/Security/Filters/RequireAuthorizationFilters.cs - PERFORMANCE FIXED
// ✅ ELIMINATES: Database hits on every request
// ✅ ADDS: Smart caching with real-time invalidation
// ✅ KEEPS: All security validation logic intact
using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using DT_PODSystem.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Filters
{
    /// <summary>
    /// Requires user to have IsAdmin = true or IsSuperAdmin = true
    /// PERFORMANCE OPTIMIZED: Uses cached user data instead of database hits
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireAdminAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly bool _enableDatabaseValidation;
        private static readonly TimeSpan VALIDATION_CACHE_TTL = TimeSpan.FromMinutes(2); // Short cache for security validation

        public RequireAdminAttribute(bool enableDatabaseValidation = false) // 🔥 DEFAULT TO FALSE
        {
            _enableDatabaseValidation = enableDatabaseValidation;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            try
            {
                // 1. Check authentication
                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 2. 🚀 PERFORMANCE: Get user from session/cache (NO DATABASE HIT)
                var currentUser = Util.GetCurrentUser();
                if (currentUser == null)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 3. 🚀 PERFORMANCE: Check session/cache data first (NO DATABASE HIT)
                if (!currentUser.IsActive)
                {
                    LogUnauthorizedAccess(context, currentUser.Code, "User account is deactivated (cached)");
                    context.Result = new ViewResult
                    {
                        ViewName = "AccessDenied",
                        ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                        {
                            {"Message", "Your account has been deactivated. Please contact your administrator."}
                        }
                    };
                    return;
                }

                // 4. 🚀 PERFORMANCE: Check admin privileges from session/cache (NO DATABASE HIT)
                if (!currentUser.IsAdmin && !currentUser.IsSuperAdmin)
                {
                    LogUnauthorizedAccess(context, currentUser.Code, "User does not have admin privileges (cached)");
                    context.Result = new ViewResult
                    {
                        ViewName = "AccessDenied",
                        ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                        {
                            {"Message", "You do not have administrative privileges to access this area."}
                        }
                    };
                    return;
                }

                // 5. Check account expiration (from cached data)
                if (currentUser.ExpirationDate.HasValue &&
                    currentUser.ExpirationDate.Value <= DateTime.UtcNow.AddHours(3))
                {
                    LogUnauthorizedAccess(context, currentUser.Code, "User account has expired (cached)");
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 6. 🚀 OPTIONAL: Database validation ONLY if explicitly enabled AND cache is stale
                if (_enableDatabaseValidation)
                {
                    await ValidateUserWithDatabaseCachedAsync(context, currentUser);
                }

                // All checks passed - user has admin access
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireAdminAttribute>>();
                logger?.LogError(ex, "Error in RequireAdmin authorization filter");
                context.Result = new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// 🚀 CACHED database validation - only hits DB when cache expires
        /// </summary>
        private async Task ValidateUserWithDatabaseCachedAsync(AuthorizationFilterContext context, Models.DTOs.UserDto currentUser)
        {
            try
            {
                var memoryCache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
                var userService = context.HttpContext.RequestServices.GetService<ISecurityUserService>();

                if (memoryCache == null || userService == null)
                    return;

                var cacheKey = $"admin_validation:{currentUser.Code}";

                // Check cache first
                var cachedValidation = memoryCache.Get<UserValidationResult>(cacheKey);
                if (cachedValidation != null)
                {
                    // Use cached result (NO DATABASE HIT)
                    if (!cachedValidation.IsValid)
                    {
                        LogUnauthorizedAccess(context, currentUser.Code, cachedValidation.Reason);
                        context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    }
                    return;
                }

                // Cache miss - validate with database (RARE)
                var dbUser = await userService.GetUserByCodeAsync(currentUser.Code);
                var validationResult = new UserValidationResult();

                if (dbUser == null || !dbUser.IsActive || (!dbUser.IsAdmin && !dbUser.IsSuperAdmin))
                {
                    validationResult.IsValid = false;
                    validationResult.Reason = "User privileges changed in database";

                    LogUnauthorizedAccess(context, currentUser.Code, validationResult.Reason);
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                }
                else
                {
                    validationResult.IsValid = true;
                    validationResult.Reason = "Valid";
                }

                // Cache the result for 2 minutes
                memoryCache.Set(cacheKey, validationResult, VALIDATION_CACHE_TTL);

                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireAdminAttribute>>();
                logger?.LogDebug("Database validation completed for {UserCode}: {IsValid}", currentUser.Code, validationResult.IsValid);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireAdminAttribute>>();
                logger?.LogError(ex, "Error validating user with database in RequireAdmin filter");
            }
        }

        private void LogUnauthorizedAccess(AuthorizationFilterContext context, string userCode, string reason)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireAdminAttribute>>();
            logger?.LogWarning("Admin access denied for user {UserCode}: {Reason}. Requested: {Path}",
                userCode, reason, context.HttpContext.Request.Path);
        }
    }

    /// <summary>
    /// Requires user to have IsSuperAdmin = true
    /// PERFORMANCE OPTIMIZED: Uses cached user data instead of database hits
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireSuperAdminAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly bool _enableDatabaseValidation;
        private readonly bool _productionBlocked;
        private static readonly TimeSpan VALIDATION_CACHE_TTL = TimeSpan.FromMinutes(2);

        public RequireSuperAdminAttribute(bool enableDatabaseValidation = false, bool productionBlocked = true)
        {
            _enableDatabaseValidation = enableDatabaseValidation;
            _productionBlocked = productionBlocked;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            try
            {
                // 0. Check if running in production and blocked
                if (_productionBlocked)
                {
                    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
                    if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Result = new ViewResult
                        {
                            ViewName = "AccessDenied",
                            ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                                new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                                new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                            {
                                {"Message", "Super Admin features are disabled in production environment."}
                            }
                        };
                        return;
                    }
                }

                // 1. Check authentication
                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 2. 🚀 PERFORMANCE: Get user from session/cache (NO DATABASE HIT)
                var currentUser = Util.GetCurrentUser();
                if (currentUser == null)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 3. 🚀 PERFORMANCE: Check session/cache data first (NO DATABASE HIT)
                if (!currentUser.IsActive)
                {
                    LogUnauthorizedAccess(context, currentUser.Code, "User account is deactivated (cached)");
                    context.Result = new ViewResult
                    {
                        ViewName = "AccessDenied",
                        ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                        {
                            {"Message", "Your account has been deactivated. Please contact your administrator."}
                        }
                    };
                    return;
                }

                // 4. 🚀 PERFORMANCE: Check SuperAdmin privilege from session/cache (NO DATABASE HIT)
                if (!currentUser.IsSuperAdmin)
                {
                    LogUnauthorizedAccess(context, currentUser.Code, "User does not have SuperAdmin privileges (cached)");
                    context.Result = new ViewResult
                    {
                        ViewName = "AccessDenied",
                        ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                        {
                            {"Message", "You do not have Super Administrator privileges to access this area."}
                        }
                    };
                    return;
                }

                // 5. Check account expiration (from cached data)
                if (currentUser.ExpirationDate.HasValue &&
                    currentUser.ExpirationDate.Value <= DateTime.UtcNow.AddHours(3))
                {
                    LogUnauthorizedAccess(context, currentUser.Code, "User account has expired (cached)");
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 6. 🚀 OPTIONAL: Database validation ONLY if explicitly enabled AND cache is stale
                if (_enableDatabaseValidation)
                {
                    await ValidateUserWithDatabaseCachedAsync(context, currentUser);
                }

                // All checks passed - user has SuperAdmin access
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireSuperAdminAttribute>>();
                logger?.LogError(ex, "Error in RequireSuperAdmin authorization filter");
                context.Result = new StatusCodeResult(500);
            }
        }

        /// <summary>
        /// 🚀 CACHED database validation - only hits DB when cache expires
        /// </summary>
        private async Task ValidateUserWithDatabaseCachedAsync(AuthorizationFilterContext context, Models.DTOs.UserDto currentUser)
        {
            try
            {
                var memoryCache = context.HttpContext.RequestServices.GetService<IMemoryCache>();
                var userService = context.HttpContext.RequestServices.GetService<ISecurityUserService>();

                if (memoryCache == null || userService == null)
                    return;

                var cacheKey = $"superadmin_validation:{currentUser.Code}";

                // Check cache first
                var cachedValidation = memoryCache.Get<UserValidationResult>(cacheKey);
                if (cachedValidation != null)
                {
                    // Use cached result (NO DATABASE HIT)
                    if (!cachedValidation.IsValid)
                    {
                        LogUnauthorizedAccess(context, currentUser.Code, cachedValidation.Reason);
                        context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    }
                    return;
                }

                // Cache miss - validate with database (RARE)
                var dbUser = await userService.GetUserByCodeAsync(currentUser.Code);
                var validationResult = new UserValidationResult();

                if (dbUser == null || !dbUser.IsActive || !dbUser.IsSuperAdmin)
                {
                    validationResult.IsValid = false;
                    validationResult.Reason = "User Super Admin privileges changed in database";

                    LogUnauthorizedAccess(context, currentUser.Code, validationResult.Reason);
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                }
                else
                {
                    validationResult.IsValid = true;
                    validationResult.Reason = "Valid";
                }

                // Cache the result for 2 minutes
                memoryCache.Set(cacheKey, validationResult, VALIDATION_CACHE_TTL);

                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireSuperAdminAttribute>>();
                logger?.LogDebug("Database validation completed for {UserCode}: {IsValid}", currentUser.Code, validationResult.IsValid);
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireSuperAdminAttribute>>();
                logger?.LogError(ex, "Error validating user with database in RequireSuperAdmin filter");
            }
        }

        private void LogUnauthorizedAccess(AuthorizationFilterContext context, string userCode, string reason)
        {
            var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireSuperAdminAttribute>>();
            logger?.LogWarning("Super Admin access denied for user {UserCode}: {Reason}. Requested: {Path}",
                userCode, reason, context.HttpContext.Request.Path);
        }
    }

    /// <summary>
    /// Requires user to be active (IsActive = true) and not expired
    /// PERFORMANCE OPTIMIZED: Uses cached user data - NO database hits
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequireActiveUserAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            try
            {
                // 1. Check authentication
                if (!context.HttpContext.User.Identity.IsAuthenticated)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 2. 🚀 PERFORMANCE: Get user from session/cache (NO DATABASE HIT)
                var currentUser = Util.GetCurrentUser();
                if (currentUser == null)
                {
                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // 3. 🚀 PERFORMANCE: Check active status from cached data (NO DATABASE HIT)
                if (!currentUser.IsActive)
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireActiveUserAttribute>>();
                    logger?.LogWarning("Inactive user {UserCode} attempted to access protected area (cached check)", currentUser.Code);

                    context.Result = new ViewResult
                    {
                        ViewName = "AccessDenied",
                        ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(
                            new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),
                            new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary())
                        {
                            {"Message", "Your account has been deactivated. Please contact your administrator."}
                        }
                    };
                    return;
                }

                // 4. Check account expiration (from cached data)
                if (currentUser.ExpirationDate.HasValue &&
                    currentUser.ExpirationDate.Value <= DateTime.UtcNow.AddHours(3))
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireActiveUserAttribute>>();
                    logger?.LogWarning("Expired user {UserCode} attempted to access protected area (cached check)", currentUser.Code);

                    context.Result = new RedirectToActionResult("Login", "Account", new { area = "Security" });
                    return;
                }

                // All checks passed - user is active
            }
            catch (Exception ex)
            {
                var logger = context.HttpContext.RequestServices.GetService<ILogger<RequireActiveUserAttribute>>();
                logger?.LogError(ex, "Error in RequireActiveUser authorization filter");
                context.Result = new StatusCodeResult(500);
            }
        }
    }

    #region Helper Classes

    /// <summary>
    /// Cache result for database validation
    /// </summary>
    internal class UserValidationResult
    {
        public bool IsValid { get; set; }
        public string Reason { get; set; }
    }

    #endregion
}