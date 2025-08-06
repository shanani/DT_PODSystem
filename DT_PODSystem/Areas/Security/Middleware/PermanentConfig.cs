using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Claims;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Data;
using DT_PODSystem.Areas.Security.Data.Seeders;
using DT_PODSystem.Areas.Security.Helpers;
using DT_PODSystem.Areas.Security.Repositories.Implementations;
using DT_PODSystem.Areas.Security.Repositories.Interfaces;
using DT_PODSystem.Areas.Security.Services.Hubs;
using DT_PODSystem.Areas.Security.Services.Implementations;
using DT_PODSystem.Areas.Security.Services.Interfaces;
using DT_PODSystem.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DT_PODSystem.Areas.Security.Integration
{
    public static class PermanentConfig
    {
        /// <summary>
        /// Configure permanent configuration sources - framework & common settings
        /// </summary>
        public static void ConfigurePermanentConfiguration(this ConfigurationManager configuration, IWebHostEnvironment environment)
        {
            // 🎯 CORE CONFIGURATION FILES (PERMANENT)
            configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            // 🎯 SECURITY AREA CONFIGURATION (PERMANENT)
            configuration.AddJsonFile("Areas/Security/security-menu.json", optional: true, reloadOnChange: true);

            // 🎯 SAMPLES AREA CONFIGURATION (PERMANENT - Framework samples)
            configuration.AddJsonFile("Areas/Samples/samples-menu.json", optional: true, reloadOnChange: true);
        }

        /// <summary>
        /// Configure permanent framework services that rarely change across projects
        /// </summary>
        public static void ConfigurePermanentServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 🎯 Core Framework Services (NO authorization convention - keeping your existing filters)
            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddSignalR();
            services.AddMemoryCache(); // ✅ Required for performance optimization

            // 🎯 Security Database Context (PERMANENT)
            ConfigureSecurityDatabase(services, configuration);

            // 🎯 Security Repositories & Services (PERMANENT)
            ConfigureSecurityServices(services);

            // 🎯 Authentication & Security
            ConfigureAuthentication(services, configuration);

            // 🎯 Essential Services
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            // 🎯 HttpClient with SSL bypass for intranet
            ConfigureHttpClient(services);

            // 🎯 Localization
            ConfigureLocalization(services);

            // 🎯 File Upload & CORS
            ConfigureFileUploadAndCors(services);

            // 🎯 View Rendering Service (PERMANENT - needed for emails)
            services.AddScoped<IViewRenderService, ViewRenderService>();
        }

        /// <summary>
        /// Configure Security Database Context - PERMANENT across all projects
        /// </summary>
        private static void ConfigureSecurityDatabase(IServiceCollection services, IConfiguration configuration)
        {
            // Security Module Database Context (Can use same or different connection)
            // For SAME DATABASE: Use "DefaultConnection"
            // For SEPARATE DATABASE: Use "SecurityConnection"
            services.AddDbContext<SecurityDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("SecurityConnection")
                    ?? configuration.GetConnectionString("DefaultConnection")));
        }

        /// <summary>
        /// Configure Security Services - COMPLETE SECURITY STACK
        /// </summary>
        private static void ConfigureSecurityServices(IServiceCollection services)
        {
            // 🎯 SECURITY MODULE REPOSITORIES (Use SecurityDbContext)
            services.AddScoped<ISecurityUnitOfWork, SecurityUnitOfWork>();
            services.AddScoped(typeof(ISecurityRepository<>), typeof(SecurityRepository<>));

            // 🎯 CORE SECURITY SERVICES (PERMANENT) - All services now in Security area
            services.AddScoped<ISecurityUserService, SecurityUserService>();
            services.AddScoped<ISecurityRoleService, SecurityRoleService>();
            services.AddScoped<IPermissionService, PermissionService>();
            services.AddScoped<ISecurityService, SecurityService>();
            services.AddScoped<ISecurityAuditService, SecurityAuditService>();
            services.AddScoped<IRolePermissionService, RolePermissionService>();

            // 🚀 NEW: PERFORMANCE & SECURITY OPTIMIZATION SERVICES
            services.AddScoped<ISecurityCacheService, SecurityCacheService>();
            services.AddScoped<ISessionManagerService, SessionManagerService>();

            // 🎯 EXTERNAL API SERVICES (Already configured in HttpClient section)
            services.AddScoped<IApiADService, ApiADService>();
            services.AddScoped<IApiEmailService, ApiEmailService>();

            // 🎯 SECURITY SEEDERS (PERMANENT)
            services.AddScoped<SecurityMasterSeeder>();
            services.AddScoped<SecurityRoleSeeder>();
            services.AddScoped<SecurityUserSeeder>();

            // 🚀 REMOVED: UserSessionMiddleware should NOT be registered as service
            // Middleware is automatically instantiated by the pipeline

            // 🎯 BACKGROUND SERVICES (Optional - for advanced scenarios)
            // services.AddHostedService<SecurityCacheCleanupService>(); // Uncomment if needed
            // services.AddHostedService<SessionMonitoringService>(); // Uncomment if needed
        }

        /// <summary>
        /// Configure permanent pipeline settings that rarely change
        /// </summary>
        public static void ConfigurePermanentPipeline(this WebApplication app)
        {
            // 🎯 Path Base Configuration (Production)
            var pathBase = app.Configuration["PathBase"];
            if (!string.IsNullOrEmpty(pathBase) && app.Environment.IsProduction())
            {
                app.UsePathBase(pathBase);
            }

            // 🎯 Exception Handling - UPDATED to use Security/Error
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Security/Error");  // ✅ UPDATED - Security Error Controller
                app.UseHsts();
            }

            // 🎯 SignalR Hubs
            app.MapHub<NotificationHub>("/notificationHub");

            // 🎯 Status Code Pages - UPDATED to use Security/Error
            app.UseStatusCodePagesWithReExecute("/Security/Error/{0}");  // ✅ UPDATED - Security Error Controller

            // 🎯 HTTPS & Static Files
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            // 🎯 AREA-SPECIFIC STATIC FILES
            ConfigureAreaStaticFiles(app);

            // 🎯 Routing
            app.UseRouting();

            // 🔥 ADD THIS MISSING LINE - Area route configuration
            app.MapControllerRoute(
                name: "areas",
                pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");


            // 🎯 CORS
            app.UseCors();

            // 🎯 Session (BEFORE Authentication)
            app.UseSession();

            // 🎯 Authentication & Authorization
            app.UseAuthentication();

            // 🚀 ENHANCED: User Session Middleware (PERFORMANCE OPTIMIZED)
            app.UseMiddleware<UserSessionMiddleware>();

            app.UseAuthorization();

            // 🎯 Path Injection Middleware (FIXED VERSION)
            AddPathInjectionMiddleware(app);
        }

        /// <summary>
        /// Configure static files for all areas
        /// </summary>
        private static void ConfigureAreaStaticFiles(WebApplication app)
        {
            // 🎯 ADMIN AREA STATIC FILES
            var adminWwwrootPath = Path.Combine(app.Environment.ContentRootPath, "Areas", "Admin", "wwwroot");
            if (Directory.Exists(adminWwwrootPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(adminWwwrootPath),
                    RequestPath = "/Admin"
                });
            }

            // 🎯 SECURITY AREA STATIC FILES
            var securityWwwrootPath = Path.Combine(app.Environment.ContentRootPath, "Areas", "Security", "wwwroot");
            if (Directory.Exists(securityWwwrootPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(securityWwwrootPath),
                    RequestPath = "/Security"
                });
            }

            // 🎯 SAMPLES AREA STATIC FILES
            var samplesWwwrootPath = Path.Combine(app.Environment.ContentRootPath, "Areas", "Samples", "wwwroot");
            if (Directory.Exists(samplesWwwrootPath))
            {
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(samplesWwwrootPath),
                    RequestPath = "/Samples"
                });
            }
        }

        private static void ConfigureAuthentication(IServiceCollection services, IConfiguration configuration)
        {
            var cookieSchemeName = configuration["Authentication:CookieScheme"] ?? "OpsHubCookiesScheme";

            Console.WriteLine($"🔧 Configuring authentication with scheme: {cookieSchemeName}");

            services.AddAuthentication(cookieSchemeName)
                .AddCookie(cookieSchemeName, options =>
                {
                    options.LoginPath = "/Security/Account/Login";
                    options.LogoutPath = "/Security/Account/Logout";
                    options.AccessDeniedPath = "/Security/Error/AccessDenied";
                    options.Cookie.Name = cookieSchemeName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;

                    // 🔥 SIMPLIFIED AUTHENTICATION EVENTS (NO CACHE SERVICE CALLS)
                    options.Events = new CookieAuthenticationEvents
                    {
                        OnSigningOut = async context =>
                        {
                            Console.WriteLine($"🔧 Cookie authentication signing out: {cookieSchemeName}");
                            await Task.CompletedTask;
                        },

                        OnValidatePrincipal = async context =>
                        {
                            try
                            {
                                var userService = context.HttpContext.RequestServices.GetService<ISecurityUserService>();
                                if (userService == null) return;

                                var userCodeClaim = context.Principal.FindFirst(ClaimTypes.Name);
                                if (userCodeClaim != null)
                                {
                                    var user = await userService.GetUserByCodeAsync(userCodeClaim.Value);
                                    if (user == null || !user.IsActive)
                                    {
                                        context.RejectPrincipal();
                                        return;
                                    }

                                    if (user.ExpirationDate.HasValue && user.ExpirationDate.Value <= DateTime.UtcNow.AddHours(3))
                                    {
                                        context.RejectPrincipal();
                                        return;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"🔥 Error in OnValidatePrincipal: {ex.Message}");
                                context.RejectPrincipal();
                            }
                        }
                    };
                });

            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromHours(8);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                options.Cookie.SameSite = SameSiteMode.Lax;
            });
        }

        /// <summary>
        /// Configure HttpClient with SSL bypass for intranet scenarios
        /// </summary>
        private static void ConfigureHttpClient(IServiceCollection services)
        {
            // ✅ GENERIC HttpClient with COMPLETE SSL bypass
            services.AddHttpClient("DefaultHttpClient")
                .ConfigurePrimaryHttpMessageHandler(() =>
                {
                    return new HttpClientHandler()
                    {
                        ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                    };
                });

            // ✅ FIXED: Complete SSL bypass for IADApiService HttpClient
            services.AddHttpClient<IApiADService, ApiADService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler()
                {
                    // 🔥 COMPLETE BYPASS - Always return true, ignore all SSL errors
                    ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };
            });

            // ✅ FIXED: Complete SSL bypass for IADApiService HttpClient
            services.AddHttpClient<IApiEmailService, ApiEmailService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler()
                {
                    // 🔥 COMPLETE BYPASS - Always return true, ignore all SSL errors
                    ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true
                };
            });
        }

        /// <summary>
        /// Configure localization settings
        /// </summary>
        private static void ConfigureLocalization(IServiceCollection services)
        {
            services.Configure<RequestLocalizationOptions>(options =>
            {
                var supportedCultures = new[] { new CultureInfo("en-GB") };

                options.DefaultRequestCulture = new RequestCulture("en-GB");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
        }

        /// <summary>
        /// Configure file upload and CORS settings
        /// </summary>
        private static void ConfigureFileUploadAndCors(IServiceCollection services)
        {
            // File Upload Configuration
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 52428800; // 50MB
            });

            // Add CORS if needed for API endpoints
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });
        }

        /// <summary>
        /// Add path injection middleware for production path base support
        /// </summary>
        private static void AddPathInjectionMiddleware(WebApplication app)
        {
            // Path injection middleware implementation
            app.Use(async (context, next) =>
            {
                var pathBase = app.Configuration["PathBase"];
                if (!string.IsNullOrEmpty(pathBase) && app.Environment.IsProduction())
                {
                    context.Request.PathBase = pathBase;
                }
                await next();
            });
        }
    }

    #region 🚀 NEW: Session Manager Service Interface & Implementation

    /// <summary>
    /// Session Manager Service Interface for clean dependency injection
    /// </summary>
    public interface ISessionManagerService
    {
        void ForceRefreshUserSession(string userCode);
        void InvalidateUserCache(string userCode);
        void CleanupStaleCache();
    }

    /// <summary>
    /// Session Manager Service Implementation
    /// Clean separation of concerns for session management
    /// </summary>
    public class SessionManagerService : ISessionManagerService
    {
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<SessionManagerService> _logger;

        // Same cache constants as middleware
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromSeconds(30);
        private static readonly string CACHE_KEY_PREFIX = "user_session:";
        private static readonly string REFRESH_FLAG_PREFIX = "refresh_required:";

        public SessionManagerService(IMemoryCache memoryCache, ILogger<SessionManagerService> logger)
        {
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public void ForceRefreshUserSession(string userCode)
        {
            var refreshFlagKey = $"{REFRESH_FLAG_PREFIX}{userCode}";
            var cacheKey = $"{CACHE_KEY_PREFIX}{userCode}";

            _memoryCache.Set(refreshFlagKey, true, TimeSpan.FromMinutes(1));
            _memoryCache.Remove(cacheKey);

            _logger.LogInformation("Force refresh flag set for user: {UserCode}", userCode);
        }

        public void InvalidateUserCache(string userCode)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{userCode}";
            _memoryCache.Remove(cacheKey);

            _logger.LogInformation("Cache invalidated for user: {UserCode}", userCode);
        }

        public void CleanupStaleCache()
        {
            // Memory cache automatically expires based on TTL
            _logger.LogDebug("Cache cleanup completed");
        }
    }

    #endregion
}