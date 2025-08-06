
// Areas/Security/Data/Configurations/ApplicationUserRoleConfiguration.cs
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class SecurityUserRoleConfiguration : IEntityTypeConfiguration<SecurityUserRole>
    {
        public void Configure(EntityTypeBuilder<SecurityUserRole> builder)
        {
            builder.ToTable("ApplicationUserRoles", "Security");

            // Primary Key
            builder.HasKey(ur => ur.Id);

            // Properties
            builder.Property(ur => ur.UserId)
                .IsRequired();

            builder.Property(ur => ur.RoleId)
                .IsRequired();

            builder.Property(ur => ur.IsActive)
                .HasDefaultValue(true);

            builder.Property(ur => ur.AssignedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(ur => ur.AssignedBy)
                .HasMaxLength(100);

            builder.Property(ur => ur.RevokedBy)
                .HasMaxLength(100);

            // Indexes
            builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
                .IsUnique()
                .HasDatabaseName("IX_ApplicationUserRoles_User_Role");

            builder.HasIndex(ur => ur.UserId)
                .HasDatabaseName("IX_ApplicationUserRoles_UserId");

            builder.HasIndex(ur => ur.RoleId)
                .HasDatabaseName("IX_ApplicationUserRoles_RoleId");

            builder.HasIndex(ur => ur.IsActive)
                .HasDatabaseName("IX_ApplicationUserRoles_IsActive");

            // Relationships
            builder.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore calculated properties
            builder.Ignore(ur => ur.IsEffective);
            builder.Ignore(ur => ur.StatusBadge);
            builder.Ignore(ur => ur.StatusText);
        }
    }
}
