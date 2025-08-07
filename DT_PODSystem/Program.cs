// Project references

using System;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Configuration;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Data.Seeders;
using DT_PODSystem.Areas.Security.Integration;
using DT_PODSystem.Data;
using DT_PODSystem.Helpers;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Services.Implementation;
using DT_PODSystem.Services.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DT_PODSystem
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 🎯 Configure PERMANENT configuration sources (moved to PermanentConfig)
            builder.Configuration.ConfigurePermanentConfiguration(builder.Environment);


            // 🎯 Configure PROJECT-SPECIFIC configuration sources
            builder.Configuration
            .AddJsonFile("application-menu.json", optional: true, reloadOnChange: true);

            // 🎯 Configure permanent services (framework stuff)
            builder.Services.ConfigurePermanentServices(builder.Configuration);

            // 🎯 Configure project-specific services
            ConfigureProjectServices(builder.Services, builder.Configuration);

            var app = builder.Build();


            //app.Use(async (context, next) =>
            //{
            //    context.Response.Headers.Add("Content-Security-Policy",
            //        "default-src 'self'; " +
            //        "style-src 'self' 'unsafe-inline' https://*.stc.com.sa http://localhost:* https://localhost:*; " +
            //        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://*.stc.com.sa http://localhost:* https://localhost:*; " +
            //        "font-src 'self' https://*.stc.com.sa http://localhost:* https://localhost:*; " +
            //        "img-src 'self' data: https://*.stc.com.sa http://localhost:* https://localhost:*; " +
            //        "connect-src 'self' https://*.stc.com.sa http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*; " +
            //        "frame-src 'none'; " +
            //        "object-src 'none'; " +
            //        "base-uri 'self';"
            //    );
            //    await next();
            //});


            Util.Initialize(app.Services.GetRequiredService<IHttpContextAccessor>(), app.Services);

            // Check for seed command
            if (args.Length > 0 && args[0] == "seed")
            {
                await SeedDatabase(app.Services);
                return;
            }

            // 🎯 Configure permanent pipeline (framework stuff)
            app.ConfigurePermanentPipeline();

            // 🎯 ADD THESE LINES FOR SECURITY SEEDING
            if (app.Environment.IsDevelopment())
            {
                try
                {
                    await app.EnsureSecurityDatabaseAsync();
                    await app.SeedSecurityDataAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding security data: {ex.Message}");
                }
            }

            // 🎯 Configure project-specific pipeline
            ConfigureProjectPipeline(app, builder.Environment);

            app.Run();
        }

        // Find this method in your Program.cs and update it:

        private static async Task SeedDatabase(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            Console.WriteLine("🌱 Starting database seeding...");

            try
            {

                // 🔧 ADD THESE 4 LINES FOR SECURITY DATABASE
                var securityContext = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
                await securityContext.Database.EnsureCreatedAsync();
                Console.WriteLine("✅ Security database ensured");

                // 🔧 ADD THESE 3 LINES FOR SECURITY SEEDING
                var securityMasterSeeder = scope.ServiceProvider.GetRequiredService<SecurityMasterSeeder>();
                await securityMasterSeeder.SeedAsync();
                Console.WriteLine("✅ Security data seeded");

                Console.WriteLine("🎉 Database seeding completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Seeding failed: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private static void ConfigureProjectServices(IServiceCollection services, IConfiguration configuration)
        {

            // Database (NEW)
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // ✅ Custom view locations for automatic resolution
            services.Configure<RazorViewEngineOptions>(options =>
            {
                // Admin controllers in Controllers/Admin/ folder
                options.ViewLocationFormats.Add("/Views/Admin/{1}/{0}.cshtml");
                options.ViewLocationFormats.Add("/Views/Admin/Shared/{0}.cshtml");
            });

            services.AddSingleton(configuration);

            // N PROJECT-SPECIFIC SHARED SERVICES
            services.AddMemoryCache();
            services.AddScoped<IFileUploadService, FileUploadService>();
            services.AddScoped<ITemplateService, TemplateService>();
            services.AddScoped<IQueryService, QueryService>();
            services.AddScoped<IPODService, PODService>();
            //services.AddScoped<IReportService, ReportService>();

            services.AddScoped<IPdfProcessingService, PdfProcessingService>();
         
            services.AddScoped<ILookupsService, LookupsService>();

            //services.AddScoped<IDashboardStatisticsService, DashboardStatisticsService>();

        }

        private static void ConfigureProjectPipeline(WebApplication app, IWebHostEnvironment env)
        {

            Util.Configure(
                app.Services.GetService<IHttpContextAccessor>(),
                app.Services.GetService<IConfiguration>()
            );


            // ============================================================================
            // 🎯 MAIN APPLICATION ROUTES ONLY
            // ============================================================================
            ;

            // ✅ MAIN APPLICATION DEFAULT ROUTE (MUST BE LAST)
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Dashboard}/{action=Index}/{id?}",
                defaults: new { area = "" });

            app.MapRazorPages();
        }
    }
}