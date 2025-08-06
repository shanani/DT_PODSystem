// Areas/Security/Data/Configurations/PermissionTypeConfiguration.cs
using DT_PODSystem.Areas.Security.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class PermissionTypeConfiguration : IEntityTypeConfiguration<PermissionType>
    {
        public void Configure(EntityTypeBuilder<PermissionType> builder)
        {
            builder.ToTable("PermissionTypes", "Security");

            // Primary Key
            builder.HasKey(pt => pt.Id);

            // Properties
            builder.Property(pt => pt.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(pt => pt.Description)
                .HasMaxLength(200);

            builder.Property(pt => pt.Icon)
                .HasMaxLength(50)
                .HasDefaultValue("fas fa-shield-alt");

            builder.Property(pt => pt.Color)
                .HasMaxLength(20)
                .HasDefaultValue("primary");

            builder.Property(pt => pt.SortOrder)
                .HasDefaultValue(0);

            builder.Property(pt => pt.IsActive)
                .HasDefaultValue(true);

            builder.Property(pt => pt.IsSystemType)
                .HasDefaultValue(false);

            builder.Property(pt => pt.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(pt => pt.CreatedBy)
                .HasMaxLength(100);

            builder.Property(pt => pt.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(pt => pt.UpdatedBy)
                .HasMaxLength(100);

            // Indexes
            builder.HasIndex(pt => pt.Name)
                .IsUnique()
                .HasDatabaseName("IX_PermissionTypes_Name");

            builder.HasIndex(pt => pt.SortOrder)
                .HasDatabaseName("IX_PermissionTypes_SortOrder");

            builder.HasIndex(pt => pt.IsActive)
                .HasDatabaseName("IX_PermissionTypes_IsActive");

            // Relationships
            builder.HasMany(pt => pt.Permissions)
                .WithOne(p => p.PermissionType)
                .HasForeignKey(p => p.PermissionTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Ignore calculated properties

            builder.Ignore(pt => pt.Icon);
        }
    }
}

