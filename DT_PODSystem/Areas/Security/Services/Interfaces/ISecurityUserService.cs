using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.DTOs;
using DT_PODSystem.Areas.Security.Models.Entities;

namespace DT_PODSystem.Areas.Security.Services.Interfaces
{
    public interface ISecurityUserService
    {

        Task<(bool Success, string Message, bool NewStatus)> ToggleUserStatusAsync(int userId, string updatedBy);
        Task<bool> CanUserModifyAsync(int targetUserId, string currentUserCode);
        Task<bool> CanUserDeleteAsync(int targetUserId, string currentUserCode);
        Task<bool> CanUserDeactivateAsync(int targetUserId, string currentUserCode);
        Task<(bool CanModify, string Reason)> ValidateUserModificationAsync(SecurityUser targetUser, SecurityUser updatedUser, string currentUserCode);
        Task<IEnumerable<SecurityUser>> GetAllUsersAsync();

        Task<SecurityUser> SyncUserWithADAsync(SecurityUser localUser, ADUserDetails adUser);
        Task<SecurityUser> GetOrCreateUserFromADAsync(string userCode, ADUserDetails adUser);

        // Basic CRUD
        Task<SecurityUser> GetUserByCodeAsync(string userCode);
        Task<SecurityUser> GetUserByEmailAsync(string email);
        Task<SecurityUser> GetUserByIdAsync(int userId);
        Task<SecurityUser> CreateUserAsync(SecurityUser user, string createdBy = null);
        Task<SecurityUser> UpdateUserAsync(SecurityUser user, string updatedBy = null);
        Task<bool> DeleteUserAsync(int userId, string deletedBy = null);

        // Integration with main project
        Task<SecurityUser> SyncUserFromMainProjectAsync(string userCode, object mainProjectUser);
        Task<SecurityUser> GetOrCreateUserAsync(string userCode, string firstName, string lastName, string email, string department = null);

        // Role management
        Task<bool> AddUserToRoleAsync(int userId, string roleName, string assignedBy = null);
        Task<bool> RemoveUserFromRoleAsync(int userId, string roleName, string revokedBy = null);
        Task<List<string>> GetUserRolesAsync(int userId);
        Task<bool> IsUserInRoleAsync(int userId, string roleName);

        // User management
        Task<IEnumerable<SecurityUser>> GetActiveUsersAsync();
        Task<IEnumerable<SecurityUser>> GetUsersByRoleAsync(string roleName);
        Task<bool> LockUserAsync(int userId, DateTime? lockoutEnd = null, string lockedBy = null);
        Task<bool> UnlockUserAsync(int userId, string unlockedBy = null);

        // Super Admin functions
        Task<bool> SetSuperAdminAsync(int userId, bool isSuperAdmin, string updatedBy = null);
        Task<List<SecurityUser>> GetSuperAdminsAsync();

        // Statistics
        Task<int> GetTotalUsersAsync();
        Task<int> GetActiveUsersCountAsync();
        Task<Dictionary<string, int>> GetUserStatisticsByRoleAsync();
    }
}