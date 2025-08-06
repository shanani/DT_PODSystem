// Areas/Security/Data/Seeders/SecurityUserSeeder.cs
using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Data.Seeders
{
    public class SecurityUserSeeder
    {
        private readonly SecurityDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SecurityUserSeeder> _logger;

        public SecurityUserSeeder(
            SecurityDbContext context,
            IConfiguration configuration,
            ILogger<SecurityUserSeeder> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Only seed if no users exist
                if (await _context.SecurityUsers.AnyAsync())
                {
                    _logger.LogInformation("Security users already exist - skipping user seeding");
                    return;
                }

                _logger.LogInformation("Creating initial admin user...");

                // Create single admin user with highest privileges
                var adminUser = new SecurityUser
                {
                    Code = "admin",
                    Email = "admin@stc.com.sa",
                    FirstName = "System",
                    LastName = "Administrator",
                    Department = "IT",
                    JobTitle = "System Administrator",
                    IsActive = true,
                    IsAdmin = true,        // Admin privileges
                    IsSuperAdmin = true,   // Highest privileges
                    CreatedBy = "System",
                    CreatedAt = DateTime.UtcNow.AddHours(3)
                };

                await _context.SecurityUsers.AddAsync(adminUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Created admin user: admin@stc.com.sa with highest privileges");
                _logger.LogInformation("🎉 Security user seeding completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding security users");
                throw;
            }
        }
    }
}