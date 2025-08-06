// Areas/Security/Data/SecurityDbContextFactory.cs (For Migrations)
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DT_PODSystem.Areas.Security.Data
{
    /// <summary>
    /// Factory for creating SecurityDbContext during migrations
    /// </summary>
    public class SecurityDbContextFactory : IDesignTimeDbContextFactory<SecurityDbContext>
    {
        public SecurityDbContext CreateDbContext(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<SecurityDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection") ??
                                  configuration.GetConnectionString("SecurityConnection") ??
                                  "Server=.;Database=ED_LandingPage_Security;Trusted_Connection=true;MultipleActiveResultSets=true";

            optionsBuilder.UseSqlServer(connectionString);

            return new SecurityDbContext(optionsBuilder.Options);
        }
    }
}