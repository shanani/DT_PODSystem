using System;
using System.Linq;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Repositories.Implementations;
using DT_PODSystem.Areas.Security.Repositories.Interfaces;
using DT_PODSystem.Areas.Security.Services.Implementations;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Configuration
{
    public static class SecurityServiceExtensions
    {
        /// <summary>
        /// Add Security Area services with separate SecurityDbContext
        /// </summary>
        public static IServiceCollection AddSecurityArea(this IServiceCollection services, IConfiguration configuration)
        {
            // Add Security DbContext (separate from main ApplicationDbContext)
            services.AddDbContext<SecurityDbContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("SecurityConnection") ??
                                      configuration.GetConnectionString("DefaultConnection");

                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__SecurityMigrationsHistory", "Security");
                });
            });

            // Add Security UnitOfWork (separate from main UnitOfWork)
            services.AddScoped<ISecurityUnitOfWork, SecurityUnitOfWork>();

            // Add Security Services
            services.AddScoped<ISecurityService, SecurityService>();
            services.AddScoped<ISecurityUserService, SecurityUserService>();
            services.AddScoped<ISecurityRoleService, SecurityRoleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<IRolePermissionService, RolePermissionService>();
            services.AddScoped<ISecurityAuditService, SecurityAuditService>();

            return services;
        }

        /// <summary>
        /// Configure Security Area routing
        /// </summary>
        public static WebApplication UseSecurityArea(this WebApplication app)
        {
            app.MapControllerRoute(
                name: "SecurityArea",
                pattern: "Security/{controller=Dashboard}/{action=Index}/{id?}",
                defaults: new { area = "Security" });

            return app;
        }

        /// <summary>
        /// Ensure Security database exists and is migrated
        /// </summary>
        public static async Task<WebApplication> EnsureSecurityDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SecurityDbContext>>();

            try
            {
                // Ensure database exists
                await context.Database.EnsureCreatedAsync();

                // Apply any pending migrations
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogInformation("Applying Security database migrations...");
                    await context.Database.MigrateAsync();
                }

                logger.LogInformation("Security database is ready");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error ensuring Security database");
                throw;
            }

            return app;
        }

        /// <summary>
        /// Seed default security data (roles, permissions)
        /// </summary>
        public static async Task<WebApplication> SeedSecurityDataAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

            try
            {
                // Seed default roles
                var roleService = scope.ServiceProvider.GetRequiredService<ISecurityRoleService>();


                // Seed default permissions
                var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
                //await permissionService.SeedDefaultPermissionsAsync();

                logger.LogInformation("Security data seeded successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding security data");
                // Don't throw - seeding failures shouldn't stop the application
            }

            return app;
        }
    }
}