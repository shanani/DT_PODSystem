using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Repositories.Interfaces;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Implementations
{
    public class SecurityAuditService : ISecurityAuditService
    {
        private readonly ISecurityUnitOfWork _securityUnitOfWork;
        private readonly ILogger<SecurityAuditService> _logger;

        public SecurityAuditService(
            ISecurityUnitOfWork securityUnitOfWork,
            ILogger<SecurityAuditService> logger)
        {
            _securityUnitOfWork = securityUnitOfWork;
            _logger = logger;
        }



        public async Task LogSystemActionAsync(string action, string userId, string details, string ipAddress)
        {
            try
            {
                // Basic audit log implementation
                _logger.LogInformation("System Action: {Action} by User: {UserId} from IP: {IpAddress}. Details: {Details}",
                    action, userId, ipAddress, details);

                // You can implement database logging here later if needed
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging system action: {Action}", action);
            }
        }

        public async Task LogPermissionTypeActionAsync(string action, int permissionTypeId, string permissionTypeName,
            string userId, string userName, string details, string ipAddress, bool success, string errorMessage)
        {
            try
            {
                var logMessage = success
                    ? $"Permission Type Action: {action} on '{permissionTypeName}' (ID: {permissionTypeId}) by {userName} ({userId}) from IP: {ipAddress}. Details: {details}"
                    : $"Failed Permission Type Action: {action} on '{permissionTypeName}' (ID: {permissionTypeId}) by {userName} ({userId}) from IP: {ipAddress}. Error: {errorMessage}";

                if (success)
                {
                    _logger.LogInformation(logMessage);
                }
                else
                {
                    _logger.LogWarning(logMessage);
                }

                // You can implement database logging here later if needed
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging permission type action: {Action}", action);
            }
        }

        #region Audit Log Creation

        public async Task<bool> LogSecurityEventAsync(string action, string entityType, string entityId,
            string userId, string userName, string description, string ipAddress = null, string userAgent = null,
            bool isSuccessful = true, string errorMessage = null)
        {
            try
            {
                var auditLog = new SecurityAuditLog
                {
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    UserId = userId,
                    UserName = userName,
                    Description = description,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = errorMessage,
                    CreatedAt = DateTime.UtcNow.AddHours(3)
                };

                await _securityUnitOfWork.Repository<SecurityAuditLog>().AddAsync(auditLog);
                await _securityUnitOfWork.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging security event: {Action} for {EntityType}:{EntityId}", action, entityType, entityId);
                return false;
            }
        }

        public async Task<bool> LogUserActionAsync(string action, string userId, string userName,
            string description, string ipAddress = null, string userAgent = null, bool isSuccessful = true, string errorMessage = null)
        {
            return await LogSecurityEventAsync(action, "User", userId, userId, userName, description,
                ipAddress, userAgent, isSuccessful, errorMessage);
        }

        public async Task<bool> LogRoleActionAsync(string action, int roleId, string userId, string userName,
            string description, string ipAddress = null, string userAgent = null, bool isSuccessful = true, string errorMessage = null)
        {
            return await LogSecurityEventAsync(action, "Role", roleId.ToString(), userId, userName, description,
                ipAddress, userAgent, isSuccessful, errorMessage);
        }

        public async Task<bool> LogPermissionActionAsync(string action, int permissionId, string userId, string userName,
            string description, string ipAddress = null, string userAgent = null, bool isSuccessful = true, string errorMessage = null)
        {
            return await LogSecurityEventAsync(action, "Permission", permissionId.ToString(), userId, userName, description,
                ipAddress, userAgent, isSuccessful, errorMessage);
        }

        public async Task<bool> LogLoginAttemptAsync(string userId, string userName, bool isSuccessful,
            string ipAddress = null, string userAgent = null, string errorMessage = null)
        {
            var action = isSuccessful ? "Login_Success" : "Login_Failed";
            var description = isSuccessful ?
                $"User {userName} logged in successfully" :
                $"Failed login attempt for user {userName}";

            return await LogSecurityEventAsync(action, "Authentication", userId, userId, userName, description,
                ipAddress, userAgent, isSuccessful, errorMessage);
        }

        public async Task<bool> LogLogoutAsync(string userId, string userName, string ipAddress = null, string userAgent = null)
        {
            return await LogSecurityEventAsync("Logout", "Authentication", userId, userId, userName,
                $"User {userName} logged out", ipAddress, userAgent, true);
        }

        #endregion

        #region Generic Log Method

        public async Task<bool> LogAsync(string action, string entityType = null, string entityId = null,
            string userId = null, string userName = null, string description = null,
            string ipAddress = null, string userAgent = null, bool isSuccessful = true, string errorMessage = null)
        {
            return await LogSecurityEventAsync(action, entityType ?? "System", entityId ?? "0",
                userId ?? "System", userName ?? "System", description ?? action,
                ipAddress, userAgent, isSuccessful, errorMessage);
        }

        public async Task<Dictionary<string, int>> GetAuditActivityByDateAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                var activities = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.CreatedAt >= fromDate && log.CreatedAt <= toDate)
                    .GroupBy(log => log.CreatedAt.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Date)
                    .ToDictionaryAsync(x => x.Date.ToString("yyyy-MM-dd"), x => x.Count);

                return activities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit activity by date: {FromDate} to {ToDate}", fromDate, toDate);
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Audit Log Queries

        public async Task<IEnumerable<SecurityAuditDto>> GetRecentAuditLogsAsync(int count = 50)
        {
            try
            {
                var logs = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return logs.Select(log => new SecurityAuditDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = int.TryParse(log.EntityId, out var id) ? id : (int?)null,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt,
                    IsSuccessful = log.IsSuccessful,
                    ErrorMessage = log.ErrorMessage,
                    ActionIcon = GetActionIcon(log.Action),
                    ActionColor = GetActionColor(log.Action, log.IsSuccessful),
                    RelativeTime = GetRelativeTime(log.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent audit logs");
                return new List<SecurityAuditDto>();
            }
        }

        public async Task<IEnumerable<SecurityAuditDto>> GetAuditLogsByUserAsync(string userId, int count = 50)
        {
            try
            {
                var logs = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.UserId == userId)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return logs.Select(log => new SecurityAuditDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityType = log.EntityType,

                    EntityId = int.TryParse(log.EntityId, out var id) ? id : (int?)null,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt,
                    IsSuccessful = log.IsSuccessful,
                    ErrorMessage = log.ErrorMessage,
                    ActionIcon = GetActionIcon(log.Action),
                    ActionColor = GetActionColor(log.Action, log.IsSuccessful),
                    RelativeTime = GetRelativeTime(log.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs by user: {UserId}", userId);
                return new List<SecurityAuditDto>();
            }
        }

        public async Task<IEnumerable<SecurityAuditDto>> GetAuditLogsByEntityAsync(string entityType, string entityId, int count = 50)
        {
            try
            {
                var logs = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.EntityType == entityType && log.EntityId == entityId)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return logs.Select(log => new SecurityAuditDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = int.TryParse(log.EntityId, out var id) ? id : (int?)null,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt,
                    IsSuccessful = log.IsSuccessful,
                    ErrorMessage = log.ErrorMessage,
                    ActionIcon = GetActionIcon(log.Action),
                    ActionColor = GetActionColor(log.Action, log.IsSuccessful),
                    RelativeTime = GetRelativeTime(log.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs by entity: {EntityType}:{EntityId}", entityType, entityId);
                return new List<SecurityAuditDto>();
            }
        }

        public async Task<IEnumerable<SecurityAuditDto>> GetAuditLogsByDateRangeAsync(DateTime fromDate, DateTime toDate, int count = 100)
        {
            try
            {
                var logs = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.CreatedAt >= fromDate && log.CreatedAt <= toDate)
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return logs.Select(log => new SecurityAuditDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = int.TryParse(log.EntityId, out var id) ? id : (int?)null,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt,
                    IsSuccessful = log.IsSuccessful,
                    ErrorMessage = log.ErrorMessage,
                    ActionIcon = GetActionIcon(log.Action),
                    ActionColor = GetActionColor(log.Action, log.IsSuccessful),
                    RelativeTime = GetRelativeTime(log.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit logs by date range: {FromDate} to {ToDate}", fromDate, toDate);
                return new List<SecurityAuditDto>();
            }
        }

        #endregion

        #region Audit Statistics

        public async Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _securityUnitOfWork.Repository<SecurityAuditLog>().GetQueryable();

                if (fromDate.HasValue)
                    query = query.Where(log => log.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(log => log.CreatedAt <= toDate.Value);

                var totalEvents = await query.CountAsync();
                var successfulEvents = await query.CountAsync(log => log.IsSuccessful);
                var failedEvents = totalEvents - successfulEvents;

                var uniqueUsers = await query
                    .Where(log => !string.IsNullOrEmpty(log.UserId))
                    .Select(log => log.UserId)
                    .Distinct()
                    .CountAsync();

                var uniqueIPs = await query
                    .Where(log => !string.IsNullOrEmpty(log.IpAddress))
                    .Select(log => log.IpAddress)
                    .Distinct()
                    .CountAsync();

                return new Dictionary<string, int>
                {
                    ["TotalEvents"] = totalEvents,
                    ["SuccessfulEvents"] = successfulEvents,
                    ["FailedEvents"] = failedEvents,
                    ["UniqueUsers"] = uniqueUsers,
                    ["UniqueIPs"] = uniqueIPs
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit statistics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetActionStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _securityUnitOfWork.Repository<SecurityAuditLog>().GetQueryable();

                if (fromDate.HasValue)
                    query = query.Where(log => log.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(log => log.CreatedAt <= toDate.Value);

                var actionStats = await query
                    .GroupBy(log => log.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToDictionaryAsync(x => x.Action, x => x.Count);

                return actionStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting action statistics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetEntityTypeStatisticsAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _securityUnitOfWork.Repository<SecurityAuditLog>().GetQueryable();

                if (fromDate.HasValue)
                    query = query.Where(log => log.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(log => log.CreatedAt <= toDate.Value);

                var entityStats = await query
                    .GroupBy(log => log.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .ToDictionaryAsync(x => x.EntityType, x => x.Count);

                return entityStats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting entity type statistics");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, int>> GetHourlyActivityAsync(DateTime date)
        {
            try
            {
                var startOfDay = date.Date;
                var endOfDay = startOfDay.AddDays(1);

                var hourlyActivity = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.CreatedAt >= startOfDay && log.CreatedAt < endOfDay)
                    .GroupBy(log => log.CreatedAt.Hour)
                    .Select(g => new { Hour = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Hour.ToString("D2"), x => x.Count);

                // Fill in missing hours with 0
                for (int hour = 0; hour < 24; hour++)
                {
                    var hourKey = hour.ToString("D2");
                    if (!hourlyActivity.ContainsKey(hourKey))
                        hourlyActivity[hourKey] = 0;
                }

                return hourlyActivity.OrderBy(kvp => kvp.Key).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hourly activity for date: {Date}", date);
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Security Alerts

        public async Task<IEnumerable<SecurityAuditDto>> GetFailedLoginAttemptsAsync(int count = 50)
        {
            try
            {
                var failedLogins = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.Action == "Login_Failed")
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return failedLogins.Select(log => new SecurityAuditDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = int.TryParse(log.EntityId, out var id) ? id : (int?)null,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt,
                    IsSuccessful = log.IsSuccessful,
                    ErrorMessage = log.ErrorMessage,
                    ActionIcon = "fas fa-exclamation-triangle",
                    ActionColor = "text-danger",
                    RelativeTime = GetRelativeTime(log.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed login attempts");
                return new List<SecurityAuditDto>();
            }
        }

        public async Task<IEnumerable<SecurityAuditDto>> GetSuspiciousActivitiesAsync(int count = 50)
        {
            try
            {
                var suspiciousActivities = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => !log.IsSuccessful ||
                                 log.Action.Contains("Failed") ||
                                 log.Action.Contains("Delete") ||
                                 log.Action.Contains("SuperAdmin"))
                    .OrderByDescending(log => log.CreatedAt)
                    .Take(count)
                    .ToListAsync();

                return suspiciousActivities.Select(log => new SecurityAuditDto
                {
                    Id = log.Id,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = int.TryParse(log.EntityId, out var id) ? id : (int?)null,
                    UserId = log.UserId,
                    UserName = log.UserName,
                    Description = log.Description,
                    IpAddress = log.IpAddress,
                    UserAgent = log.UserAgent,
                    CreatedAt = log.CreatedAt,
                    IsSuccessful = log.IsSuccessful,
                    ErrorMessage = log.ErrorMessage,
                    ActionIcon = GetActionIcon(log.Action),
                    ActionColor = "text-warning",
                    RelativeTime = GetRelativeTime(log.CreatedAt)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting suspicious activities");
                return new List<SecurityAuditDto>();
            }
        }

        public async Task<Dictionary<string, int>> GetFailedLoginsByIPAsync(DateTime fromDate, int count = 20)
        {
            try
            {
                var failedLoginsByIP = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.Action == "Login_Failed" &&
                                 log.CreatedAt >= fromDate &&
                                 !string.IsNullOrEmpty(log.IpAddress))
                    .GroupBy(log => log.IpAddress)
                    .Select(g => new { IP = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(count)
                    .ToDictionaryAsync(x => x.IP, x => x.Count);

                return failedLoginsByIP;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting failed logins by IP");
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Audit Log Cleanup

        public async Task<int> CleanupOldAuditLogsAsync(DateTime beforeDate)
        {
            try
            {
                var oldLogs = await _securityUnitOfWork.Repository<SecurityAuditLog>()
                    .GetQueryable()
                    .Where(log => log.CreatedAt < beforeDate)
                    .ToListAsync();

                if (oldLogs.Any())
                {
                    foreach (var log in oldLogs)
                    {
                        _securityUnitOfWork.Repository<SecurityAuditLog>().Remove(log);
                    }
                    await _securityUnitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} old audit logs before {Date}", oldLogs.Count, beforeDate);
                }

                return oldLogs.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old audit logs before: {Date}", beforeDate);
                return 0;
            }
        }

        public async Task<int> GetAuditLogCountAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityAuditLog>().CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit log count");
                return 0;
            }
        }

        #endregion

        #region Helper Methods

        private string GetActionIcon(string action)
        {
            return action.ToLower() switch
            {
                var a when a.Contains("login") => "fas fa-sign-in-alt",
                var a when a.Contains("logout") => "fas fa-sign-out-alt",
                var a when a.Contains("create") => "fas fa-plus",
                var a when a.Contains("edit") || a.Contains("update") => "fas fa-edit",
                var a when a.Contains("delete") => "fas fa-trash",
                var a when a.Contains("assign") => "fas fa-user-plus",
                var a when a.Contains("revoke") => "fas fa-user-minus",
                var a when a.Contains("lock") => "fas fa-lock",
                var a when a.Contains("unlock") => "fas fa-unlock",
                var a when a.Contains("failed") => "fas fa-exclamation-triangle",
                var a when a.Contains("superadmin") => "fas fa-crown",
                _ => "fas fa-info-circle"
            };
        }

        private string GetActionColor(string action, bool isSuccessful)
        {
            if (!isSuccessful || action.ToLower().Contains("failed"))
                return "text-danger";

            return action.ToLower() switch
            {
                var a when a.Contains("login") => "text-success",
                var a when a.Contains("logout") => "text-info",
                var a when a.Contains("create") => "text-success",
                var a when a.Contains("edit") || a.Contains("update") => "text-warning",
                var a when a.Contains("delete") => "text-danger",
                var a when a.Contains("assign") => "text-primary",
                var a when a.Contains("revoke") => "text-warning",
                var a when a.Contains("lock") => "text-danger",
                var a when a.Contains("unlock") => "text-success",
                var a when a.Contains("superadmin") => "text-purple",
                _ => "text-secondary"
            };
        }

        private string GetRelativeTime(DateTime dateTime)
        {
            var timeSpan = DateTime.UtcNow.AddHours(3) - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "Just now";
            if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} minutes ago";
            if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} hours ago";
            if (timeSpan.TotalDays < 30)
                return $"{(int)timeSpan.TotalDays} days ago";
            if (timeSpan.TotalDays < 365)
                return $"{(int)(timeSpan.TotalDays / 30)} months ago";

            return $"{(int)(timeSpan.TotalDays / 365)} years ago";
        }

        #endregion
    }
}