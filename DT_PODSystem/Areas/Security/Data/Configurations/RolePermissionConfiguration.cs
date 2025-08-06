// Areas/Security/Data/Configurations/RolePermissionConfiguration.cs (Updated)
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions", "Security");

            // Primary Key
            builder.HasKey(rp => rp.Id);

            // Properties
            builder.Property(rp => rp.RoleId)
                .IsRequired();

            builder.Property(rp => rp.PermissionId)
                .IsRequired();

            builder.Property(rp => rp.IsGranted)
                .HasDefaultValue(true);

            builder.Property(rp => rp.GrantedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(rp => rp.GrantedBy)
                .HasMaxLength(100);

            builder.Property(rp => rp.RevokedBy)
                .HasMaxLength(100);

            builder.Property(rp => rp.IsActive)
                .HasDefaultValue(true);

            builder.Property(rp => rp.Notes)
                .HasMaxLength(500);

            // Indexes
            builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                .IsUnique()
                .HasDatabaseName("IX_RolePermissions_Role_Permission");

            builder.HasIndex(rp => rp.RoleId)
                .HasDatabaseName("IX_RolePermissions_RoleId");

            builder.HasIndex(rp => rp.PermissionId)
                .HasDatabaseName("IX_RolePermissions_PermissionId");

            builder.HasIndex(rp => rp.IsActive)
                .HasDatabaseName("IX_RolePermissions_IsActive");

            builder.HasIndex(rp => rp.IsGranted)
                .HasDatabaseName("IX_RolePermissions_IsGranted");

            // Relationships
            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore calculated properties
            builder.Ignore(rp => rp.IsEffective);
            builder.Ignore(rp => rp.StatusBadge);
            builder.Ignore(rp => rp.StatusText);
        }
    }
}