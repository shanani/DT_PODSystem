using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class SecurityConfiguration : IEntityTypeConfiguration<SecurityUser>
    {
        public void Configure(EntityTypeBuilder<SecurityUser> builder)
        {
            // Add the new IsSuperAdmin property to existing ApplicationUser configuration
            builder.Property(u => u.IsSuperAdmin)
                .HasDefaultValue(false);

            // Index for Super Admin queries
            builder.HasIndex(u => u.IsSuperAdmin)
                .HasDatabaseName("IX_ApplicationUsers_IsSuperAdmin");

            // Ignore calculated properties
            builder.Ignore(u => u.HasSuperAdminAccess);
        }
    }
}