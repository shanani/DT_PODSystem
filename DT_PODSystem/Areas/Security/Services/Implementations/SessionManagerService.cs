using System;
using DT_PODSystem.Areas.Security.Integration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

/// <summary>
/// Session Manager Service Implementation
/// Clean separation of concerns for session management
/// </summary>
public class SessionManagerService : ISessionManagerService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SessionManagerService> _logger;

    // Same cache constants as middleware
    private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(30);
    private static readonly string CACHE_KEY_PREFIX = "user_session:";
    private static readonly string REFRESH_FLAG_PREFIX = "refresh_required:";

    public SessionManagerService(IMemoryCache memoryCache, ILogger<SessionManagerService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public void ForceRefreshUserSession(string userCode)
    {
        var refreshFlagKey = $"{REFRESH_FLAG_PREFIX}{userCode}";
        var cacheKey = $"{CACHE_KEY_PREFIX}{userCode}";

        _memoryCache.Set(refreshFlagKey, true, TimeSpan.FromMinutes(1));
        _memoryCache.Remove(cacheKey);

        _logger.LogInformation("Force refresh flag set for user: {UserCode}", userCode);
    }

    public void InvalidateUserCache(string userCode)
    {
        var cacheKey = $"{CACHE_KEY_PREFIX}{userCode}";
        _memoryCache.Remove(cacheKey);

        _logger.LogInformation("Cache invalidated for user: {UserCode}", userCode);
    }

    public void CleanupStaleCache()
    {
        // Memory cache automatically expires based on TTL
        _logger.LogDebug("Cache cleanup completed");
    }
}