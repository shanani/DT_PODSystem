// Areas/Security/Data/Seeders/SecurityMasterSeeder.cs
using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Data.Seeders
{
    /// <summary>
    /// Simplified master seeder for Security Area - creates only essential data
    /// </summary>
    public class SecurityMasterSeeder
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SecurityMasterSeeder> _logger;

        public SecurityMasterSeeder(IServiceProvider serviceProvider, ILogger<SecurityMasterSeeder> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                _logger.LogInformation("🔐 Starting simplified Security Area seeding...");

                using var scope = _serviceProvider.CreateScope();

                // 1. Create one "Standard User" role (not assigned to anyone)
                var roleSeeder = scope.ServiceProvider.GetRequiredService<SecurityRoleSeeder>();
                await roleSeeder.SeedAsync();

                // 2. Create one admin user with highest privileges (no role assignment needed)
                var userSeeder = scope.ServiceProvider.GetRequiredService<SecurityUserSeeder>();
                await userSeeder.SeedAsync();

                _logger.LogInformation("🎉 Simplified Security Area seeding completed!");
                _logger.LogInformation("📋 Created: 1 Admin User (with flags), 1 Standard User Role");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error during Security Area seeding");
                throw;
            }
        }
    }
}