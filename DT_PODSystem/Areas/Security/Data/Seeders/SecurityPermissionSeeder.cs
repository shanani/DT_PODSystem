
// Areas/Security/Data/Seeders/SecurityPermissionSeeder.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Data.Seeders
{
    public class SecurityPermissionSeeder
    {
        private readonly SecurityDbContext _context;
        private readonly ILogger<SecurityPermissionSeeder> _logger;

        public SecurityPermissionSeeder(SecurityDbContext context, ILogger<SecurityPermissionSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                var permissionTypes = await _context.PermissionTypes.ToListAsync();

                foreach (var permissionType in permissionTypes)
                {
                    var permissions = GetPermissionsForType(permissionType.Name, permissionType.Id);

                    foreach (var permission in permissions)
                    {
                        if (!await _context.Permissions.AnyAsync(p => p.PermissionTypeId == permission.PermissionTypeId && p.Name == permission.Name))
                        {
                            await _context.Permissions.AddAsync(permission);
                            _logger.LogInformation("Seeded permission: {PermissionName}", permission.Name);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Security permissions seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding security permissions");
                throw;
            }
        }

        private List<Permission> GetPermissionsForType(string typeName, int typeId)
        {
            var permissions = new List<Permission>();
            var createdBy = "System";

            switch (typeName)
            {
                case "Security":
                    permissions.AddRange(new[]
                    {
                        new Permission { PermissionTypeId = typeId, Name = "Access", DisplayName = "Access Security Area", Description = "Access security management", Action = PermissionAction.Read, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "ManageRoles", DisplayName = "Manage Roles", Description = "Manage security roles", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "ManageUsers", DisplayName = "Manage Users", Description = "Manage security users", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "ManagePermissions", DisplayName = "Manage Permissions", Description = "Manage permission assignments", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "ViewAudit", DisplayName = "View Audit Logs", Description = "View security audit logs", Action = PermissionAction.Read, IsSystemPermission = true, CreatedBy = createdBy }
                    });
                    break;

                case "Admin":
                    permissions.AddRange(new[]
                    {
                        new Permission { PermissionTypeId = typeId, Name = "Dashboard", DisplayName = "Admin Dashboard", Description = "Access admin dashboard", Action = PermissionAction.Read, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "System", DisplayName = "System Administration", Description = "System administration access", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Settings", DisplayName = "System Settings", Description = "Manage system settings", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy }
                    });
                    break;

                case "Portal":
                    permissions.AddRange(new[]
                    {
                        new Permission { PermissionTypeId = typeId, Name = "Create", DisplayName = "Create Portal", Description = "Create new portals", Action = PermissionAction.Create, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Edit", DisplayName = "Edit Portal", Description = "Edit portal information", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Delete", DisplayName = "Delete Portal", Description = "Delete portals", Action = PermissionAction.Delete, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "View", DisplayName = "View Portal", Description = "View portal details", Action = PermissionAction.Read, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Manage", DisplayName = "Manage Portals", Description = "Full portal management", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy }
                    });
                    break;

                case "User":
                    permissions.AddRange(new[]
                    {
                        new Permission { PermissionTypeId = typeId, Name = "Create", DisplayName = "Create User", Description = "Create new users", Action = PermissionAction.Create, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Edit", DisplayName = "Edit User", Description = "Edit user information", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Delete", DisplayName = "Delete User", Description = "Delete users", Action = PermissionAction.Delete, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "View", DisplayName = "View User", Description = "View user details", Action = PermissionAction.Read, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Lock", DisplayName = "Lock User", Description = "Lock/unlock users", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy }
                    });
                    break;

                case "Role":
                    permissions.AddRange(new[]
                    {
                        new Permission { PermissionTypeId = typeId, Name = "Create", DisplayName = "Create Role", Description = "Create new roles", Action = PermissionAction.Create, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Edit", DisplayName = "Edit Role", Description = "Edit role information", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "Delete", DisplayName = "Delete Role", Description = "Delete roles", Action = PermissionAction.Delete, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "View", DisplayName = "View Role", Description = "View role details", Action = PermissionAction.Read, IsSystemPermission = true, CreatedBy = createdBy },
                        new Permission { PermissionTypeId = typeId, Name = "AssignPermissions", DisplayName = "Assign Permissions", Description = "Assign permissions to roles", Action = PermissionAction.Update, IsSystemPermission = true, CreatedBy = createdBy }
                    });
                    break;
            }

            // Set sort order
            for (int i = 0; i < permissions.Count; i++)
            {
                permissions[i].SortOrder = i + 1;
            }

            return permissions;
        }
    }
}