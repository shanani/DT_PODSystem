
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Integration; // 🚀 Add for ISessionManagerService
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;
        private readonly ISessionManagerService _sessionManager; // Missing!

        // Connection tracking for security events
        private static readonly ConcurrentDictionary<string, string> _userConnections = new ConcurrentDictionary<string, string>();
        private static readonly ConcurrentDictionary<string, DateTime> _connectionTimes = new ConcurrentDictionary<string, DateTime>();
        private static readonly ConcurrentDictionary<string, List<string>> _userGroups = new ConcurrentDictionary<string, List<string>>();

        public NotificationHub(ILogger<NotificationHub> logger, ISessionManagerService sessionManager, IServiceProvider serviceProvider, IMemoryCache memoryCache)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
            _sessionManager = sessionManager;

        }

        #region Connection Management (ENHANCED)

        public override async Task OnConnectedAsync()
        {
            try
            {
                var userCode = Context.User?.Identity?.Name;
                var connectionId = Context.ConnectionId;

                if (!string.IsNullOrEmpty(userCode))
                {
                    // Track user connection with timestamp
                    _userConnections.AddOrUpdate(userCode, connectionId, (key, oldValue) => connectionId);
                    _connectionTimes.AddOrUpdate(userCode, DateTime.UtcNow.AddHours(3), (key, oldValue) => DateTime.UtcNow.AddHours(3));

                    // Track user groups for easier management
                    var userGroups = new List<string> { "AllUsers" };

                    // Add to general notifications group
                    await Groups.AddToGroupAsync(connectionId, "AllUsers");

                    // Add to admin group if applicable
                    if (Context.User.IsAdmin())
                    {
                        await Groups.AddToGroupAsync(connectionId, "AdminUsers");
                        userGroups.Add("AdminUsers");
                    }

                    // Add to super admin group if applicable
                    if (Context.User.IsSuperAdmin())
                    {
                        await Groups.AddToGroupAsync(connectionId, "SuperAdminUsers");
                        userGroups.Add("SuperAdminUsers");
                    }

                    // Store user groups
                    _userGroups.AddOrUpdate(userCode, userGroups, (key, oldValue) => userGroups);

                    _logger.LogInformation("User {UserCode} connected to SignalR with connection {ConnectionId}", userCode, connectionId);

                    // Notify admins about new connection
                    await Clients.Group("AdminUsers").SendAsync("UserConnected", new
                    {
                        userCode,
                        connectionId,
                        timestamp = DateTime.UtcNow.AddHours(3),
                        isAdmin = Context.User.IsAdmin(),
                        isSuperAdmin = Context.User.IsSuperAdmin()
                    });
                }

                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnConnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var userCode = Context.User?.Identity?.Name;
                var connectionId = Context.ConnectionId;

                if (!string.IsNullOrEmpty(userCode))
                {
                    // Remove user connection tracking
                    _userConnections.TryRemove(userCode, out _);
                    _connectionTimes.TryRemove(userCode, out _);
                    _userGroups.TryRemove(userCode, out _);

                    _logger.LogInformation("User {UserCode} disconnected from SignalR (Connection: {ConnectionId})", userCode, connectionId);

                    // Notify admins about disconnection
                    await Clients.Group("AdminUsers").SendAsync("UserDisconnected", new
                    {
                        userCode,
                        connectionId,
                        timestamp = DateTime.UtcNow.AddHours(3),
                        reason = exception?.Message
                    });
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnDisconnectedAsync for connection {ConnectionId}", Context.ConnectionId);
            }
        }

        #endregion

        #region 🚀 NEW: Real-time Security Event Methods

        /// <summary>
        /// 🆕 Force refresh user session (called when user roles/permissions change)
        /// </summary>
        public async Task RefreshUserSession(string userCode)
        {
            try
            {
                if (string.IsNullOrEmpty(userCode))
                    return;

                // Force refresh user session cache using proper service
                _sessionManager.ForceRefreshUserSession(userCode);

                // Clear permission cache
                ClearUserPermissionCache(userCode);

                // Get connection for specific user
                if (_userConnections.TryGetValue(userCode, out string connectionId))
                {
                    // Notify specific user to refresh their session
                    await Clients.Client(connectionId).SendAsync("SessionRefreshRequired", new
                    {
                        userCode,
                        timestamp = DateTime.UtcNow.AddHours(3),
                        reason = "Security changes detected"
                    });

                    _logger.LogInformation("Session refresh sent to user {UserCode}", userCode);
                }
                else
                {
                    _logger.LogWarning("User {UserCode} not connected - session will refresh on next login", userCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing user session for {UserCode}", userCode);
            }
        }

        /// <summary>
        /// 🆕 Invalidate user permissions cache (called when permissions change)
        /// </summary>
        public async Task InvalidateUserPermissions(string userCode)
        {
            try
            {
                if (string.IsNullOrEmpty(userCode))
                    return;

                // Clear permission cache
                ClearUserPermissionCache(userCode);

                // Invalidate user cache using proper service
                _sessionManager.InvalidateUserCache(userCode);

                // Get connection for specific user
                if (_userConnections.TryGetValue(userCode, out string connectionId))
                {
                    // Notify specific user about permission changes
                    await Clients.Client(connectionId).SendAsync("PermissionsUpdated", new
                    {
                        userCode,
                        timestamp = DateTime.UtcNow.AddHours(3),
                        message = "Your permissions have been updated"
                    });

                    _logger.LogInformation("Permission update notification sent to user {UserCode}", userCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating permissions for {UserCode}", userCode);
            }
        }

        /// <summary>
        /// 🆕 Broadcast security change to all users (system-wide changes)
        /// </summary>
        public async Task BroadcastSecurityChange(string changeType, string message = null)
        {
            try
            {
                // Clear all permission cache
                ClearAllPermissionCache();

                var notification = new
                {
                    changeType,
                    message = message ?? "Security settings have been updated",
                    timestamp = DateTime.UtcNow.AddHours(3),
                    requiresRefresh = true
                };

                // Notify all connected users
                await Clients.All.SendAsync("SecurityChangeNotification", notification);

                _logger.LogInformation("Security change broadcast sent: {ChangeType}", changeType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting security change: {ChangeType}", changeType);
            }
        }

        /// <summary>
        /// 🆕 Emergency security lockdown (immediate response)
        /// </summary>
        public async Task EmergencyLockdown(string reason = null)
        {
            try
            {
                var notification = new
                {
                    type = "EMERGENCY_LOCKDOWN",
                    reason = reason ?? "Emergency security lockdown initiated",
                    timestamp = DateTime.UtcNow.AddHours(3),
                    action = "LOGOUT_REQUIRED"
                };

                // Notify all connected users to logout immediately
                await Clients.All.SendAsync("EmergencyNotification", notification);

                _logger.LogCritical("Emergency lockdown notification sent: {Reason}", reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emergency lockdown notification");
            }
        }

        /// <summary>
        /// 🆕 Notify admins about security events
        /// </summary>
        public async Task NotifyAdminsSecurityEvent(string eventType, string details, string userCode = null)
        {
            try
            {
                var notification = new
                {
                    eventType,
                    details,
                    userCode,
                    timestamp = DateTime.UtcNow.AddHours(3),
                    severity = "HIGH"
                };

                // Notify admin users only
                await Clients.Group("AdminUsers").SendAsync("SecurityEventNotification", notification);

                _logger.LogWarning("Security event notification sent to admins: {EventType} - {Details}", eventType, details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying admins about security event: {EventType}", eventType);
            }
        }

        #endregion

        #region EXISTING: General Notification Methods (PRESERVED)

        /// <summary>
        /// Send notification to specific user (EXISTING METHOD)
        /// </summary>
        public async Task SendNotificationToUser(string userCode, string title, string message, string type = "info")
        {
            try
            {
                if (_userConnections.TryGetValue(userCode, out string connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveNotification", new
                    {
                        title,
                        message,
                        type,
                        timestamp = DateTime.UtcNow.AddHours(3)
                    });

                    _logger.LogDebug("Notification sent to user {UserCode}: {Title}", userCode, title);
                }
                else
                {
                    _logger.LogWarning("User {UserCode} not connected - notification will be stored for later", userCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserCode}", userCode);
            }
        }

        /// <summary>
        /// Send notification to all users (EXISTING METHOD)
        /// </summary>
        public async Task SendNotificationToAll(string title, string message, string type = "info")
        {
            try
            {
                await Clients.All.SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    type,
                    timestamp = DateTime.UtcNow.AddHours(3)
                });

                _logger.LogInformation("Broadcast notification sent: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to all users");
            }
        }

        /// <summary>
        /// Send notification to admin users only (EXISTING METHOD)
        /// </summary>
        public async Task SendNotificationToAdmins(string title, string message, string type = "info")
        {
            try
            {
                await Clients.Group("AdminUsers").SendAsync("ReceiveNotification", new
                {
                    title,
                    message,
                    type,
                    timestamp = DateTime.UtcNow.AddHours(3)
                });

                _logger.LogInformation("Admin notification sent: {Title}", title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to admin users");
            }
        }

        #endregion

        #region 🚀 NEW: User Management Event Handlers

        /// <summary>
        /// 🆕 Handle user role assignment changes
        /// </summary>
        public async Task UserRoleChanged(string userCode, string roleName, string action)
        {
            try
            {
                // Force refresh user session and permissions
                await RefreshUserSession(userCode);

                // Notify admins about the change
                await NotifyAdminsSecurityEvent("USER_ROLE_CHANGED",
                    $"User {userCode} - Role '{roleName}' {action}", userCode);

                _logger.LogInformation("User role change processed: {UserCode} - {RoleName} {Action}", userCode, roleName, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user role change: {UserCode}", userCode);
            }
        }

        /// <summary>
        /// 🆕 Handle user status changes (active/inactive)
        /// </summary>
        public async Task UserStatusChanged(string userCode, bool isActive)
        {
            try
            {
                if (!isActive)
                {
                    // User deactivated - force immediate logout
                    if (_userConnections.TryGetValue(userCode, out string connectionId))
                    {
                        await Clients.Client(connectionId).SendAsync("ForceLogout", new
                        {
                            reason = "Account has been deactivated",
                            timestamp = DateTime.UtcNow.AddHours(3)
                        });
                    }
                }
                else
                {
                    // User reactivated - refresh session
                    await RefreshUserSession(userCode);
                }

                // Notify admins
                var status = isActive ? "activated" : "deactivated";
                await NotifyAdminsSecurityEvent("USER_STATUS_CHANGED",
                    $"User {userCode} has been {status}", userCode);

                _logger.LogInformation("User status change processed: {UserCode} - {Status}", userCode, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling user status change: {UserCode}", userCode);
            }
        }

        /// <summary>
        /// 🆕 Handle role permission changes
        /// </summary>
        public async Task RolePermissionChanged(string roleName, string permissionName, string action)
        {
            try
            {
                // This affects all users with this role - broadcast change
                await BroadcastSecurityChange("ROLE_PERMISSION_CHANGED",
                    $"Permissions for role '{roleName}' have been updated");

                // Notify admins about the specific change
                await NotifyAdminsSecurityEvent("ROLE_PERMISSION_CHANGED",
                    $"Role '{roleName}' - Permission '{permissionName}' {action}");

                _logger.LogInformation("Role permission change processed: {RoleName} - {PermissionName} {Action}", roleName, permissionName, action);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling role permission change: {RoleName}", roleName);
            }
        }

        #endregion

        #region 🚀 NEW: Client-side Event Handlers

        /// <summary>
        /// 🆕 Client requests permission refresh
        /// </summary>
        public async Task RequestPermissionRefresh()
        {
            try
            {
                var userCode = Context.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(userCode))
                {
                    await RefreshUserSession(userCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling permission refresh request from {UserCode}", Context.User?.Identity?.Name);
            }
        }

        /// <summary>
        /// 🆕 Client heartbeat (keep connection alive and check status)
        /// </summary>
        public async Task Heartbeat()
        {
            try
            {
                var userCode = Context.User?.Identity?.Name;
                if (!string.IsNullOrEmpty(userCode))
                {
                    // Update connection time
                    _connectionTimes.AddOrUpdate(userCode, DateTime.UtcNow.AddHours(3), (key, oldValue) => DateTime.UtcNow.AddHours(3));

                    // Respond with current timestamp
                    await Clients.Caller.SendAsync("HeartbeatResponse", new
                    {
                        timestamp = DateTime.UtcNow.AddHours(3),
                        userCode,
                        status = "OK"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling heartbeat from {UserCode}", Context.User?.Identity?.Name);
            }
        }

        #endregion

        #region EXISTING: Static Helper Methods (FOR NOTIFICATIONSERVICE COMPATIBILITY)

        /// <summary>
        /// 🆕 STATIC: Get connection ID for specific user (for NotificationService)
        /// </summary>
        public static string GetUserConnectionId(string userCode)
        {
            _userConnections.TryGetValue(userCode, out string connectionId);
            return connectionId;
        }

        /// <summary>
        /// 🆕 STATIC: Check if user is connected (for NotificationService)
        /// </summary>
        public static bool IsUserConnected(string userCode)
        {
            return _userConnections.ContainsKey(userCode);
        }

        /// <summary>
        /// 🆕 STATIC: Get all connected users (for NotificationService)
        /// </summary>
        public static List<string> GetConnectedUsers()
        {
            return _userConnections.Keys.ToList();
        }

        /// <summary>
        /// 🆕 STATIC: Get connection count (for monitoring)
        /// </summary>
        public static int GetConnectionCount()
        {
            return _userConnections.Count;
        }

        /// <summary>
        /// 🆕 STATIC: Get connection statistics
        /// </summary>
        public static object GetConnectionStatistics()
        {
            return new
            {
                TotalConnections = _userConnections.Count,
                ConnectedUsers = _userConnections.Keys.ToList(),
                AdminConnections = _userGroups.Count(g => g.Value.Contains("AdminUsers")),
                SuperAdminConnections = _userGroups.Count(g => g.Value.Contains("SuperAdminUsers"))
            };
        }

        #endregion

        #region Private Helper Methods

        private void ClearUserPermissionCache(string userCode)
        {
            // Use SecurityExtensions static method
            SecurityExtensions.ClearUserPermissionCache(userCode, _serviceProvider);
        }

        private void ClearAllPermissionCache()
        {
            // Use SecurityExtensions static method
            SecurityExtensions.ClearAllPermissionCache(_serviceProvider);
        }

        #endregion
    }
}