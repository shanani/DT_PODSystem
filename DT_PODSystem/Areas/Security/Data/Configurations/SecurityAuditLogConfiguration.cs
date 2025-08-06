
// Areas/Security/Data/Configurations/SecurityAuditLogConfiguration.cs
using DT_PODSystem.Areas.Security.Models.Entities;
using DT_PODSystem.Areas.Security.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DT_PODSystem.Areas.Security.Data.Configurations
{
    public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
    {
        public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
        {
            builder.ToTable("SecurityAuditLogs", "Security");

            // Primary Key
            builder.HasKey(sal => sal.Id);

            // Properties
            builder.Property(sal => sal.ActionType)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(sal => sal.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sal => sal.EntityName)
                .HasMaxLength(200);

            builder.Property(sal => sal.Action)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(sal => sal.Details)
                .HasMaxLength(2000);

            builder.Property(sal => sal.PerformedBy)
                .HasMaxLength(100);

            builder.Property(sal => sal.IpAddress)
                .HasMaxLength(50);

            builder.Property(sal => sal.UserAgent)
                .HasMaxLength(500);

            builder.Property(sal => sal.Timestamp)
                .HasDefaultValueSql("GETUTCDATE()");

            // Indexes
            builder.HasIndex(sal => sal.ActionType)
                .HasDatabaseName("IX_SecurityAuditLogs_ActionType");

            builder.HasIndex(sal => sal.EntityType)
                .HasDatabaseName("IX_SecurityAuditLogs_EntityType");

            builder.HasIndex(sal => sal.EntityId)
                .HasDatabaseName("IX_SecurityAuditLogs_EntityId");

            builder.HasIndex(sal => sal.PerformedBy)
                .HasDatabaseName("IX_SecurityAuditLogs_PerformedBy");

            builder.HasIndex(sal => sal.Timestamp)
                .HasDatabaseName("IX_SecurityAuditLogs_Timestamp");

            // Composite indexes for common queries
            builder.HasIndex(sal => new { sal.EntityType, sal.EntityId, sal.Timestamp })
                .HasDatabaseName("IX_SecurityAuditLogs_Entity_Timestamp");

            builder.HasIndex(sal => new { sal.PerformedBy, sal.Timestamp })
                .HasDatabaseName("IX_SecurityAuditLogs_PerformedBy_Timestamp");

            // Ignore calculated properties
            builder.Ignore(sal => sal.ActionIcon);
            builder.Ignore(sal => sal.ActionColor);
        }
    }
}

