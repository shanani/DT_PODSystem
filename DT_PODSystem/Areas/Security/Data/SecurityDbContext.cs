// Areas/Security/Data/SecurityDbContext.cs
// ONLY Security entities - NO main application entities

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DT_PODSystem.Areas.Security.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace DT_PODSystem.Areas.Security.Data
{
    public class SecurityDbContext : DbContext
    {
        public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options)
        {
        }

        // ONLY Security entities
        public DbSet<SecurityUser> SecurityUsers { get; set; }
        public DbSet<SecurityRole> SecurityRoles { get; set; }
        public DbSet<SecurityUserRole> SecurityUserRoles { get; set; }
        public DbSet<PermissionType> PermissionTypes { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ONLY Security entities
            ConfigureSecurityUser(builder);
            ConfigureSecurityRole(builder);
            ConfigureSecurityUserRole(builder);
            ConfigurePermissionType(builder);
            ConfigurePermission(builder);
            ConfigureRolePermission(builder);
        }

        // 🆕 UPDATED ConfigurePermission method with modern check constraints
        private void ConfigurePermission(ModelBuilder builder)
        {
            builder.Entity<Permission>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).HasMaxLength(100).IsRequired();
                entity.Property(p => p.DisplayName).HasMaxLength(200);
                entity.Property(p => p.Description).HasMaxLength(500);
                entity.Property(p => p.Icon).HasMaxLength(50);
                entity.Property(p => p.Color).HasMaxLength(20);

                // 🆕 NEW HIERARCHICAL PROPERTIES
                entity.Property(p => p.HierarchyPath).HasMaxLength(500);

                // 🆕 HIERARCHICAL RELATIONSHIPS
                entity.HasOne(p => p.ParentPermission)
                      .WithMany(p => p.Children)
                      .HasForeignKey(p => p.ParentPermissionId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete to avoid cycles

                // EXISTING RELATIONSHIP
                entity.HasOne(p => p.PermissionType)
                      .WithMany(pt => pt.Permissions)
                      .HasForeignKey(p => p.PermissionTypeId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 🆕 INDEXES FOR PERFORMANCE
                entity.HasIndex(p => p.ParentPermissionId)
                      .HasDatabaseName("IX_Permissions_ParentPermissionId");

                entity.HasIndex(p => p.HierarchyPath)
                      .HasDatabaseName("IX_Permissions_HierarchyPath");

                entity.HasIndex(p => new { p.PermissionTypeId, p.Level })
                      .HasDatabaseName("IX_Permissions_Type_Level");

                entity.HasIndex(p => new { p.PermissionTypeId, p.ParentPermissionId, p.SortOrder })
                      .HasDatabaseName("IX_Permissions_Type_Parent_Sort");

                // EXISTING INDEXES (keep your current ones if any)
                entity.HasIndex(p => new { p.Name, p.PermissionTypeId })
                      .IsUnique()
                      .HasDatabaseName("IX_Permissions_Name_Type");

                // 🆕 MODERN WAY TO ADD CHECK CONSTRAINTS
                entity.ToTable("Permissions", t =>
                {
                    t.HasCheckConstraint("CK_Permission_Level", "[Level] >= 0 AND [Level] <= 10");
                    t.HasCheckConstraint("CK_Permission_NoSelfReference", "[ParentPermissionId] != [Id]");
                });
            });
        }



        private void ConfigureSecurityUser(ModelBuilder builder)
        {
            builder.Entity<SecurityUser>(entity =>
            {
                entity.HasKey(u => u.Id);
                entity.Property(u => u.Code).HasMaxLength(100).IsRequired();
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.Email).HasMaxLength(255);
                entity.Property(u => u.Department).HasMaxLength(200);
                entity.Property(u => u.JobTitle).HasMaxLength(200);
                entity.Property(u => u.CreatedBy).HasMaxLength(100);
                entity.Property(u => u.UpdatedBy).HasMaxLength(100);

                // Default values
                entity.Property(u => u.IsActive).HasDefaultValue(true);
                entity.Property(u => u.IsAdmin).HasDefaultValue(false);
                entity.Property(u => u.IsSuperAdmin).HasDefaultValue(false);
                entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(u => u.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // Indexes
                entity.HasIndex(u => u.Code).IsUnique().HasDatabaseName("IX_SecurityUser_Code");
                entity.HasIndex(u => u.Email).HasDatabaseName("IX_SecurityUser_Email");
                entity.HasIndex(u => u.IsActive).HasDatabaseName("IX_SecurityUser_IsActive");

                entity.ToTable("SecurityUsers");
            });
        }

        private void ConfigureSecurityRole(ModelBuilder builder)
        {
            builder.Entity<SecurityRole>(entity =>
            {
                entity.HasKey(r => r.Id);
                entity.Property(r => r.Name).HasMaxLength(100).IsRequired();
                entity.Property(r => r.Description).HasMaxLength(500);

                entity.HasIndex(r => r.Name).IsUnique().HasDatabaseName("IX_SecurityRole_Name");
                entity.ToTable("SecurityRoles");
            });
        }

        private void ConfigureSecurityUserRole(ModelBuilder builder)
        {
            builder.Entity<SecurityUserRole>(entity =>
            {
                entity.HasKey(ur => ur.Id);

                entity.HasOne(ur => ur.User)
                      .WithMany(u => u.UserRoles)
                      .HasForeignKey(ur => ur.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ur => ur.Role)
                      .WithMany(r => r.UserRoles)
                      .HasForeignKey(ur => ur.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(ur => new { ur.UserId, ur.RoleId })
                      .IsUnique()
                      .HasDatabaseName("IX_SecurityUserRole_User_Role");

                entity.ToTable("SecurityUserRoles");
            });
        }

        private void ConfigurePermissionType(ModelBuilder builder)
        {
            builder.Entity<PermissionType>(entity =>
            {
                entity.HasKey(pt => pt.Id);
                entity.Property(pt => pt.Name).HasMaxLength(100).IsRequired();
                entity.Property(pt => pt.Description).HasMaxLength(500);
                entity.Property(pt => pt.Icon).HasMaxLength(50);
                entity.Property(pt => pt.Color).HasMaxLength(20);

                entity.ToTable("PermissionTypes");
            });
        }


        private void ConfigureRolePermission(ModelBuilder builder)
        {
            builder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(rp => rp.Id);

                entity.HasOne(rp => rp.Role)
                      .WithMany(r => r.RolePermissions)
                      .HasForeignKey(rp => rp.RoleId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rp => rp.Permission)
                      .WithMany(p => p.RolePermissions)
                      .HasForeignKey(rp => rp.PermissionId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(rp => new { rp.RoleId, rp.PermissionId })
                      .IsUnique()
                      .HasDatabaseName("IX_RolePermission_Role_Permission");

                entity.ToTable("RolePermissions");
            });
        }

        // Audit tracking
        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is SecurityUser user)
                {
                    if (entry.State == EntityState.Added)
                        user.CreatedAt = DateTime.UtcNow.AddHours(3);
                    user.UpdatedAt = DateTime.UtcNow.AddHours(3);
                }
                // Add similar logic for other entities as needed
            }
        }
    }
}