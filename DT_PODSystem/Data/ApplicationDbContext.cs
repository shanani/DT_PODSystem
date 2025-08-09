using System;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace DT_PODSystem.Data
{
    /// <summary>
    /// Entity Framework DbContext for DT_PODSystem application
    /// UPDATED - Added POD as parent entity of PdfTemplate with business logic
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<PODEntry> PODEntries { get; set; }

        // Core lookup entities
        public DbSet<Category> Categories { get; set; }
        public DbSet<GeneralDirectorate> GeneralDirectorates { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Vendor> Vendors { get; set; }

        // ✅ NEW: POD - Parent business entity
        public DbSet<POD> PODs { get; set; }
        public DbSet<PODAttachment> PODAttachments { get; set; }

        // File and template entities
        public DbSet<UploadedFile> UploadedFiles { get; set; }
        public DbSet<PdfTemplate> PdfTemplates { get; set; } // Now child of POD
        public DbSet<TemplateAttachment> TemplateAttachments { get; set; }

        // Field mapping entities
        public DbSet<FieldMapping> FieldMappings { get; set; }
        public DbSet<TemplateAnchor> TemplateAnchors { get; set; }

        // Query entities (separated from Template)
        public DbSet<QueryConstant> QueryConstants { get; set; }
        public DbSet<Query> Queries { get; set; }
        public DbSet<QueryOutput> QueryOutputs { get; set; }
        public DbSet<FormulaCanvas> FormulaCanvases { get; set; }

        // Processing and audit entities
        public DbSet<ProcessedFile> ProcessedFiles { get; set; }
        public DbSet<ProcessedField> ProcessedFields { get; set; }
        public DbSet<QueryResult> QueryResults { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints
            ConfigureBaseEntities(modelBuilder);
            ConfigureLookupEntities(modelBuilder);
            ConfigurePODEntities(modelBuilder); // ✅ NEW: POD configuration
            ConfigureTemplateEntities(modelBuilder); // Updated for POD relationship
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

        // ✅ NEW: POD Entity Configuration
        private void ConfigurePODEntities(ModelBuilder modelBuilder)
        {
            // POD configuration
            modelBuilder.Entity<POD>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.PODCode).IsUnique(); // Unique POD code
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CategoryId);
                entity.HasIndex(e => e.DepartmentId);
                entity.HasIndex(e => e.VendorId);
                entity.HasIndex(e => e.AutomationStatus);
                entity.HasIndex(e => e.Frequency);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.IsFinancialData);
                entity.HasIndex(e => e.PONumber);
                entity.HasIndex(e => e.ContractNumber);

                // Default values
                entity.Property(e => e.PODCode).HasDefaultValueSql("NEWID()"); // Temporary GUID, will be customized
                entity.Property(e => e.Status).HasDefaultValue(PODStatus.Draft);
                entity.Property(e => e.AutomationStatus).HasDefaultValue(AutomationStatus.PDF);
                entity.Property(e => e.Frequency).HasDefaultValue(ProcessingFrequency.Monthly);
                entity.Property(e => e.ProcessingPriority).HasDefaultValue(5);
                entity.Property(e => e.RequiresApproval).HasDefaultValue(false);
                entity.Property(e => e.IsFinancialData).HasDefaultValue(false);
                entity.Property(e => e.ProcessedCount).HasDefaultValue(0);

                // Relationships - POD owns the organizational relationships now
                entity.HasOne(p => p.Category)
                    .WithMany() // Categories don't need to navigate back to PODs directly
                    .HasForeignKey(p => p.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Department)
                    .WithMany() // Departments don't need to navigate back to PODs directly
                    .HasForeignKey(p => p.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(p => p.Vendor)
                    .WithMany() // Vendors don't need to navigate back to PODs directly
                    .HasForeignKey(p => p.VendorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // ✅ NEW: PODEntry configuration
            modelBuilder.Entity<PODEntry>(entity =>
            {
                entity.HasIndex(e => e.PODId);
                entity.HasIndex(e => e.EntryType);
                entity.HasIndex(e => e.EntryOrder);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.Category);

                // Default values
                entity.Property(e => e.EntryType).HasDefaultValue("single");
                entity.Property(e => e.EntryOrder).HasDefaultValue(0);
                entity.Property(e => e.IsRequired).HasDefaultValue(false);
                entity.Property(e => e.IsActive).HasDefaultValue(true);

                // Relationships
                entity.HasOne(pe => pe.POD)
                    .WithMany(p => p.Entries)
                    .HasForeignKey(pe => pe.PODId)
                    .OnDelete(DeleteBehavior.Cascade); // If POD deleted, entries are deleted
            });


            // ✅ CLEAN: PODAttachment configuration - No file duplication
            modelBuilder.Entity<PODAttachment>(entity =>
            {
                entity.HasIndex(e => new { e.PODId, e.UploadedFileId }).IsUnique(); // Prevent duplicate file attachments
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsPrimary);
                entity.HasIndex(e => e.DisplayOrder);
                entity.HasIndex(e => e.DocumentNumber);
                entity.HasIndex(e => e.DocumentDate);
                entity.HasIndex(e => e.ExpiryDate);
                entity.HasIndex(e => e.DocumentStatus);

                // Default values
                entity.Property(e => e.Type).HasDefaultValue(PODAttachmentType.Contract);
                entity.Property(e => e.IsPrimary).HasDefaultValue(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.RequiresApproval).HasDefaultValue(false);
                entity.Property(e => e.DocumentStatus).HasDefaultValue("Active");

                // Relationships
                entity.HasOne(pa => pa.POD)
                    .WithMany(p => p.Attachments)
                    .HasForeignKey(pa => pa.PODId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(pa => pa.UploadedFile)
                    .WithMany(uf => uf.PODAttachments)
                    .HasForeignKey(pa => pa.UploadedFileId)
                    .OnDelete(DeleteBehavior.Restrict); // Protect central file
            });
        }

        private void ConfigureTemplateEntities(ModelBuilder modelBuilder)
        {
            // ✅ UPDATED: PdfTemplate configuration - Now child of POD
            modelBuilder.Entity<PdfTemplate>(entity =>
            {
                entity.HasIndex(e => e.PODId); // Index on parent POD
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedDate);
                entity.HasIndex(e => e.NamingConvention);

                // Default values
                entity.Property(e => e.Status).HasDefaultValue(TemplateStatus.Draft);
                entity.Property(e => e.ProcessingPriority).HasDefaultValue(5);
                entity.Property(e => e.ProcessedCount).HasDefaultValue(0);
                entity.Property(e => e.HasFormFields).HasDefaultValue(false);

                // ✅ NEW: Parent relationship to POD
                entity.HasOne(t => t.POD)
                    .WithMany(p => p.Templates)
                    .HasForeignKey(t => t.PODId)
                    .OnDelete(DeleteBehavior.Cascade); // If POD deleted, templates are deleted

                // Remove old relationships to Category, Department, Vendor (now in POD)
            });

            // ✅ UPDATED: UploadedFile configuration - Central file hub
            modelBuilder.Entity<UploadedFile>(entity =>
            {
                entity.HasIndex(e => e.OriginalFileName);
                entity.HasIndex(e => e.SavedFileName).IsUnique(); // Ensure unique saved file names
                entity.HasIndex(e => e.IsTemporary);
                entity.HasIndex(e => e.CreatedDate);                 
                entity.HasIndex(e => e.ExpiryDate);
                entity.HasIndex(e => e.UploadSource);

                // Default values
                entity.Property(e => e.IsTemporary).HasDefaultValue(true);

                // File size and path constraints
                entity.Property(e => e.FileSize).IsRequired();
                entity.Property(e => e.FilePath).IsRequired().HasMaxLength(500);
            });

            // ✅ CLEAN: TemplateAttachment configuration - No file duplication
            modelBuilder.Entity<TemplateAttachment>(entity =>
            {
                entity.HasIndex(e => new { e.TemplateId, e.UploadedFileId }).IsUnique(); // Prevent duplicate file attachments
                entity.HasIndex(e => e.Type);
                entity.HasIndex(e => e.IsPrimary);
                entity.HasIndex(e => e.DisplayOrder);
                entity.HasIndex(e => e.ProcessingStatus);

                // Default values
                entity.Property(e => e.Type).HasDefaultValue(AttachmentType.Reference);
                entity.Property(e => e.IsPrimary).HasDefaultValue(false);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.HasFormFields).HasDefaultValue(false);

                // Relationships
                entity.HasOne(ta => ta.Template)
                    .WithMany(t => t.Attachments)
                    .HasForeignKey(ta => ta.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ta => ta.UploadedFile)
                    .WithMany(uf => uf.TemplateAttachments)
                    .HasForeignKey(ta => ta.UploadedFileId)
                    .OnDelete(DeleteBehavior.Restrict); // Protect central file
            });
        }

        private void ConfigureFieldMappingEntities(ModelBuilder modelBuilder)
        {
            // FieldMapping configuration (unchanged)
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

            // TemplateAnchor configuration (unchanged)
            modelBuilder.Entity<TemplateAnchor>(entity =>
            {
                entity.HasIndex(e => e.TemplateId);
                entity.HasIndex(e => new { e.TemplateId, e.PageNumber });
                entity.HasIndex(e => new { e.TemplateId, e.Name }).IsUnique();
                entity.HasIndex(e => e.DisplayOrder);

                // Default values
                entity.Property(e => e.PageNumber).HasDefaultValue(1);
                entity.Property(e => e.IsRequired).HasDefaultValue(true);
                entity.Property(e => e.ConfidenceThreshold).HasDefaultValue(0.8m);
                entity.Property(e => e.IsVisible).HasDefaultValue(true);
                entity.Property(e => e.Color).HasDefaultValue("#00C48C");
                entity.Property(e => e.BorderColor).HasDefaultValue("#00C48C");

                entity.HasOne(ta => ta.Template)
                    .WithMany(t => t.TemplateAnchors)
                    .HasForeignKey(ta => ta.TemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.X).HasPrecision(18, 6);
                entity.Property(e => e.Y).HasPrecision(18, 6);
                entity.Property(e => e.Width).HasPrecision(18, 6);
                entity.Property(e => e.Height).HasPrecision(18, 6);
                entity.Property(e => e.ConfidenceThreshold).HasPrecision(5, 4);
            });
        }

        private void ConfigureQueryEntities(ModelBuilder modelBuilder)
        {
            // Query configuration (unchanged)
            modelBuilder.Entity<Query>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.IsActive);
                entity.HasIndex(e => e.ExecutionPriority);

                // Default values
                entity.Property(e => e.Status).HasDefaultValue(QueryStatus.Draft);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.ExecutionPriority).HasDefaultValue(5);
                entity.Property(e => e.ExecutionCount).HasDefaultValue(0);
            });

            // QueryConstant configuration (unchanged)
            modelBuilder.Entity<QueryConstant>(entity =>
            {
                entity.HasIndex(e => e.Name);
                entity.HasIndex(e => new { e.QueryId, e.Name }).IsUnique();
                entity.HasIndex(e => e.IsGlobal);
                entity.HasIndex(e => e.IsActive);
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

            // QueryOutput configuration (unchanged)
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

            // FormulaCanvas configuration (unchanged)
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
            // ProcessedFile configuration (unchanged)
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

                // Template relationship (unchanged)
                entity.HasOne(pf => pf.Template)
                    .WithMany()
                    .HasForeignKey(pf => pf.TemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ProcessedField configuration (unchanged)
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

            // QueryResult configuration (unchanged)
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
                    Name = "Legal Documents",
                    Description = "Legal contracts, agreements, and compliance documents",
                    ColorCode = "#FF6B6B",
                    IconClass = "fa fa-gavel",
                    DisplayOrder = 4,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Category
                {
                    Id = 5,
                    Name = "Procurement Documents",
                    Description = "Purchase orders, contracts, and procurement related documents",
                    ColorCode = "#4ECDC4",
                    IconClass = "fa fa-shopping-cart",
                    DisplayOrder = 5,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed GeneralDirectorates
            modelBuilder.Entity<GeneralDirectorate>().HasData(
                new GeneralDirectorate
                {
                    Id = 1,
                    Name = "Information Technology",
                    Description = "IT systems, software, and technology services",
                    DisplayOrder = 1,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 2,
                    Name = "Finance and Administration",
                    Description = "Financial operations, budgeting, and administrative services",
                    DisplayOrder = 2,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 3,
                    Name = "Human Resources",
                    Description = "Personnel management, training, and development",
                    DisplayOrder = 3,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 4,
                    Name = "Operations and Maintenance",
                    Description = "Operational activities and facility maintenance",
                    DisplayOrder = 4,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new GeneralDirectorate
                {
                    Id = 5,
                    Name = "Legal and Compliance",
                    Description = "Legal affairs, regulatory compliance, and risk management",
                    DisplayOrder = 5,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed Departments
            modelBuilder.Entity<Department>().HasData(
                // IT Departments
                new Department
                {
                    Id = 1,
                    Name = "Software Development",
                    Description = "Application development and maintenance",
                    GeneralDirectorateId = 1,
                    DisplayOrder = 1,
                    ManagerName = "Ahmed Al-Rashid",
                    ContactEmail = "ahmed.rashid@company.sa",
                    ContactPhone = "+966-11-1234567",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 2,
                    Name = "Infrastructure and Networks",
                    Description = "IT infrastructure, networking, and security",
                    GeneralDirectorateId = 1,
                    DisplayOrder = 2,
                    ManagerName = "Sara Al-Mahmoud",
                    ContactEmail = "sara.mahmoud@company.sa",
                    ContactPhone = "+966-11-1234568",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 3,
                    Name = "Data Analytics",
                    Description = "Business intelligence and data analysis",
                    GeneralDirectorateId = 1,
                    DisplayOrder = 3,
                    ManagerName = "Omar Al-Fahad",
                    ContactEmail = "omar.fahad@company.sa",
                    ContactPhone = "+966-11-1234569",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },

                // Finance Departments
                new Department
                {
                    Id = 4,
                    Name = "Financial Planning",
                    Description = "Budget planning and financial forecasting",
                    GeneralDirectorateId = 2,
                    DisplayOrder = 1,
                    ManagerName = "Fatima Al-Zahra",
                    ContactEmail = "fatima.zahra@company.sa",
                    ContactPhone = "+966-11-1234570",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 5,
                    Name = "Accounts Payable",
                    Description = "Vendor payments and expense management",
                    GeneralDirectorateId = 2,
                    DisplayOrder = 2,
                    ManagerName = "Khalid Al-Otaibi",
                    ContactEmail = "khalid.otaibi@company.sa",
                    ContactPhone = "+966-11-1234571",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 6,
                    Name = "Treasury",
                    Description = "Cash management and financial investments",
                    GeneralDirectorateId = 2,
                    DisplayOrder = 3,
                    ManagerName = "Noura Al-Saud",
                    ContactEmail = "noura.saud@company.sa",
                    ContactPhone = "+966-11-1234572",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },

                // HR Departments
                new Department
                {
                    Id = 7,
                    Name = "Talent Acquisition",
                    Description = "Recruitment and onboarding",
                    GeneralDirectorateId = 3,
                    DisplayOrder = 1,
                    ManagerName = "Maha Al-Ghamdi",
                    ContactEmail = "maha.ghamdi@company.sa",
                    ContactPhone = "+966-11-1234573",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 8,
                    Name = "Employee Development",
                    Description = "Training and professional development",
                    GeneralDirectorateId = 3,
                    DisplayOrder = 2,
                    ManagerName = "Ibrahim Al-Harbi",
                    ContactEmail = "ibrahim.harbi@company.sa",
                    ContactPhone = "+966-11-1234574",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },

                // Operations Departments
                new Department
                {
                    Id = 9,
                    Name = "Facility Management",
                    Description = "Building maintenance and facility operations",
                    GeneralDirectorateId = 4,
                    DisplayOrder = 1,
                    ManagerName = "Abdullah Al-Mutairi",
                    ContactEmail = "abdullah.mutairi@company.sa",
                    ContactPhone = "+966-11-1234575",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 10,
                    Name = "Security Operations",
                    Description = "Physical and information security",
                    GeneralDirectorateId = 4,
                    DisplayOrder = 2,
                    ManagerName = "Reem Al-Johani",
                    ContactEmail = "reem.johani@company.sa",
                    ContactPhone = "+966-11-1234576",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },

                // Legal Departments
                new Department
                {
                    Id = 11,
                    Name = "Contract Management",
                    Description = "Contract negotiation and management",
                    GeneralDirectorateId = 5,
                    DisplayOrder = 1,
                    ManagerName = "Yousef Al-Dosari",
                    ContactEmail = "yousef.dosari@company.sa",
                    ContactPhone = "+966-11-1234577",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Department
                {
                    Id = 12,
                    Name = "Regulatory Compliance",
                    Description = "Regulatory compliance and auditing",
                    GeneralDirectorateId = 5,
                    DisplayOrder = 2,
                    ManagerName = "Layla Al-Shammari",
                    ContactEmail = "layla.shammari@company.sa",
                    ContactPhone = "+966-11-1234578",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed Vendors
            modelBuilder.Entity<Vendor>().HasData(
                new Vendor
                {
                    Id = 1,
                    Name = "Saudi Technology Solutions",
                    CompanyName = "STS Co. Ltd.",
                    Address = "King Fahd Road, Riyadh 12345, Saudi Arabia",
                    ContactPerson = "Ahmad Al-Riyadh",
                    ContactEmail = "ahmad@sts.sa",
                    ContactPhone = "+966-11-2345678",
                    TaxNumber = "300012345600003",
                    CommercialRegister = "1010123456",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddMonths(-6),
                    ApprovedBy = "System Admin",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Vendor
                {
                    Id = 2,
                    Name = "Gulf Consulting Group",
                    CompanyName = "GCG International",
                    Address = "Olaya District, Riyadh 11564, Saudi Arabia",
                    ContactPerson = "Mariam Al-Khalil",
                    ContactEmail = "mariam@gcg.com",
                    ContactPhone = "+966-11-3456789",
                    TaxNumber = "300012345600004",
                    CommercialRegister = "1010234567",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddMonths(-4),
                    ApprovedBy = "Procurement Manager",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Vendor
                {
                    Id = 3,
                    Name = "Digital Transformation Partners",
                    CompanyName = "DTP Solutions LLC",
                    Address = "King Abdullah Financial District, Riyadh 13519, Saudi Arabia",
                    ContactPerson = "Mohammed Al-Faisal",
                    ContactEmail = "mohammed@dtp.sa",
                    ContactPhone = "+966-11-4567890",
                    TaxNumber = "300012345600005",
                    CommercialRegister = "1010345678",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddMonths(-2),
                    ApprovedBy = "IT Director",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Vendor
                {
                    Id = 4,
                    Name = "Arabian Business Services",
                    CompanyName = "ABS Company",
                    Address = "Al-Malaz District, Riyadh 11432, Saudi Arabia",
                    ContactPerson = "Aisha Al-Mutairi",
                    ContactEmail = "aisha@abs.sa",
                    ContactPhone = "+966-11-5678901",
                    TaxNumber = "300012345600006",
                    CommercialRegister = "1010456789",
                    IsApproved = false, // Pending approval
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Vendor
                {
                    Id = 5,
                    Name = "Advanced Analytics Corp",
                    CompanyName = "AAC Saudi Arabia",
                    Address = "Diplomatic Quarter, Riyadh 11693, Saudi Arabia",
                    ContactPerson = "Hassan Al-Zahrani",
                    ContactEmail = "hassan@aac.sa",
                    ContactPhone = "+966-11-6789012",
                    TaxNumber = "300012345600007",
                    CommercialRegister = "1010567890",
                    IsApproved = true,
                    ApprovalDate = DateTime.UtcNow.AddMonths(-1),
                    ApprovedBy = "Finance Director",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            );

            // Seed Sample PODs
            modelBuilder.Entity<POD>().HasData(
                new POD
                {
                    Id = 1,
                    Name = "ERP System Implementation",
                    PODCode = "POD-ERP-2025-001",
                    Description = "Complete ERP system implementation for financial and operational modules",
                    PONumber = "PO-2025-IT-001",
                    ContractNumber = "CTR-ERP-2025",
                    CategoryId = 1, // MSP Certificates
                    DepartmentId = 1, // Software Development
                    VendorId = 1, // Saudi Technology Solutions
                    AutomationStatus = AutomationStatus.FullyAutomated,
                    Frequency = ProcessingFrequency.Monthly,
                    VendorSPOCUsername = "ahmad.vendor",
                    GovernorSPOCUsername = "sara.governor",
                    FinanceSPOCUsername = "fatima.finance",
                    Status = PODStatus.Active,
                    Version = "1.0",
                    RequiresApproval = true,
                    IsFinancialData = true,
                    ProcessingPriority = 8,
                    ApprovedBy = "IT Director",
                    ApprovalDate = DateTime.UtcNow.AddDays(-30),
                    ProcessedCount = 15,
                    LastProcessedDate = DateTime.UtcNow.AddDays(-5),
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddMonths(-2)
                },
                new POD
                {
                    Id = 2,
                    Name = "Financial Reporting Automation",
                    PODCode = "POD-FIN-2025-002",
                    Description = "Automated processing of monthly financial reports and compliance documents",
                    PONumber = "PO-2025-FIN-002",
                    ContractNumber = "CTR-FIN-2025-A",
                    CategoryId = 2, // Financial Documents
                    DepartmentId = 4, // Financial Planning
                    VendorId = 2, // Gulf Consulting Group
                    AutomationStatus = AutomationStatus.PDF,
                    Frequency = ProcessingFrequency.Monthly,
                    VendorSPOCUsername = "mariam.vendor",
                    GovernorSPOCUsername = "khalid.governor",
                    FinanceSPOCUsername = "noura.finance",
                    Status = PODStatus.Active,
                    Version = "1.2",
                    RequiresApproval = true,
                    IsFinancialData = true,
                    ProcessingPriority = 9,
                    ApprovedBy = "Finance Director",
                    ApprovalDate = DateTime.UtcNow.AddDays(-45),
                    ProcessedCount = 8,
                    LastProcessedDate = DateTime.UtcNow.AddDays(-2),
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddMonths(-3)
                },
                new POD
                {
                    Id = 3,
                    Name = "HR Performance Management",
                    PODCode = "POD-HR-2025-003",
                    Description = "Employee performance reviews and development tracking system",
                    CategoryId = 3, // HR Documents
                    DepartmentId = 7, // Talent Acquisition
                    VendorId = 3, // Digital Transformation Partners
                    AutomationStatus = AutomationStatus.ManualEntryWorkflow,
                    Frequency = ProcessingFrequency.Quarterly,
                    VendorSPOCUsername = "mohammed.vendor",
                    GovernorSPOCUsername = "maha.governor",
                    FinanceSPOCUsername = "fatima.finance",
                    Status = PODStatus.PendingApproval,
                    Version = "1.0",
                    RequiresApproval = true,
                    IsFinancialData = false,
                    ProcessingPriority = 6,
                    ProcessedCount = 0,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                },
                new POD
                {
                    Id = 4,
                    Name = "Facility Maintenance Contracts",
                    PODCode = "POD-OPS-2025-004",
                    Description = "Processing of facility maintenance requests and contract compliance",
                    PONumber = "PO-2025-OPS-004",
                    ContractNumber = "CTR-MAINT-2025",
                    CategoryId = 5, // Procurement Documents
                    DepartmentId = 9, // Facility Management
                    AutomationStatus = AutomationStatus.PDF,
                    Frequency = ProcessingFrequency.Monthly,
                    GovernorSPOCUsername = "abdullah.governor",
                    FinanceSPOCUsername = "khalid.finance",
                    Status = PODStatus.Draft,
                    Version = "1.0",
                    RequiresApproval = false,
                    IsFinancialData = true,
                    ProcessingPriority = 4,
                    ProcessedCount = 0,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-7)
                },
                new POD
                {
                    Id = 5,
                    Name = "Legal Compliance Reporting",
                    PODCode = "POD-LEG-2025-005",
                    Description = "Regulatory compliance reports and legal documentation processing",
                    CategoryId = 4, // Legal Documents
                    DepartmentId = 12, // Regulatory Compliance
                    VendorId = 5, // Advanced Analytics Corp
                    AutomationStatus = AutomationStatus.FullyAutomated,
                    Frequency = ProcessingFrequency.Quarterly,
                    VendorSPOCUsername = "hassan.vendor",
                    GovernorSPOCUsername = "layla.governor",
                    FinanceSPOCUsername = "noura.finance",
                    Status = PODStatus.Active,
                    Version = "1.1",
                    RequiresApproval = true,
                    IsFinancialData = true,
                    ProcessingPriority = 7,
                    ApprovedBy = "Legal Director",
                    ApprovalDate = DateTime.UtcNow.AddDays(-20),
                    ProcessedCount = 3,
                    LastProcessedDate = DateTime.UtcNow.AddDays(-10),
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddMonths(-1)
                }
            );

            // Seed Sample Uploaded Files (Central repository)
            modelBuilder.Entity<UploadedFile>().HasData(
                new UploadedFile
                {
                    Id = 1,
                    OriginalFileName = "ERP_Contract_Main.pdf",
                    SavedFileName = "erp_contract_20250101_001.pdf",
                    FilePath = "/uploads/documents/2025/01/erp_contract_20250101_001.pdf",
                    ContentType = "application/pdf",
                    FileSize = 2456789,
                    FileHash = "sha256_erp_contract_hash_001",
                    IsTemporary = false,
                    ProcessedDate = DateTime.UtcNow.AddDays(-60),
                    ProcessedBy = "System",
                    MimeType = "application/pdf",
                    UploadSource = "POD",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-60)
                },
                new UploadedFile
                {
                    Id = 2,
                    OriginalFileName = "Financial_Report_Template.pdf",
                    SavedFileName = "fin_report_template_20250102_001.pdf",
                    FilePath = "/uploads/templates/2025/01/fin_report_template_20250102_001.pdf",
                    ContentType = "application/pdf",
                    FileSize = 1234567,
                    FileHash = "sha256_fin_template_hash_001",
                    IsTemporary = false,
                    ProcessedDate = DateTime.UtcNow.AddDays(-45),
                    ProcessedBy = "System",
                    MimeType = "application/pdf",
                    UploadSource = "Wizard",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-45)
                },
                new UploadedFile
                {
                    Id = 3,
                    OriginalFileName = "HR_Policy_Document.pdf",
                    SavedFileName = "hr_policy_20250103_001.pdf",
                    FilePath = "/uploads/documents/2025/01/hr_policy_20250103_001.pdf",
                    ContentType = "application/pdf",
                    FileSize = 987654,
                    FileHash = "sha256_hr_policy_hash_001",
                    IsTemporary = false,
                    ProcessedDate = DateTime.UtcNow.AddDays(-15),
                    ProcessedBy = "System",
                    MimeType = "application/pdf",
                    UploadSource = "POD",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                }
            );

            // Seed Sample POD Attachments (Official documents)
            modelBuilder.Entity<PODAttachment>().HasData(
                new PODAttachment
                {
                    Id = 1,
                    PODId = 1, // ERP System Implementation
                    UploadedFileId = 1,
                    Type = PODAttachmentType.Contract,
                    DisplayName = "Main ERP Implementation Contract",
                    Description = "Primary contract document for ERP system implementation",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    DocumentNumber = "CTR-ERP-2025-001",
                    DocumentDate = DateTime.UtcNow.AddDays(-60),
                    IssuedBy = "Legal Department",
                    DocumentVersion = "1.0",
                    RequiresApproval = true,
                    ApprovedBy = "Legal Director",
                    ApprovalDate = DateTime.UtcNow.AddDays(-58),
                    DocumentStatus = "Active",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-60)
                },
                new PODAttachment
                {
                    Id = 2,
                    PODId = 3, // HR Performance Management
                    UploadedFileId = 3,
                    Type = PODAttachmentType.Specification,
                    DisplayName = "HR Policy and Procedures",
                    Description = "Standard operating procedures for HR performance management",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    DocumentNumber = "SOP-HR-2025-001",
                    DocumentDate = DateTime.UtcNow.AddDays(-15),
                    IssuedBy = "HR Department",
                    DocumentVersion = "2.1",
                    RequiresApproval = false,
                    DocumentStatus = "Active",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-15)
                }
            );

            // Seed Sample PDF Templates (Now children of PODs)
            modelBuilder.Entity<PdfTemplate>().HasData(
                new PdfTemplate
                {
                    Id = 1,
                    PODId = 1, // ERP System Implementation
                    NamingConvention = "ERP_INV_{YYYYMM}",
                    Status = TemplateStatus.Active,
                    Version = "1.2",
                    ProcessingPriority = 8,
                    ApprovedBy = "IT Director",
                    ApprovalDate = DateTime.UtcNow.AddDays(-30),
                    ProcessedCount = 15,
                    LastProcessedDate = DateTime.UtcNow.AddDays(-5),
                    TechnicalNotes = "Requires OCR preprocessing for invoice amounts",
                    HasFormFields = false,
                    ExpectedPdfVersion = "1.7",
                    ExpectedPageCount = 3,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-30)
                },
                new PdfTemplate
                {
                    Id = 2,
                    PODId = 2, // Financial Reporting Automation
                    NamingConvention = "FIN_RPT_{YYYYMM}_{DD}",
                    Status = TemplateStatus.Active,
                    Version = "2.0",
                    ProcessingPriority = 9,
                    ApprovedBy = "Finance Director",
                    ApprovalDate = DateTime.UtcNow.AddDays(-45),
                    ProcessedCount = 8,
                    LastProcessedDate = DateTime.UtcNow.AddDays(-2),
                    TechnicalNotes = "Multi-page template with dynamic table extraction",
                    HasFormFields = true,
                    ExpectedPdfVersion = "1.6",
                    ExpectedPageCount = 5,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-45)
                },
                new PdfTemplate
                {
                    Id = 3,
                    PODId = 5, // Legal Compliance Reporting
                    NamingConvention = "LEG_COMP_{YYYY}Q{Q}",
                    Status = TemplateStatus.Draft,
                    Version = "1.0",
                    ProcessingPriority = 7,
                    ProcessedCount = 0,
                    TechnicalNotes = "Quarterly compliance template with signature verification",
                    HasFormFields = true,
                    ExpectedPdfVersion = "1.7",
                    ExpectedPageCount = 8,
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-10)
                }
            );

            // Seed Sample Template Attachments (PDF processing files)
            modelBuilder.Entity<TemplateAttachment>().HasData(
                new TemplateAttachment
                {
                    Id = 1,
                    TemplateId = 2, // Financial Reporting Template
                    UploadedFileId = 2, // Financial_Report_Template.pdf
                    Type = AttachmentType.Original,
                    DisplayName = "Monthly Financial Report Template",
                    Description = "Primary template for monthly financial report processing",
                    DisplayOrder = 1,
                    IsPrimary = true,
                    PageCount = 5,
                    PdfVersion = "1.6",
                    HasFormFields = true,
                    ProcessingStatus = "Ready",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow.AddDays(-45)
                }
            );
        }
    }
}