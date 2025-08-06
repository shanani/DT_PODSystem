// Areas/Security/Data/Seeders/SecurityRoleSeeder.cs
using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Data.Seeders
{
    public class SecurityRoleSeeder
    {
        private readonly SecurityDbContext _context;
        private readonly ILogger<SecurityRoleSeeder> _logger;

        public SecurityRoleSeeder(SecurityDbContext context, ILogger<SecurityRoleSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Only seed if no roles exist
                if (await _context.SecurityRoles.AnyAsync())
                {
                    _logger.LogInformation("Security roles already exist - skipping role seeding");
                    return;
                }

                _logger.LogInformation("Creating initial security roles...");

                // Create only Standard User role
                var standardUserRole = new SecurityRole
                {
                    Name = "Standard User",
                    Description = "Standard user with basic permissions",
                    IsSystemRole = true,
                    IsActive = true,
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow.AddHours(3)
                };

                await _context.SecurityRoles.AddAsync(standardUserRole);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Created 'Standard User' role");
                _logger.LogInformation("🎉 Security roles seeding completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding security roles");
                throw;
            }
        }
    }
}