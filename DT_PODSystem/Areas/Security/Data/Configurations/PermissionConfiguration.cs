// Areas/Security/Data/Configurations/PermissionConfiguration.cs
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions", "Security");

            // Primary Key
            builder.HasKey(p => p.Id);

            // Properties
            builder.Property(p => p.PermissionTypeId)
                .IsRequired();

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.DisplayName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Description)
                .HasMaxLength(500);

            builder.Property(p => p.Scope)
                .HasConversion<int>()
                .HasDefaultValue(PermissionScope.Global);

            builder.Property(p => p.Action)
                .HasConversion<int>()
                .HasDefaultValue(PermissionAction.Read);

            builder.Property(p => p.SortOrder)
                .HasDefaultValue(0);

            builder.Property(p => p.IsActive)
                .HasDefaultValue(true);

            builder.Property(p => p.IsSystemPermission)
                .HasDefaultValue(false);

            builder.Property(p => p.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.CreatedBy)
                .HasMaxLength(100);

            builder.Property(p => p.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(p => p.UpdatedBy)
                .HasMaxLength(100);

            // Indexes
            builder.HasIndex(p => new { p.PermissionTypeId, p.Name })
                .IsUnique()
                .HasDatabaseName("IX_Permissions_PermissionTypeId_Name");

            builder.HasIndex(p => p.Name)
                .HasDatabaseName("IX_Permissions_Name");

            builder.HasIndex(p => p.DisplayName)
                .HasDatabaseName("IX_Permissions_DisplayName");

            builder.HasIndex(p => p.IsActive)
                .HasDatabaseName("IX_Permissions_IsActive");

            builder.HasIndex(p => p.Scope)
                .HasDatabaseName("IX_Permissions_Scope");

            builder.HasIndex(p => p.Action)
                .HasDatabaseName("IX_Permissions_Action");

            // Relationships
            builder.HasOne(p => p.PermissionType)
                .WithMany(pt => pt.Permissions)
                .HasForeignKey(p => p.PermissionTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(p => p.RolePermissions)
                .WithOne(rp => rp.Permission)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore calculated properties
            builder.Ignore(p => p.DisplayName);


        }
    }
}

