using DT_PODSystemWorker.Services;
using DT_PODSystem.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

namespace DT_PODSystemWorker
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // ✅ ENHANCED: Environment-aware logging configuration
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            var isDevelopment = environment == "Development";

            // ✅ Create logs directory structure
            CreateLogDirectories(isDevelopment);

            // ✅ ENHANCED: Configure Serilog with your existing logging style
            var loggerConfig = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();

            if (isDevelopment)
            {
                // ✅ DEVELOPMENT: Comprehensive file logging matching your existing style
                loggerConfig
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)

                    // Console output - keeping your style
                    .WriteTo.Console()

                    // ✅ 1. COMPLETE DEBUG LOG - Everything (matches your detailed logging)
                    .WriteTo.File(
                        path: "Logs/Development/worker-complete-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} | {Message:lj}{NewLine}{Exception}",
                        fileSizeLimitBytes: 209715200, // 200MB
                        rollOnFileSizeLimit: true,
                        restrictedToMinimumLevel: LogEventLevel.Debug)

                    // ✅ 2. PDF EXTRACTION LOG - Matches your field extraction logging
                    .WriteTo.Logger(lg => lg
                        .Filter.ByIncludingOnly(evt =>
                            evt.MessageTemplate.Text.Contains("[EXTRACTION") ||
                            evt.MessageTemplate.Text.Contains("[TEMPLATE") ||
                            evt.MessageTemplate.Text.Contains("[FIELD MAPPING") ||
                            evt.MessageTemplate.Text.Contains("[PDF") ||
                            evt.MessageTemplate.Text.Contains("[TEXT BLOCKS") ||
                            evt.MessageTemplate.Text.Contains("[SUCCESS]") ||
                            evt.MessageTemplate.Text.Contains("[FALLBACK") ||
                            evt.MessageTemplate.Text.Contains("[FINAL RESULT]"))
                        .WriteTo.File(
                            path: "Logs/Development/pdf-extraction-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "[{Timestamp:HH:mm:ss.fff}] {Level:u3} | {Message:lj}{NewLine}{Exception}",
                            fileSizeLimitBytes: 104857600)) // 100MB

                    // ✅ 3. COORDINATE & CALIBRATION LOG - Your transformation logging
                    .WriteTo.Logger(lg => lg
                        .Filter.ByIncludingOnly(evt =>
                            evt.MessageTemplate.Text.Contains("[CALIBRATION") ||
                            evt.MessageTemplate.Text.Contains("[TRANSFORMATION") ||
                            evt.MessageTemplate.Text.Contains("📐") ||
                            evt.MessageTemplate.Text.Contains("🎯") ||
                            evt.MessageTemplate.Text.Contains("Coords=") ||
                            evt.MessageTemplate.Text.Contains("Offset:") ||
                            evt.MessageTemplate.Text.Contains("Scale:"))
                        .WriteTo.File(
                            path: "Logs/Development/coordinates-calibration-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "[{Timestamp:HH:mm:ss.fff}] COORD | {Message:lj}{NewLine}{Exception}",
                            fileSizeLimitBytes: 52428800)) // 50MB

                    // ✅ 4. FORMULA CALCULATION LOG - All your formula processing
                    .WriteTo.Logger(lg => lg
                        .Filter.ByIncludingOnly(evt =>
                            evt.MessageTemplate.Text.Contains("🧮") ||
                            evt.MessageTemplate.Text.Contains("[CONTEXT DEBUG]") ||
                            evt.MessageTemplate.Text.Contains("Formula") ||
                            evt.MessageTemplate.Text.Contains("Calculated") ||
                            evt.MessageTemplate.Text.Contains("Input field") ||
                            evt.MessageTemplate.Text.Contains("Processed:") ||
                            evt.MessageTemplate.Text.Contains("Evaluating:"))
                        .WriteTo.File(
                            path: "Logs/Development/formula-calculation-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "[{Timestamp:HH:mm:ss.fff}] CALC | {Message:lj}{NewLine}{Exception}",
                            fileSizeLimitBytes: 104857600)) // 100MB

                    // ✅ 5. FIELD MAPPING DETAILS - Your field coordinate logging
                    .WriteTo.Logger(lg => lg
                        .Filter.ByIncludingOnly(evt =>
                            evt.MessageTemplate.Text.Contains("📝 [FIELD MAPPING]") ||
                            evt.MessageTemplate.Text.Contains("📍 [ALL TEXT BLOCKS]") ||
                            evt.MessageTemplate.Text.Contains("Block ") ||
                            evt.MessageTemplate.Text.Contains("Field '") ||
                            evt.MessageTemplate.Text.Contains("Page ") ||
                            evt.MessageTemplate.Text.Contains(" at ("))
                        .WriteTo.File(
                            path: "Logs/Development/field-mapping-details-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 7,
                            outputTemplate: "[{Timestamp:HH:mm:ss.fff}] MAP | {Message:lj}{NewLine}{Exception}",
                            fileSizeLimitBytes: 52428800)) // 50MB

                    // ✅ 6. ERROR LOG - All errors and warnings
                    .WriteTo.File(
                        path: "Logs/Development/errors-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30,
                        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {Level:u3} | {SourceContext} | {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: LogEventLevel.Warning,
                        fileSizeLimitBytes: 104857600) // 100MB

                    // ✅ 7. PROCESSING RESULTS - Your final results logging
                    .WriteTo.Logger(lg => lg
                        .Filter.ByIncludingOnly(evt =>
                            evt.MessageTemplate.Text.Contains("📊 [FINAL RESULT]") ||
                            evt.MessageTemplate.Text.Contains("[EXTRACTION COMPLETE]") ||
                            evt.MessageTemplate.Text.Contains("Successfully extracted") ||
                            evt.MessageTemplate.Text.Contains("Processing:") ||
                            evt.MessageTemplate.Text.Contains("✅ Calculated"))
                        .WriteTo.File(
                            path: "Logs/Development/processing-results-.log",
                            rollingInterval: RollingInterval.Day,
                            retainedFileCountLimit: 15,
                            outputTemplate: "[{Timestamp:HH:mm:ss.fff}] RESULT | {Message:lj}{NewLine}{Exception}",
                            fileSizeLimitBytes: 26214400)); // 25MB
            }
            else
            {
                // ✅ PRODUCTION: Keep your existing minimal logging
                loggerConfig
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)

                    .WriteTo.Console()
                    .WriteTo.File(
                        path: "Logs/worker-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 30)
                    .WriteTo.File(
                        path: "Logs/errors-.log",
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 60,
                        restrictedToMinimumLevel: LogEventLevel.Error);
            }

            Log.Logger = loggerConfig.CreateLogger();

            try
            {
                Log.Information("🚀 Starting DT_PODSystemWorker in {Environment} mode", environment);

                if (isDevelopment)
                {
                    Log.Information("📋 Development logging enabled - Organized file structure:");
                    Log.Information("   📄 Complete trace: Logs/Development/worker-complete-{Date}.log");
                    Log.Information("   🔍 PDF Extraction: Logs/Development/pdf-extraction-{Date}.log");
                    Log.Information("   📐 Coordinates: Logs/Development/coordinates-calibration-{Date}.log");
                    Log.Information("   🧮 Formulas: Logs/Development/formula-calculation-{Date}.log");
                    Log.Information("   📝 Field Mapping: Logs/Development/field-mapping-details-{Date}.log");
                    Log.Information("   📊 Results: Logs/Development/processing-results-{Date}.log");
                    Log.Information("   ❌ Errors: Logs/Development/errors-{Date}.log");
                    Log.Information("");
                    Log.Information("🎯 All your existing detailed logging will be captured and organized!");
                }

                var host = CreateHostBuilder(args).Build();

                // Run database migrations on startup
                await EnsureDatabaseUpdated(host);

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "💥 Worker terminated unexpectedly");
            }
            finally
            {
                Log.Information("🛑 DT_PODSystemWorker shutting down");
                Log.CloseAndFlush();
            }
        }

        // ✅ Create log directory structure
        private static void CreateLogDirectories(bool isDevelopment)
        {
            Directory.CreateDirectory("Logs");

            if (isDevelopment)
            {
                Directory.CreateDirectory("Logs/Development");
                Console.WriteLine("📁 Created development log directories");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog() // Use Serilog instead of default logging
                .UseWindowsService() // Enables running as Windows Service
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    // Database - Using main ApplicationDbContext
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

                    // Configuration
                    services.Configure<WorkerSettings>(
                        configuration.GetSection("WorkerSettings"));
                    services.Configure<FileOrganizationSettings>(
                        configuration.GetSection("FileOrganization"));

                    // ✅ UPDATED: Services for field extraction only (no calculations)
                    services.AddScoped<IFileDiscoveryService, FileDiscoveryService>();
                    services.AddScoped<IPdfExtractionService, PdfExtractionService>();
                    services.AddScoped<IFileOrganizationService, FileOrganizationService>();

                    // ✅ ADDED: FormulaCalculationService for Query calculations
                    services.AddScoped<IFormulaCalculationService, FormulaCalculationService>();

                    // ✅ Background services
                    services.AddHostedService<DocumentProcessorService>();
                    services.AddHostedService<QueryProcessorService>(); // ✅ NEW: Query calculations
                });

        private static async Task EnsureDatabaseUpdated(IHost host)
        {
            using var scope = host.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                Log.Information("🔄 Checking database migrations...");
                await context.Database.MigrateAsync();
                Log.Information("✅ Database is up to date");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "💥 Database migration failed");
                throw;
            }
        }
    }
}