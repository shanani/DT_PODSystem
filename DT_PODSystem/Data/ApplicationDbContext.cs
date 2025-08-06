using System;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DT_PODSystem.Data
{
    /// <summary>
    /// Entity Framework DbContext for DT_PODSystem application
    /// UPDATED - Added QueryResult entity for calculated query outputs
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Core lookup entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<GeneralDirectorate> GeneralDirectorates { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Vendor> Vendors { get; set; }

        // File and template entities
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<PdfTemplate> PdfTemplates { get; set; }
        public DbSet<TemplateAttachment> TemplateAttachments { get; set; }

        // Field mapping entities
        public DbSet<FieldMapping> FieldMappings { get; set; }
        public DbSet<TemplateAnchor> TemplateAnchors { get; set; } // ✅ RENAMED: AnchorPoints → TemplateAnchors

        // ✅ Query entities (separated from Template)
        public DbSet<QueryConstant> QueryConstants { get; set; }
        public DbSet<Query> Queries { get; set; }
        public DbSet<QueryOutput> QueryOutputs { get; set; }
        public DbSet<FormulaCanvas> FormulaCanvases { get; set; }

        // Processing and audit entities
        public DbSet<ProcessedFile> ProcessedFiles { get; set; }
        public DbSet<ProcessedField> ProcessedFields { get; set; }
        public DbSet<QueryResult> QueryResults { get; set; } // ✅ NEW: Query calculation results
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureBaseEntities(modelBuilder);
            ConfigureLookupEntities(modelBuilder);
            ConfigureTemplateEntities(modelBuilder);
            ConfigureFieldMappingEntities(modelBuilder);
            ConfigureQueryEntities(modelBuilder);
            ConfigureProcessedEntities(modelBuilder);

            // Seed initial data
            SeedInitialData(modelBuilder);
        }

        private void ConfigureBaseEntities(ModelBuilder modelBuilder)
        {
            // Configure common properties for all BaseEntity-derived entities
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedDate")
                        .HasDefaultValueSql("GETUTCDATE()");

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("IsActive")
                        .HasDefaultValue(true);

                    // Add index on CreatedDate for performance
                    modelBuilder.Entity(entityType.ClrType)
                        .HasIndex("CreatedDate")
                        .HasDatabaseName($"IX_{entityType.ClrType.Name}_CreatedDate");

                    // Add index on IsActive for filtering
                    modelBuilder.Entity(entityType.ClrType)
                        .HasIndex("IsActive")
                        .HasDatabaseName($"IX_{entityType.ClrType.Name}_IsActive");
                }
            }
        }

        private void ConfigureLookupEntities(ModelBuilder modelBuilder)
        {
            // Category configuration
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasIndex(e => e.Name).IsUnique();
                entity.HasIndex(e => e.DisplayOrder);
                entity.Property(e => e.ColorCode).HasDefaultValue("#A54EE1");
                entity.Property(e => e.IconClass).HasDefaultValue("fa fa-folder");
            });

            // GeneralDirectorate configuration
            modelBuilder.Entity<GeneralDirectorate>(entity =>
            {

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.DisplayOrder);
            });

            // Department configuration
            modelBuilder.Entity<Department>(entity =>
            {

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => new { e.GeneralDirectorateId, e.DisplayOrder });

                entity.HasOne(d => d.GeneralDirectorate)
                    .WithMany(gd => gd.Departments)
                    .HasForeignKey(d => d.GeneralDirectorateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Vendor configuration
            modelBuilder.Entity<Vendor>(entity =>
            {

                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.IsApproved);
                entity.HasIndex(e => e.TaxNumber);
                entity.HasIndex(e => e.CommercialRegister);
                entity.Property(e => e.IsApproved).HasDefaultValue(false);
            });
        }

        private void ConfigureTemplateEntities(ModelBuilder modelBuilder)
        {
            // PdfTemplate configuration
            modelBuilder.Entity<PdfTemplate>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.DepartmentId);
                entity.HasIndex(e => e.VendorId);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.IsFinancialData);
                entity.Property(e => e.Status).HasDefaultValue(TemplateStatus.Draft);
                entity.Property(e => e.ProcessingPriority).HasDefaultValue(5);
                entity.Property(e => e.RequiresApproval).HasDefaultValue(false);
                entity.Property(e => e.IsFinancialData).HasDefaultValue(false);
                entity.Property(e => e.ProcessedCount).HasDefaultValue(0);
                entity.HasOne(t => t.Category).WithMany(c => c.Templates).HasForeignKey(t => t.CategoryId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Department).WithMany(d => d.Templates).HasForeignKey(t => t.DepartmentId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(t => t.Vendor).WithMany(v => v.Templates).HasForeignKey(t => t.VendorId).OnDelete(DeleteBehavior.SetNull);
            });

            // UploadedFile configuration
            modelBuilder.Entity<UploadedFile>(entity =>
            {
                entity.HasIndex(e => e.OriginalFileName);
                entity.HasIndex(e => e.SavedFileName);
                entity.HasIndex(e => e.IsTemporary);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.FileHash);
                entity.Property(e => e.IsTemporary).HasDefaultValue(true);
            });

            // TemplateAttachment configuration
            modelBuilder.Entity<TemplateAttachment>(entity =>
            {
                entity.HasIndex(e => new { e.TemplateId, e.UploadedFileId });
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsPrimary);
                entity.HasIndex(e => e.DisplayOrder);
                entity.Property(e => e.IsPrimary).HasDefaultValue(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.HasFormFields).HasDefaultValue(false);
                entity.HasOne(ta => ta.Template).WithMany(t => t.Attachments).HasForeignKey(ta => ta.TemplateId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ta => ta.UploadedFile).WithMany(uf => uf.TemplateAttachments).HasForeignKey(ta => ta.UploadedFileId).OnDelete(DeleteBehavior.Restrict);
            });
        }

        private void ConfigureFieldMappingEntities(ModelBuilder modelBuilder)
        {
            // FieldMapping configuration
            modelBuilder.Entity<FieldMapping>(entity =>
            {
                entity.HasIndex(e => new { e.TemplateId, e.FieldName }).IsUnique();
                entity.HasIndex(e => new { e.TemplateId, e.PageNumber });
                entity.HasIndex(e => new { e.TemplateId, e.DisplayOrder });
                entity.HasIndex(e => e.DataType);

                // Default values
                entity.Property(e => e.PageNumber).HasDefaultValue(1);
                entity.Property(e => e.IsRequired).HasDefaultValue(false);
                entity.Property(e => e.UseOCR).HasDefaultValue(true);
                entity.Property(e => e.OCRLanguage).HasDefaultValue("eng");
                entity.Property(e => e.OCRConfidenceThreshold).HasDefaultValue(0.7m);
                entity.Property(e => e.IsVisible).HasDefaultValue(true);
                entity.Property(e => e.BorderColor).HasDefaultValue("#A54EE1");

                entity.HasOne(fm => fm.Template)
                    .WithMany(t => t.FieldMappings)
                    .HasForeignKey(fm => fm.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.X).HasPrecision(18, 6);
                entity.Property(e => e.Y).HasPrecision(18, 6);
                entity.Property(e => e.Width).HasPrecision(18, 6);
                entity.Property(e => e.Height).HasPrecision(18, 6);
                entity.Property(e => e.OCRConfidenceThreshold).HasPrecision(5, 4);
                entity.Property(e => e.MinValue).HasPrecision(18, 6);
                entity.Property(e => e.MaxValue).HasPrecision(18, 6);
            });

            // ✅ UPDATED: TemplateAnchor configuration (renamed from AnchorPoint)
            modelBuilder.Entity<TemplateAnchor>(entity =>
            {
                entity.HasIndex(e => e.TemplateId);
                entity.HasIndex(e => new { e.TemplateId, e.PageNumber });
                entity.HasIndex(e => new { e.TemplateId, e.Name }).IsUnique();
                entity.HasIndex(e => new { e.TemplateId, e.DisplayOrder });

                // Default values
                entity.Property(e => e.PageNumber).HasDefaultValue(1);
                entity.Property(e => e.IsRequired).HasDefaultValue(true);
                entity.Property(e => e.ConfidenceThreshold).HasDefaultValue(0.8m);
                entity.Property(e => e.IsVisible).HasDefaultValue(true);
                entity.Property(e => e.Color).HasDefaultValue("#00C48C");
                entity.Property(e => e.BorderColor).HasDefaultValue("#00C48C");

                // Navigation property to Template
                entity.HasOne(ta => ta.Template)
                    .WithMany(t => t.TemplateAnchors)
                    .HasForeignKey(ta => ta.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                // ✅ ADDED: Precision settings for rectangle coordinates
                entity.Property(e => e.X).HasPrecision(18, 6);
                entity.Property(e => e.Y).HasPrecision(18, 6);
                entity.Property(e => e.Width).HasPrecision(18, 6);
                entity.Property(e => e.Height).HasPrecision(18, 6);
                entity.Property(e => e.ConfidenceThreshold).HasPrecision(5, 4);
            });
        }

        private void ConfigureQueryEntities(ModelBuilder modelBuilder)
        {
            // Query configuration
            modelBuilder.Entity<Query>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ExecutionPriority);
                entity.HasIndex(e => e.CreatedDate);

                // Default values
                entity.Property(e => e.Status).HasDefaultValue(QueryStatus.Draft);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.ExecutionPriority).HasDefaultValue(5);
                entity.Property(e => e.ExecutionCount).HasDefaultValue(0);
                entity.Property(e => e.Version).HasDefaultValue("1.0");
            });

            // QueryConstant configuration
            modelBuilder.Entity<QueryConstant>(entity =>
            {
                entity.HasIndex(e => new { e.QueryId, e.Name }).IsUnique();
                entity.HasIndex(e => e.IsGlobal);
                entity.HasIndex(e => e.IsConstant);
                entity.HasIndex(e => e.DisplayOrder);

                // Default values
                entity.Property(e => e.IsGlobal).HasDefaultValue(false);
                entity.Property(e => e.IsConstant).HasDefaultValue(true);
                entity.Property(e => e.IsRequired).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.DataType).HasDefaultValue(DataTypeEnum.Number);

                // Query relationship (NULL for global constants)
                entity.HasOne(qc => qc.Query)
                    .WithMany(q => q.QueryConstants)
                    .HasForeignKey(qc => qc.QueryId)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired(false); // Allow NULL for global constants
            });

            // QueryOutput configuration
            modelBuilder.Entity<QueryOutput>(entity =>
            {
                entity.HasIndex(e => new { e.QueryId, e.Name }).IsUnique();
                entity.HasIndex(e => new { e.QueryId, e.ExecutionOrder });
                entity.HasIndex(e => e.IncludeInOutput);
                entity.HasIndex(e => e.IsActive);

                // Default values
                entity.Property(e => e.DataType).HasDefaultValue(DataTypeEnum.Number);
                entity.Property(e => e.DecimalPlaces).HasDefaultValue(2);
                entity.Property(e => e.FormatString).HasDefaultValue("N2");
                entity.Property(e => e.IsValid).HasDefaultValue(false);
                entity.Property(e => e.IncludeInOutput).HasDefaultValue(true);
                entity.Property(e => e.IsRequired).HasDefaultValue(false);
                entity.Property(e => e.IsVisible).HasDefaultValue(true);
                entity.Property(e => e.ExecutionOrder).HasDefaultValue(0);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Dependency JSON arrays defaults
                entity.Property(e => e.InputDependencies).HasDefaultValue("[]");
                entity.Property(e => e.OutputDependencies).HasDefaultValue("[]");
                entity.Property(e => e.GlobalDependencies).HasDefaultValue("[]");
                entity.Property(e => e.LocalDependencies).HasDefaultValue("[]");

                // Query relationship (CASCADE)
                entity.HasOne(qo => qo.Query)
                    .WithMany(q => q.QueryOutputs)
                    .HasForeignKey(qo => qo.QueryId)
                    .OnDelete(DeleteBehavior.Cascade);

                // FormulaCanvas relationship (NO ACTION)
                entity.HasOne(qo => qo.FormulaCanvas)
                    .WithMany(fc => fc.QueryOutputs)
                    .HasForeignKey(qo => qo.FormulaCanvasId)
                    .OnDelete(DeleteBehavior.NoAction)
                    .IsRequired(false);
            });

            // FormulaCanvas configuration
            modelBuilder.Entity<FormulaCanvas>(entity =>
            {
                entity.HasIndex(e => e.QueryId).IsUnique(); // 1:1 relationship with Query
                entity.HasIndex(e => e.IsActive);

                // Default values
                entity.Property(e => e.Width).HasDefaultValue(1200);
                entity.Property(e => e.Height).HasDefaultValue(800);
                entity.Property(e => e.ZoomLevel).HasPrecision(5, 2).HasDefaultValue(1.0m);
                entity.Property(e => e.IsValid).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.Version).HasDefaultValue("1.0");

                // 1:1 relationship with Query
                entity.HasOne(fc => fc.Query)
                    .WithOne(q => q.FormulaCanvas)
                    .HasForeignKey<FormulaCanvas>(fc => fc.QueryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void ConfigureProcessedEntities(ModelBuilder modelBuilder)
        {
            // ProcessedFile configuration
            modelBuilder.Entity<ProcessedFile>(entity =>
            {
                entity.HasIndex(e => e.TemplateId);
                entity.HasIndex(e => e.PeriodId);
                entity.HasIndex(e => new { e.TemplateId, e.PeriodId });
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.ProcessedDate);
                entity.HasIndex(e => e.NeedApproval);
                entity.HasIndex(e => e.HasFinancialInfo);

                // Default values
                entity.Property(e => e.Status).HasDefaultValue("Pending");
                entity.Property(e => e.NeedApproval).HasDefaultValue(false);
                entity.Property(e => e.HasFinancialInfo).HasDefaultValue(false);
                entity.Property(e => e.ProcessedDate).HasDefaultValueSql("GETUTCDATE()");

                // Template relationship
                entity.HasOne(pf => pf.Template)
                    .WithMany()
                    .HasForeignKey(pf => pf.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ProcessedField configuration
            modelBuilder.Entity<ProcessedField>(entity =>
            {
                entity.HasIndex(e => e.ProcessedFileId);
                entity.HasIndex(e => e.FieldMappingId);
                entity.HasIndex(e => e.FieldName);
                entity.HasIndex(e => e.OutputDataType);
                entity.HasIndex(e => e.IsValid);

                // Default values
                entity.Property(e => e.OutputDataType).HasDefaultValue("String");
                entity.Property(e => e.ExtractionConfidence).HasDefaultValue(0);
                entity.Property(e => e.CalculationConfidence).HasDefaultValue(0);
                entity.Property(e => e.IsValid).HasDefaultValue(true);

                // ProcessedFile relationship (CASCADE)
                entity.HasOne(pf => pf.ProcessedFile)
                    .WithMany(f => f.ProcessedFields)
                    .HasForeignKey(pf => pf.ProcessedFileId)
                    .OnDelete(DeleteBehavior.Cascade);

                // FieldMapping relationship
                entity.HasOne(pf => pf.MappedField)
                    .WithMany()
                    .HasForeignKey(pf => pf.FieldMappingId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ✅ NEW: QueryResult configuration
            modelBuilder.Entity<QueryResult>(entity =>
            {
                entity.HasIndex(e => e.QueryId);
                entity.HasIndex(e => e.QueryOutputId);
                entity.HasIndex(e => e.ProcessedFileId);
                entity.HasIndex(e => e.PeriodId);
                entity.HasIndex(e => new { e.QueryId, e.ProcessedFileId });
                entity.HasIndex(e => new { e.QueryId, e.PeriodId });
                entity.HasIndex(e => e.OutputName);
                entity.HasIndex(e => e.ExecutedDate);
                entity.HasIndex(e => e.IsValid);
                entity.HasIndex(e => e.NeedApproval);
                entity.HasIndex(e => e.HasFinancialData);
                entity.HasIndex(e => e.IsApproved);

                // Default values
                entity.Property(e => e.OutputDataType).HasDefaultValue("Number");
                entity.Property(e => e.ExecutedDate).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.ExecutionTimeMs).HasDefaultValue(0);
                entity.Property(e => e.CalculationConfidence).HasDefaultValue(0);
                entity.Property(e => e.IsValid).HasDefaultValue(true);
                entity.Property(e => e.NeedApproval).HasDefaultValue(false);
                entity.Property(e => e.HasFinancialData).HasDefaultValue(false);
                entity.Property(e => e.IsApproved).HasDefaultValue(false);

                // Query relationship (RESTRICT)
                entity.HasOne(qr => qr.Query)
                    .WithMany()
                    .HasForeignKey(qr => qr.QueryId)
                    .OnDelete(DeleteBehavior.Restrict);

                // QueryOutput relationship (RESTRICT)
                entity.HasOne(qr => qr.QueryOutput)
                    .WithMany()
                    .HasForeignKey(qr => qr.QueryOutputId)
                    .OnDelete(DeleteBehavior.Restrict);

                // ProcessedFile relationship (CASCADE)
                entity.HasOne(qr => qr.ProcessedFile)
                    .WithMany()
                    .HasForeignKey(qr => qr.ProcessedFileId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void SeedInitialData(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "MSP Certificates",
                    Description = "Financial and accounting related documents",
                    ColorCode = "#A54EE1",
                    IconClass = "fa fa-dollar-sign",
                    DisplayOrder = 1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    Id = 2,
                    Name = "Financial Documents",
                    Description = "Legal contracts and agreements",
                    ColorCode = "#4F008C",
                    IconClass = "fa fa-balance-scale",
                    DisplayOrder = 2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    Id = 3,
                    Name = "HR Documents",
                    Description = "Human resources and personnel documents",
                    ColorCode = "#00C48C",
                    IconClass = "fa fa-users",
                    DisplayOrder = 3,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    Id = 4,
                    Name = "Technical Documents",
                    Description = "Technical specifications and documentation",
                    ColorCode = "#1BCED8",
                    IconClass = "fa fa-cogs",
                    DisplayOrder = 4,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed General Directorates
            modelBuilder.Entity<GeneralDirectorate>().HasData(
                new GeneralDirectorate
                {
                    Id = 1,
                    Name = "Finance and Administration",

                    Description = "Financial management and administrative services",
                    DisplayOrder = 1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 2,
                    Name = "Information Technology",

                    Description = "Information technology services and infrastructure",
                    DisplayOrder = 2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 3,
                    Name = "Human Resources",

                    Description = "Human resources and organizational development",
                    DisplayOrder = 3,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 4,
                    Name = "Operations",

                    Description = "Operational activities and service delivery",
                    DisplayOrder = 4,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed Departments
            modelBuilder.Entity<Department>().HasData(
                new Department
                {
                    Id = 1,
                    Name = "TB",

                    Description = "Financial accounting and reporting",
                    GeneralDirectorateId = 1,
                    DisplayOrder = 1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 2,
                    Name = "SOC",

                    Description = "Procurement and vendor management",
                    GeneralDirectorateId = 1,
                    DisplayOrder = 2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 3,
                    Name = "NOC",

                    Description = "Software development and maintenance",
                    GeneralDirectorateId = 2,
                    DisplayOrder = 1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 4,
                    Name = "Infrastructure",

                    Description = "IT infrastructure and security",
                    GeneralDirectorateId = 2,
                    DisplayOrder = 2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 5,
                    Name = "Fixed",

                    Description = "Talent acquisition and development",
                    GeneralDirectorateId = 3,
                    DisplayOrder = 1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 6,
                    Name = "Transport",

                    Description = "Employee relations and compliance",
                    GeneralDirectorateId = 3,
                    DisplayOrder = 2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed Sample Vendors
            modelBuilder.Entity<Vendor>().HasData(
                new Vendor
                {
                    Id = 1,
                    Name = "Nokia",

                    CompanyName = "Nokia",
                    ContactPerson = "Ahmed Al-Rashid",
                    ContactEmail = "ahmed@nokia.sa",
                    ContactPhone = "+96650112367",
                    TaxNumber = "300123456789003",
                    CommercialRegister = "1010123456",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddDays(-30),
                    ApprovedBy = "System",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-45)
                },
                new Vendor
                {
                    Id = 2,
                    Name = "Ericsson",

                    CompanyName = "Ericsson Company",
                    ContactPerson = "Fatima Al-Zahra",
                    ContactEmail = "sali@Ericsson.sa",
                    ContactPhone = "+966542345678",
                    TaxNumber = "300234567890003",
                    CommercialRegister = "1010234567",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddDays(-15),
                    ApprovedBy = "System",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                },
                new Vendor
                {
                    Id = 3,
                    Name = "Huawei",

                    CompanyName = "Huawei Company",
                    ContactPerson = "Fatima Al-Zahra",
                    ContactEmail = "fatima@Huawei.sa",
                    ContactPhone = "+966509945678",
                    TaxNumber = "300234567890003",
                    CommercialRegister = "1010234567",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddDays(-15),
                    ApprovedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                },
                new Vendor
                {
                    Id = 4,
                    Name = "Cisco",

                    CompanyName = "Cisco Company",
                    ContactPerson = "Fatima Al-Zahra",
                    ContactEmail = "mohammad@cisco.sa",
                    ContactPhone = "+966502340078",
                    TaxNumber = "300234567890003",
                    CommercialRegister = "1010234567",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddDays(-15),
                    ApprovedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-20)
                }
            );
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Enable sensitive data logging in development only
#if DEBUG
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
#endif
        }
    }
}