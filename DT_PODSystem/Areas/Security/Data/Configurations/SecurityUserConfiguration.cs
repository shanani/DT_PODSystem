// Areas/Security/Data/Configurations/SecurityUserConfiguration.cs - Compatible with existing structure
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class SecurityUserConfiguration : IEntityTypeConfiguration<SecurityUser>
    {
        public void Configure(EntityTypeBuilder<SecurityUser> builder)
        {
            builder.ToTable("ApplicationUsers", "Security");  // 🔥 KEEP: Existing table structure

            // Primary Key
            builder.HasKey(u => u.Id);

            // Properties - keeping existing structure
            builder.Property(u => u.Code)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);  // Updated from 128 to match entity

            builder.Property(u => u.FirstName)
                .HasMaxLength(100);

            builder.Property(u => u.LastName)
                .HasMaxLength(100);

            builder.Property(u => u.Department)
                .HasMaxLength(200);

            builder.Property(u => u.JobTitle)
                .HasMaxLength(200);  // Updated from 100 to match entity

            builder.Property(u => u.Mobile)
                .HasMaxLength(20);

            builder.Property(u => u.Phone)
                .HasMaxLength(20);

            builder.Property(u => u.CreatedBy)
                .HasMaxLength(100);

            builder.Property(u => u.UpdatedBy)
                .HasMaxLength(100);

            builder.Property(u => u.PreferredLanguage)
                .HasMaxLength(10)
                .HasDefaultValue("en");

            builder.Property(u => u.TimeZone)
                .HasMaxLength(50)
                .HasDefaultValue("UTC");

            // 🔥 CLEAN: Simple boolean flags
            builder.Property(u => u.IsActive)
                .HasDefaultValue(true);

            builder.Property(u => u.IsAdmin)
                .HasDefaultValue(false);

            builder.Property(u => u.IsSuperAdmin)
                .HasDefaultValue(false);

            // Other properties
            builder.Property(u => u.AccessFailedCount)
                .HasDefaultValue(0);

            builder.Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(u => u.ReceiveEmailNotifications)
                .HasDefaultValue(true);

            builder.Property(u => u.ReceiveSystemNotifications)
                .HasDefaultValue(true);

            builder.Property(u => u.ReceiveAnnouncementNotifications)
                .HasDefaultValue(true);

            builder.Property(u => u.ReceiveMDTNotifications)
                .HasDefaultValue(true);

            // 🔥 KEEP: Existing indexes structure
            builder.HasIndex(u => u.Code)
                .IsUnique()
                .HasDatabaseName("IX_ApplicationUsers_Code");

            builder.HasIndex(u => u.Email)
                .IsUnique()
                .HasDatabaseName("IX_ApplicationUsers_Email");

            builder.HasIndex(u => u.IsActive)
                .HasDatabaseName("IX_ApplicationUsers_IsActive");

            // 🔥 NEW: Admin flags indexes
            builder.HasIndex(u => u.IsAdmin)
                .HasDatabaseName("IX_ApplicationUsers_IsAdmin");

            builder.HasIndex(u => u.IsSuperAdmin)
                .HasDatabaseName("IX_ApplicationUsers_IsSuperAdmin");

            // Relationships
            builder.HasMany(u => u.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 🔥 FIX: SecurityAuditLog relationship (using correct foreign key)
            builder.HasMany(u => u.CreatedAuditLogs)
                .WithOne()
                .HasForeignKey("SecurityUserId")
                .OnDelete(DeleteBehavior.SetNull);

            // 🔥 CLEAN: Ignore computed properties
            builder.Ignore(u => u.FullName);
            builder.Ignore(u => u.DisplayName);
            builder.Ignore(u => u.HasValidAccess);
            builder.Ignore(u => u.HasAdminAccess);
            builder.Ignore(u => u.HasSuperAdminAccess);
        }
    }
}