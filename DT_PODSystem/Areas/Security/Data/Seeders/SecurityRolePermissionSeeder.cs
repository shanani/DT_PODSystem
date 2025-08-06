
// Areas/Security/Data/Seeders/SecurityRolePermissionSeeder.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Data.Seeders
{
    public class SecurityRolePermissionSeeder
    {
        private readonly SecurityDbContext _context;
        private readonly ILogger<SecurityRolePermissionSeeder> _logger;

        public SecurityRolePermissionSeeder(SecurityDbContext context, ILogger<SecurityRolePermissionSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                var roles = await _context.SecurityRoles.ToListAsync();
                var permissions = await _context.Permissions.Include(p => p.PermissionType).ToListAsync();

                // Admin Role - Gets most permissions except Super Admin functions
                var adminRole = roles.FirstOrDefault(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    var adminPermissions = permissions.Where(p =>
                        p.PermissionType.Name == "Admin" ||
                        p.PermissionType.Name == "Portal" ||
                        p.PermissionType.Name == "User" ||
                        p.PermissionType.Name == "Role" ||
                        (p.PermissionType.Name == "Security" && p.Name != "ManagePermissions")
                    ).ToList();

                    await AssignPermissionsToRole(adminRole.Id, adminPermissions, "System");
                }

                // User Role - Gets basic read permissions
                var userRole = roles.FirstOrDefault(r => r.Name == "User");
                if (userRole != null)
                {
                    var userPermissions = permissions.Where(p =>
                        p.Action == PermissionAction.Read &&
                        (p.PermissionType.Name == "Portal" || p.PermissionType.Name == "User")
                    ).ToList();

                    await AssignPermissionsToRole(userRole.Id, userPermissions, "System");
                }

                // SecurityManager Role - Gets security-specific permissions
                var securityManagerRole = roles.FirstOrDefault(r => r.Name == "SecurityManager");
                if (securityManagerRole != null)
                {
                    var securityPermissions = permissions.Where(p =>
                        p.PermissionType.Name == "Security"
                    ).ToList();

                    await AssignPermissionsToRole(securityManagerRole.Id, securityPermissions, "System");
                }

                // RoleManager Role - Gets role management permissions
                var roleManagerRole = roles.FirstOrDefault(r => r.Name == "RoleManager");
                if (roleManagerRole != null)
                {
                    var rolePermissions = permissions.Where(p =>
                        p.PermissionType.Name == "Role" ||
                        (p.PermissionType.Name == "Security" && p.Name == "Access")
                    ).ToList();

                    await AssignPermissionsToRole(roleManagerRole.Id, rolePermissions, "System");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Security role permissions seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding security role permissions");
                throw;
            }
        }

        private async Task AssignPermissionsToRole(int roleId, List<Permission> permissions, string grantedBy)
        {
            foreach (var permission in permissions)
            {
                if (!await _context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permission.Id))
                {
                    await _context.RolePermissions.AddAsync(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permission.Id,
                        IsGranted = true,
                        GrantedBy = grantedBy
                    });
                }
            }
        }
    }
}
