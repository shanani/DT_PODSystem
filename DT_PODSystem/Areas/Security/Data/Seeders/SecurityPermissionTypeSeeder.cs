// Areas/Security/Data/Seeders/SecurityPermissionTypeSeeder.cs
using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Data.Seeders
{
    public class SecurityPermissionTypeSeeder
    {
        private readonly SecurityDbContext _context;
        private readonly ILogger<SecurityPermissionTypeSeeder> _logger;

        public SecurityPermissionTypeSeeder(SecurityDbContext context, ILogger<SecurityPermissionTypeSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                var permissionTypes = new[]
                {
                    new PermissionType { Name = "Security", Description = "Security management permissions", Icon = "fas fa-shield-alt", Color = "danger", SortOrder = 1, IsSystemType = true, CreatedBy = "System" },
                    new PermissionType { Name = "Admin", Description = "Administrative permissions", Icon = "fas fa-cog", Color = "dark", SortOrder = 2, IsSystemType = true, CreatedBy = "System" },
                    new PermissionType { Name = "Portal", Description = "Portal management permissions", Icon = "fas fa-globe", Color = "primary", SortOrder = 3, IsSystemType = true, CreatedBy = "System" },
                    new PermissionType { Name = "User", Description = "User management permissions", Icon = "fas fa-users", Color = "info", SortOrder = 4, IsSystemType = true, CreatedBy = "System" },
                    new PermissionType { Name = "Role", Description = "Role management permissions", Icon = "fas fa-user-tag", Color = "warning", SortOrder = 5, IsSystemType = true, CreatedBy = "System" }
                };

                foreach (var permissionType in permissionTypes)
                {
                    if (!await _context.PermissionTypes.AnyAsync(pt => pt.Name == permissionType.Name))
                    {
                        await _context.PermissionTypes.AddAsync(permissionType);
                        _logger.LogInformation("Seeded permission type: {PermissionTypeName}", permissionType.Name);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Security permission types seeded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding security permission types");
                throw;
            }
        }
    }
}