using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Repositories.Interfaces;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Services.Implementations
{
    public class SecurityUserService : ISecurityUserService
    {
        private readonly SecurityDbContext _securityContext; // ✅ Use SecurityDbContext
        private readonly ISecurityUnitOfWork _securityUnitOfWork; // ✅ Use Security Unit of Work

        private readonly ILogger<SecurityUserService> _logger;

        public SecurityUserService(
            ISecurityUnitOfWork securityUnitOfWork,
            SecurityDbContext securityContext,
            ILogger<SecurityUserService> logger)
        {
            _securityUnitOfWork = securityUnitOfWork;
            _securityContext = securityContext;
            _logger = logger;
        }


        public async Task<IEnumerable<SecurityUser>> GetAllUsersAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<SecurityUser>();
            }
        }


        public async Task<SecurityUser> SyncUserWithADAsync(SecurityUser localUser, ADUserDetails adUser)
        {
            try
            {
                if (localUser == null || adUser == null)
                {
                    _logger.LogWarning("Cannot sync user - localUser or adUser is null");
                    return localUser;
                }

                // 🕐 CHECK IF SYNC IS NEEDED (6 hours rule)
                var lastUpdate = localUser.LastADInfoUpdateTime ?? DateTime.MinValue;
                var hoursSinceLastUpdate = (DateTime.UtcNow.AddHours(3) - lastUpdate).TotalHours;

                if (hoursSinceLastUpdate < 6)
                {
                    _logger.LogDebug("AD sync skipped for user {UserCode} - last update was {Hours:F1} hours ago (less than 6 hours)",
                        localUser.Code, hoursSinceLastUpdate);
                    return localUser;
                }

                _logger.LogInformation("AD sync needed for user {UserCode} - last update was {Hours:F1} hours ago",
                    localUser.Code, hoursSinceLastUpdate);

                // Track if any changes were made
                bool hasChanges = false;

                // 📝 SYNC ALL FIELDS (STATIC - NOT CONFIGURABLE)
                if (localUser.FirstName != adUser.FirstName)
                {
                    localUser.FirstName = adUser.FirstName;
                    hasChanges = true;
                }

                if (localUser.LastName != adUser.LastName)
                {
                    localUser.LastName = adUser.LastName;
                    hasChanges = true;
                }

                if (localUser.Email != adUser.EmailAddress)
                {
                    localUser.Email = adUser.EmailAddress;
                    hasChanges = true;
                }

                if (localUser.Department != adUser.Department)
                {
                    localUser.Department = adUser.Department;
                    hasChanges = true;
                }

                if (!string.IsNullOrEmpty(adUser.Title) && localUser.JobTitle != adUser.Title)
                {
                    localUser.JobTitle = adUser.Title;
                    hasChanges = true;
                }

                // 🖼️ SYNC PHOTO FROM AD
                if (adUser.Photo != null && adUser.Photo.Length > 0)
                {
                    // Check if photo has changed (compare byte arrays)
                    if (localUser.Photo == null || !localUser.Photo.SequenceEqual(adUser.Photo))
                    {
                        localUser.Photo = adUser.Photo;

                        hasChanges = true;
                        _logger.LogDebug("Photo updated from AD for user: {UserCode}", localUser.Code);
                    }
                }

                // 🕐 ALWAYS UPDATE LastADInfoUpdateTime (even if no other changes)
                localUser.LastADInfoUpdateTime = DateTime.UtcNow.AddHours(3);
                localUser.UpdatedAt = DateTime.UtcNow.AddHours(3);
                localUser.UpdatedBy = "AD-AutoSync";

                await _securityContext.SaveChangesAsync();

                if (hasChanges)
                {
                    _logger.LogInformation("User auto-synced from AD with changes: {UserCode} at {Time}",
                        localUser.Code, DateTime.UtcNow.AddHours(3));
                }
                else
                {
                    _logger.LogInformation("User AD timestamp updated (no field changes): {UserCode} at {Time}",
                        localUser.Code, DateTime.UtcNow.AddHours(3));
                }

                return localUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user with AD: {UserCode}", localUser?.Code);
                throw;
            }
        }


        public async Task<SecurityUser> CreateUserAsync(SecurityUser user, string createdBy)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                // Check if user already exists
                var existingUser = await _securityContext.SecurityUsers
                    .FirstOrDefaultAsync(u => u.Code.ToLower() == user.Code.ToLower());

                if (existingUser != null)
                    throw new InvalidOperationException($"User with code '{user.Code}' already exists");

                // Set audit fields
                user.CreatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.CreatedBy = createdBy ?? "System";
                user.UpdatedBy = createdBy ?? "System";

                // ✅ Use SecurityDbContext directly
                _securityContext.SecurityUsers.Add(user);
                await _securityContext.SaveChangesAsync();

                _logger.LogInformation("User created: {UserCode} by {CreatedBy}", user.Code, createdBy);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user: {UserCode}", user?.Code);
                throw;
            }
        }

        public async Task<SecurityUser> GetOrCreateUserFromADAsync(string userCode, ADUserDetails adUser)
        {
            try
            {
                // ✅ Use SecurityDbContext to check for existing user
                var existingUser = await _securityContext.SecurityUsers
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Code.ToLower() == userCode.ToLower());

                if (existingUser != null)
                {
                    // Update existing user with AD info
                    existingUser.FirstName = adUser.FirstName;
                    existingUser.LastName = adUser.LastName;
                    existingUser.Email = adUser.EmailAddress;
                    existingUser.Department = adUser.Department;
                    existingUser.JobTitle = adUser.Title;
                    existingUser.UpdatedAt = DateTime.UtcNow.AddHours(3);

                    await _securityContext.SaveChangesAsync();
                    return existingUser;
                }

                // Create new user from AD
                var newUser = new SecurityUser
                {
                    Code = userCode,
                    FirstName = adUser.FirstName,
                    LastName = adUser.LastName,
                    Email = adUser.EmailAddress,
                    Department = adUser.Department,
                    JobTitle = adUser.Title,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddHours(3),
                    UpdatedAt = DateTime.UtcNow.AddHours(3),
                    CreatedBy = "AD_System",
                    UpdatedBy = "AD_System"
                };

                return await CreateUserAsync(newUser, "AD_System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user from AD: {UserCode}", userCode);
                throw;
            }
        }


        #region Basic CRUD

        public async Task<SecurityUser> GetUserByCodeAsync(string userCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userCode))
                    return null;

                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Code.ToLower() == userCode.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by code: {UserCode}", userCode);
                return null;
            }
        }

        public async Task<SecurityUser> GetUserByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<SecurityUser> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }



        #endregion

        #region Self-Protection Methods

        /// <summary>
        /// Check if current user can modify the target user (prevents self-modification of critical properties)
        /// </summary>
        public async Task<bool> CanUserModifyAsync(int targetUserId, string currentUserCode)
        {
            try
            {
                var targetUser = await GetUserByIdAsync(targetUserId);
                var currentUser = await GetUserByCodeAsync(currentUserCode);

                if (targetUser == null || currentUser == null)
                    return false;

                // Users cannot modify themselves
                if (targetUser.Code.Equals(currentUserCode, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User {CurrentUser} attempted to modify themselves", currentUserCode);
                    return false;
                }

                // Super Admins can modify anyone (except themselves)
                if (currentUser.IsSuperAdmin)
                    return true;

                // Regular Admins cannot modify Super Admins
                if (targetUser.IsSuperAdmin && !currentUser.IsSuperAdmin)
                {
                    _logger.LogWarning("Admin {CurrentUser} attempted to modify Super Admin {TargetUser}",
                        currentUserCode, targetUser.Code);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user modification permissions for {CurrentUser} -> {TargetUserId}",
                    currentUserCode, targetUserId);
                return false;
            }
        }

        /// <summary>
        /// Check if current user can delete the target user
        /// </summary>
        public async Task<bool> CanUserDeleteAsync(int targetUserId, string currentUserCode)
        {
            try
            {
                var targetUser = await GetUserByIdAsync(targetUserId);
                var currentUser = await GetUserByCodeAsync(currentUserCode);

                if (targetUser == null || currentUser == null)
                    return false;

                // Users cannot delete themselves
                if (targetUser.Code.Equals(currentUserCode, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User {CurrentUser} attempted to delete themselves", currentUserCode);
                    return false;
                }

                // Super Admins can delete anyone (except themselves)
                if (currentUser.IsSuperAdmin)
                    return true;

                // Regular Admins cannot delete Super Admins
                if (targetUser.IsSuperAdmin && !currentUser.IsSuperAdmin)
                {
                    _logger.LogWarning("Admin {CurrentUser} attempted to delete Super Admin {TargetUser}",
                        currentUserCode, targetUser.Code);
                    return false;
                }

                // Regular Admins cannot delete other Admins
                if (targetUser.IsAdmin && currentUser.IsAdmin && !currentUser.IsSuperAdmin)
                {
                    _logger.LogWarning("Admin {CurrentUser} attempted to delete another Admin {TargetUser}",
                        currentUserCode, targetUser.Code);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user deletion permissions for {CurrentUser} -> {TargetUserId}",
                    currentUserCode, targetUserId);
                return false;
            }
        }

        /// <summary>
        /// Check if current user can deactivate the target user
        /// </summary>
        public async Task<bool> CanUserDeactivateAsync(int targetUserId, string currentUserCode)
        {
            try
            {
                var targetUser = await GetUserByIdAsync(targetUserId);
                var currentUser = await GetUserByCodeAsync(currentUserCode);

                if (targetUser == null || currentUser == null)
                    return false;

                // Users cannot deactivate themselves
                if (targetUser.Code.Equals(currentUserCode, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("User {CurrentUser} attempted to deactivate themselves", currentUserCode);
                    return false;
                }

                // Super Admins can deactivate anyone (except themselves)
                if (currentUser.IsSuperAdmin)
                    return true;

                // Regular Admins cannot deactivate Super Admins
                if (targetUser.IsSuperAdmin && !currentUser.IsSuperAdmin)
                {
                    _logger.LogWarning("Admin {CurrentUser} attempted to deactivate Super Admin {TargetUser}",
                        currentUserCode, targetUser.Code);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user deactivation permissions for {CurrentUser} -> {TargetUserId}",
                    currentUserCode, targetUserId);
                return false;
            }
        }

        /// <summary>
        /// Comprehensive validation for user modification including admin status changes
        /// </summary>
        public async Task<(bool CanModify, string Reason)> ValidateUserModificationAsync(SecurityUser targetUser, SecurityUser updatedUser, string currentUserCode)
        {
            try
            {
                var currentUser = await GetUserByCodeAsync(currentUserCode);
                if (currentUser == null)
                    return (false, "Current user not found");

                if (targetUser == null)
                    return (false, "Target user not found");

                // Check basic modification permissions first
                if (!await CanUserModifyAsync(targetUser.Id, currentUserCode))
                    return (false, "You don't have permission to modify this user");

                // Check for self-modification attempts
                if (targetUser.Code.Equals(currentUserCode, StringComparison.OrdinalIgnoreCase))
                {
                    // Check if trying to downgrade own admin status
                    if (targetUser.IsSuperAdmin && !updatedUser.IsSuperAdmin)
                        return (false, "You cannot remove your own Super Admin privileges");

                    if (targetUser.IsAdmin && !updatedUser.IsAdmin)
                        return (false, "You cannot remove your own Admin privileges");

                    // Check if trying to deactivate themselves
                    if (targetUser.IsActive && !updatedUser.IsActive)
                        return (false, "You cannot deactivate your own account");

                    _logger.LogWarning("User {CurrentUser} attempted self-modification", currentUserCode);
                    return (false, "You cannot modify your own account");
                }

                // Check admin status changes
                if (targetUser.IsSuperAdmin != updatedUser.IsSuperAdmin)
                {
                    if (!currentUser.IsSuperAdmin)
                        return (false, "Only Super Admins can modify Super Admin status");
                }

                if (targetUser.IsAdmin != updatedUser.IsAdmin)
                {
                    if (!currentUser.IsSuperAdmin && !currentUser.IsAdmin)
                        return (false, "Only Admins can modify Admin status");
                }

                // Check status changes
                if (targetUser.IsActive != updatedUser.IsActive)
                {
                    if (!await CanUserDeactivateAsync(targetUser.Id, currentUserCode))
                        return (false, "You don't have permission to change this user's status");
                }

                return (true, "Modification allowed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user modification for {CurrentUser} -> {TargetUser}",
                    currentUserCode, targetUser?.Code);
                return (false, "Error validating modification permissions");
            }
        }

        /// <summary>
        /// Toggle user status (Active/Inactive) with proper business logic and protection checks
        /// </summary>
        public async Task<(bool Success, string Message, bool NewStatus)> ToggleUserStatusAsync(int userId, string updatedBy)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    return (false, "User not found", false);
                }

                // SELF-PROTECTION CHECK
                if (!await CanUserDeactivateAsync(userId, updatedBy))
                {
                    var targetUserName = user.FullName;
                    _logger.LogWarning("User status toggle blocked. User: {UpdatedBy} -> Target: {TargetUser}",
                        updatedBy, user.Code);

                    // Provide specific error messages based on the situation
                    var currentUser = await GetUserByCodeAsync(updatedBy);
                    if (currentUser != null && user.Code.Equals(updatedBy, StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "You cannot change your own account status", user.IsActive);
                    }
                    else if (user.IsSuperAdmin && currentUser != null && !currentUser.IsSuperAdmin)
                    {
                        return (false, "You don't have permission to change a Super Admin's status", user.IsActive);
                    }
                    else
                    {
                        return (false, "You don't have permission to change this user's status", user.IsActive);
                    }
                }

                // Store original status for logging
                var originalStatus = user.IsActive;
                var newStatus = !user.IsActive;

                // Update user status
                user.IsActive = newStatus;
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedBy = updatedBy ?? "System";

                // Save changes
                _securityUnitOfWork.Repository<SecurityUser>().Update(user);
                await _securityUnitOfWork.SaveChangesAsync();

                // Create success message
                var action = newStatus ? "activated" : "deactivated";
                var message = $"User '{user.FullName}' {action} successfully";

                _logger.LogInformation("User status toggled: {UserCode} {Action} by {UpdatedBy}",
                    user.Code, action, updatedBy);

                return (true, message, newStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status: {UserId} by {UpdatedBy}", userId, updatedBy);
                return (false, "An error occurred while updating user status", false);
            }
        }

        #endregion

        public async Task<SecurityUser> UpdateUserAsync(SecurityUser user, string updatedBy = null)
        {
            try
            {
                if (user == null)
                    throw new ArgumentNullException(nameof(user));

                // Get the original user from database for comparison
                var originalUser = await GetUserByIdAsync(user.Id);
                if (originalUser == null)
                    throw new InvalidOperationException($"User with ID {user.Id} not found");

                // SELF-PROTECTION CHECK
                var validation = await ValidateUserModificationAsync(originalUser, user, updatedBy);
                if (!validation.CanModify)
                {
                    _logger.LogWarning("User modification blocked: {Reason}. User: {UpdatedBy} -> Target: {TargetUser}",
                        validation.Reason, updatedBy, originalUser.Code);
                    throw new UnauthorizedAccessException(validation.Reason);
                }

                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedBy = updatedBy ?? "System";

                _securityUnitOfWork.Repository<SecurityUser>().Update(user);
                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("User updated: {UserCode} by {UpdatedBy}", user.Code, updatedBy);
                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user: {UserCode}", user?.Code);
                throw;
            }
        }

        // ============================================================================================
        // 4. UPDATE YOUR EXISTING DeleteUserAsync METHOD
        // ============================================================================================

        public async Task<bool> DeleteUserAsync(int userId, string deletedBy = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                    return false;

                // SELF-PROTECTION CHECK
                if (!await CanUserDeleteAsync(userId, deletedBy))
                {
                    _logger.LogWarning("User deletion blocked. User: {DeletedBy} -> Target: {TargetUser}",
                        deletedBy, user.Code);
                    throw new UnauthorizedAccessException("You don't have permission to delete this user");
                }

                // First, remove all user roles
                var userRoles = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.UserId == userId)
                    .ToListAsync();

                foreach (var userRole in userRoles)
                {
                    _securityUnitOfWork.Repository<SecurityUserRole>().Remove(userRole);
                }

                // Then remove the user completely
                _securityUnitOfWork.Repository<SecurityUser>().Remove(user);
                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("User hard deleted: {UserCode} by {DeletedBy}", user.Code, deletedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hard deleting user: {UserId}", userId);
                throw; // Re-throw to let controller handle the specific error
            }
        }





        #region Integration with Main Project

        public async Task<SecurityUser> SyncUserFromMainProjectAsync(string userCode, object mainProjectUser)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userCode) || mainProjectUser == null)
                    return null;

                var existingUser = await GetUserByCodeAsync(userCode);

                // Use reflection to get properties from mainProjectUser
                var userType = mainProjectUser.GetType();
                var firstName = userType.GetProperty("FirstName")?.GetValue(mainProjectUser)?.ToString() ?? "";
                var lastName = userType.GetProperty("LastName")?.GetValue(mainProjectUser)?.ToString() ?? "";
                var email = userType.GetProperty("Email")?.GetValue(mainProjectUser)?.ToString() ?? "";
                var department = userType.GetProperty("Department")?.GetValue(mainProjectUser)?.ToString();
                var isActive = (bool?)userType.GetProperty("IsActive")?.GetValue(mainProjectUser) ?? true;

                if (existingUser != null)
                {
                    // Update existing user
                    existingUser.FirstName = firstName;
                    existingUser.LastName = lastName;
                    existingUser.Email = email;
                    existingUser.Department = department;
                    existingUser.IsActive = isActive;

                    return await UpdateUserAsync(existingUser, "MainProject-Sync");
                }
                else
                {
                    // Create new user
                    var newUser = new SecurityUser
                    {
                        Code = userCode.ToLower(),
                        FirstName = firstName,
                        LastName = lastName,
                        Email = email,
                        Department = department,
                        IsActive = isActive,
                        IsSuperAdmin = false

                    };

                    return await CreateUserAsync(newUser, "MainProject-Sync");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing user from main project: {UserCode}", userCode);
                return null;
            }
        }

        public async Task<SecurityUser> GetOrCreateUserAsync(string userCode, string firstName, string lastName, string email, string department = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userCode))
                    return null;

                var existingUser = await GetUserByCodeAsync(userCode);
                if (existingUser != null)
                    return existingUser;

                var newUser = new SecurityUser
                {
                    Code = userCode.ToLower(),
                    FirstName = firstName ?? "",
                    LastName = lastName ?? "",
                    Email = email ?? "",
                    Department = department,
                    IsActive = true,
                    IsSuperAdmin = false

                };

                return await CreateUserAsync(newUser, "GetOrCreate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting or creating user: {UserCode}", userCode);
                return null;
            }
        }

        #endregion

        #region Role Management

        public async Task<bool> AddUserToRoleAsync(int userId, string roleName, string assignedBy = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return false;

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User not found: {UserId}", userId);
                    return false;
                }

                var role = await _securityUnitOfWork.Repository<SecurityRole>()
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
                if (role == null)
                {
                    _logger.LogWarning("Role not found: {RoleName}", roleName);
                    return false;
                }

                // Check if user already has this role
                var existingUserRole = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);

                if (existingUserRole != null)
                {
                    if (existingUserRole.IsActive)
                    {
                        _logger.LogInformation("User already has role: {UserCode} - {RoleName}", user.Code, roleName);
                        return true; // Already has active role
                    }
                    else
                    {
                        // Reactivate existing role
                        existingUserRole.IsActive = true;
                        existingUserRole.AssignedAt = DateTime.UtcNow.AddHours(3);
                        existingUserRole.AssignedBy = assignedBy ?? "System";
                        existingUserRole.RevokedAt = null;
                        existingUserRole.RevokedBy = null;
                        _securityUnitOfWork.Repository<SecurityUserRole>().Update(existingUserRole);
                    }
                }
                else
                {
                    // Create new user role
                    var newUserRole = new SecurityUserRole
                    {
                        UserId = userId,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow.AddHours(3),
                        AssignedBy = assignedBy ?? "System",
                        IsActive = true
                    };

                    await _securityUnitOfWork.Repository<SecurityUserRole>().AddAsync(newUserRole);
                }

                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Role added to user: {UserCode} - {RoleName} by {AssignedBy}", user.Code, roleName, assignedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding role to user: {UserId} - {RoleName}", userId, roleName);
                return false;
            }
        }

        public async Task<bool> RemoveUserFromRoleAsync(int userId, string roleName, string revokedBy = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return false;

                var user = await GetUserByIdAsync(userId);
                if (user == null)
                    return false;

                var role = await _securityUnitOfWork.Repository<SecurityRole>()
                    .FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
                if (role == null)
                    return false;

                var userRole = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id && ur.IsActive);

                if (userRole == null)
                    return false;

                // Soft delete by deactivating
                userRole.IsActive = false;
                userRole.RevokedAt = DateTime.UtcNow.AddHours(3);
                userRole.RevokedBy = revokedBy ?? "System";

                _securityUnitOfWork.Repository<SecurityUserRole>().Update(userRole);
                await _securityUnitOfWork.SaveChangesAsync();

                _logger.LogInformation("Role removed from user: {UserCode} - {RoleName} by {RevokedBy}", user.Code, roleName, revokedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role from user: {UserId} - {RoleName}", userId, roleName);
                return false;
            }
        }

        public async Task<List<string>> GetUserRolesAsync(int userId)
        {
            try
            {
                var userRoles = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.UserId == userId && ur.IsActive)
                    .Include(ur => ur.Role)
                    .Select(ur => ur.Role.Name)
                    .ToListAsync();

                return userRoles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles: {UserId}", userId);
                return new List<string>();
            }
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            try
            {
                var userRoles = await GetUserRolesAsync(userId);
                return userRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user is in role: {UserId} - {RoleName}", userId, roleName);
                return false;
            }
        }

        #endregion

        #region User Management

        public async Task<IEnumerable<SecurityUser>> GetActiveUsersAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Where(u => u.IsActive)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users");
                return new List<SecurityUser>();
            }
        }

        public async Task<IEnumerable<SecurityUser>> GetUsersByRoleAsync(string roleName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleName))
                    return new List<SecurityUser>();

                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Where(u => u.IsActive && u.UserRoles.Any(ur => ur.IsActive && ur.Role.Name.ToLower() == roleName.ToLower()))
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users by role: {RoleName}", roleName);
                return new List<SecurityUser>();
            }
        }

        public async Task<bool> LockUserAsync(int userId, DateTime? lockoutEnd = null, string lockedBy = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                    return false;
                user.IsActive = false;
                user.LockoutEnd = lockoutEnd;
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedBy = lockedBy ?? "System";

                await UpdateUserAsync(user, lockedBy);

                _logger.LogInformation("User locked: {UserCode} by {LockedBy}", user.Code, lockedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking user: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnlockUserAsync(int userId, string unlockedBy = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                    return false;

                user.IsActive = true;
                user.LockoutEnd = null;
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedBy = unlockedBy ?? "System";

                await UpdateUserAsync(user, unlockedBy);

                _logger.LogInformation("User unlocked: {UserCode} by {UnlockedBy}", user.Code, unlockedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user: {UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Super Admin Functions

        public async Task<bool> SetSuperAdminAsync(int userId, bool isSuperAdmin, string updatedBy = null)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null)
                    return false;

                var previousStatus = user.IsSuperAdmin;
                user.IsSuperAdmin = isSuperAdmin;
                user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                user.UpdatedBy = updatedBy ?? "System";

                await UpdateUserAsync(user, updatedBy);

                _logger.LogInformation("Super admin status changed: {UserCode} from {PreviousStatus} to {NewStatus} by {UpdatedBy}",
                    user.Code, previousStatus, isSuperAdmin, updatedBy);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting super admin status: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<SecurityUser>> GetSuperAdminsAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .Where(u => u.IsSuperAdmin && u.IsActive)
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.FirstName)
                    .ThenBy(u => u.LastName)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting super admins");
                return new List<SecurityUser>();
            }
        }

        #endregion

        #region Statistics

        public async Task<int> GetTotalUsersAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total users count");
                return 0;
            }
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            try
            {
                return await _securityUnitOfWork.Repository<SecurityUser>()
                    .GetQueryable()
                    .CountAsync(u => u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active users count");
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetUserStatisticsByRoleAsync()
        {
            try
            {
                var statistics = await _securityUnitOfWork.Repository<SecurityUserRole>()
                    .GetQueryable()
                    .Where(ur => ur.IsActive)
                    .Include(ur => ur.Role)
                    .Include(ur => ur.User)
                    .Where(ur => ur.User.IsActive)
                    .GroupBy(ur => ur.Role.Name)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count);

                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics by role");
                return new Dictionary<string, int>();
            }
        }

        #endregion
    }
}