using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DT_PODSystem.Models.Entities;
using DT_PODSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DT_PODSystem.Models.DTOs
{
    /// <summary>
    /// Request model for creating template via Job page - Maps to existing Step1DataDto
    /// </summary>
    public class CreateTemplateRequest
    {
        [Required]
        public int PODId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(100)]
        public string NamingConvention { get; set; } = "DOC_POD";

        [StringLength(500)]
        public string? TechnicalNotes { get; set; }

        [Range(1, 10)]
        public int ProcessingPriority { get; set; } = 5;

        public bool HasFormFields { get; set; } = false;

        [StringLength(10)]
        public string Version { get; set; } = "1.0";
    }

    /// <summary>
    /// Request model for updating template via Job page - Extends CreateTemplateRequest
    /// </summary>
    public class UpdateTemplateRequest : CreateTemplateRequest
    {
        [Required]
        public int TemplateId { get; set; }
    }
    public class Step1DataDto
    {
        // ✅ ALL PDFTEMPLATE ENTITY FIELDS (from PdfTemplate.cs)

        // Parent POD relationship (read-only, set during creation)
        public int PODId { get; set; }

        // Template identification
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "Untitled Template";

        // Technical PDF processing configuration
        [Required]
        [StringLength(100)]
        public string NamingConvention { get; set; } = "DOC_POD";

        [Required]
        public TemplateStatus Status { get; set; } = TemplateStatus.Draft;

        [StringLength(50)]
        public string? Version { get; set; } = "1.0";

        // Technical processing settings
        public int ProcessingPriority { get; set; } = 5; // 1-10 scale

        // Approval fields (read-only for Step 1)
        [StringLength(100)]
        public string? ApprovedBy { get; set; }
        public DateTime? ApprovalDate { get; set; }

        // Processing tracking (read-only for Step 1)
        public DateTime? LastProcessedDate { get; set; }
        public int ProcessedCount { get; set; } = 0;

        // Technical notes for this specific template configuration
        [StringLength(500)]
        public string? TechnicalNotes { get; set; }

        // PDF-specific settings
        public bool HasFormFields { get; set; } = false;

        [StringLength(50)]
        public string? ExpectedPdfVersion { get; set; }

        public int? ExpectedPageCount { get; set; }

        // Base entity fields
        public bool IsActive { get; set; } = true;
    }

    // ✅ UPDATED: TemplateDefinitionDto - Now minimal for templates
    public class TemplateDefinitionDto
    {
        public int Id { get; set; }
        public int PODId { get; set; } // ✅ NEW: Parent POD reference

        // ✅ REMOVED: Name, Description, CategoryId, DepartmentId, VendorId (now in POD)

        // ✅ TECHNICAL ONLY:
        public string NamingConvention { get; set; } = "DOC_POD";
        public TemplateStatus Status { get; set; }
        public string? Version { get; set; }
        public int ProcessingPriority { get; set; } = 5;
        public string? TechnicalNotes { get; set; }
        public bool HasFormFields { get; set; }
        public string? ExpectedPdfVersion { get; set; }
        public int? ExpectedPageCount { get; set; }
    }

    // ✅ NEW: POD DTOs
    public class PODDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PODCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PONumber { get; set; }
        public string? ContractNumber { get; set; }

        // Organizational relationships
        public int CategoryId { get; set; }
        public int DepartmentId { get; set; }
        public int? VendorId { get; set; }

        // Configuration
        public AutomationStatus AutomationStatus { get; set; }
        public ProcessingFrequency Frequency { get; set; }
        public string? VendorSPOCUsername { get; set; }
        public string? GovernorSPOCUsername { get; set; }
        public string? FinanceSPOCUsername { get; set; }

        // Status and workflow
        public PODStatus Status { get; set; }
        public string? Version { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsFinancialData { get; set; }
        public int ProcessingPriority { get; set; }

        // Navigation data
        public string CategoryName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GeneralDirectorateName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        // Statistics
        public int TemplateCount { get; set; }
        public int ProcessedCount { get; set; }
        public DateTime? LastProcessedDate { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public object Entries { get; internal set; }
    }

    // ✅ UPDATED: TemplateFilterOption - Now includes POD info
    public class TemplateFilterOption
    {
        public int Id { get; set; }
        public int PODId { get; set; } // ✅ NEW: Parent POD
        public string PODName { get; set; } = string.Empty; // ✅ NEW: Display POD name instead of template name
        public string NamingConvention { get; set; } = string.Empty; // ✅ UPDATED: Technical naming only

        // ✅ POD BUSINESS INFO:
        public string CategoryName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GeneralDirectorateName { get; set; } = string.Empty;
        public int FieldCount { get; set; }

        // ✅ TECHNICAL INFO:
        public TemplateStatus Status { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    // ✅ UPDATED: MappedFieldSearchResult - Now includes POD info
    public class MappedFieldSearchResult
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // ✅ UPDATED: Template and POD Information
        public int TemplateId { get; set; }
        public int PODId { get; set; } // ✅ NEW
        public string PODName { get; set; } = string.Empty; // ✅ NEW: Primary display name
        public string PODCode { get; set; } = string.Empty; // ✅ NEW
        public string TemplateNamingConvention { get; set; } = string.Empty; // ✅ UPDATED: Technical name only

        // ✅ POD BUSINESS INFO:
        public string CategoryName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GeneralDirectorateName { get; set; } = string.Empty;
    }

    // ✅ UPDATED: SaveProgressRequest - Now saves POD + Template data
    public class SaveProgressRequest
    {
        // ✅ POD DATA:
        public int? PODId { get; set; } // ✅ NEW: POD ID if editing
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? PONumber { get; set; }
        public string? ContractNumber { get; set; }
        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }
        public int? VendorId { get; set; }
        public AutomationStatus AutomationStatus { get; set; } = AutomationStatus.PDF;
        public ProcessingFrequency Frequency { get; set; } = ProcessingFrequency.Monthly;
        public string? VendorSPOCUsername { get; set; }
        public string? GovernorSPOCUsername { get; set; }
        public string? FinanceSPOCUsername { get; set; }
        public bool RequiresApproval { get; set; }
        public bool IsFinancialData { get; set; }
        public int ProcessingPriority { get; set; } = 5;

        // ✅ TEMPLATE DATA:
        public int TemplateId { get; set; }
        public string NamingConvention { get; set; } = "DOC_POD";
        public string? TechnicalNotes { get; set; }
        public bool HasFormFields { get; set; } = false;
        public string? ExpectedPdfVersion { get; set; }
        public int? ExpectedPageCount { get; set; }

        // Step data matching JavaScript
        public List<UploadedFileData> UploadedFiles { get; set; } = new List<UploadedFileData>();
        public List<FieldMappingData> FieldMappings { get; set; } = new List<FieldMappingData>();
    }

    // ✅ UNCHANGED: Step2DataDto, Step3DataDto, FieldMappingDto, etc. remain the same
    public class Step2DataDto
    {
        public List<UploadedFileDto> UploadedFiles { get; set; } = new();
        public string PrimaryFileName { get; set; } = string.Empty;
    }

    public class Step3DataDto
    {
        public List<FieldMappingDto> FieldMappings { get; set; } = new();
        public List<TemplateAnchorDto> TemplateAnchors { get; set; } = new();
    }

    // ✅ UNCHANGED: File and field mapping DTOs
    public class UploadedFileDto
    {
        public string OriginalFileName { get; set; } = string.Empty;
        public string SavedFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public bool Success { get; set; }
        public DateTime UploadedAt { get; set; }
        public int PageCount { get; set; }
        public string? PdfVersion { get; set; }
        public bool HasFormFields { get; set; }
    }

    public class FieldMappingDto
    {
        public int Id { get; set; }
        [Required] public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public int PageNumber { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string ValidationPattern { get; set; } = string.Empty;
        public string ValidationMessage { get; set; } = string.Empty;
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public bool UseOCR { get; set; }
        public string OCRLanguage { get; set; } = "en";
        public decimal OCRConfidenceThreshold { get; set; } = 0.8m;
        public int DisplayOrder { get; set; }
        public string BorderColor { get; set; } = "#A54EE1";
        public bool IsVisible { get; set; } = true;
    }

    public class TemplateAnchorDto
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        public int PageNumber { get; set; } = 1;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string ReferenceText { get; set; } = string.Empty;
        public string? ReferencePattern { get; set; }
        public bool IsRequired { get; set; } = true;
        public decimal ConfidenceThreshold { get; set; } = 0.8m;
        public int DisplayOrder { get; set; }
        public string? Color { get; set; } = "#00C48C";
        public bool IsVisible { get; set; } = true;
        public string? BorderColor { get; set; } = "#00C48C";
    }

    // ✅ UNCHANGED: Validation and finalization DTOs
    public class FinalizeTemplateRequest
    {
        public int TemplateId { get; set; }
        public SaveProgressRequest? ProgressData { get; set; }
        public bool RunPreValidation { get; set; } = true;
        public bool CreateBackup { get; set; } = true;
        public string? ActivationNotes { get; set; }
    }

    public class TemplateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public string? GeneratedFormula { get; set; }
    }

    // Supporting data classes
    public class UploadedFileData
    {
        public bool Success { get; set; }
        public object Data { get; set; } = new object();
        public string SavedFileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
    }

    public class FieldMappingData
    {
        public object Id { get; set; } = 0;
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Dictionary<string, object>? Coordinates { get; set; }
        public object TemplateAnchor { get; set; } = new object();
    }



    // ✅ Add this DTO class (can be in DTOs folder or same file)
    public class MappedFieldInfo
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public int TemplateId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
    }
     

    public class FileUploadDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();

        // File Information
        public string OriginalFileName { get; set; } = string.Empty;
        public string SavedFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string RelativePath { get; set; } = string.Empty; // Web-safe relative path
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileHash { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        // Storage Organization
        public string MonthFolder { get; set; } = string.Empty; // yyyy-MM format

        // PDF-specific properties
        public int PageCount { get; set; }
        public string PdfVersion { get; set; } = string.Empty;
        public bool HasFormFields { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime UploadDate { get; internal set; }
    }

    public class FileUploadRequest
    {
        public bool IsTemporary { get; set; } = false; // Default to permanent storage
        public bool UseMonthlyFolders { get; set; } = true; // Default to monthly organization
        public string CustomFolder { get; set; } = string.Empty; // Optional custom subfolder
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class BatchUploadDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalFiles { get; set; }
        public int SuccessfulUploads { get; set; }
        public int FailedUploads { get; set; }
        public List<FileUploadDto> Results { get; set; } = new List<FileUploadDto>();
        public Dictionary<string, object> Summary { get; set; } = new Dictionary<string, object>();
    }

    public class FileValidationDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public FileInfoDto FileInfo { get; set; } = new FileInfoDto();
    }

    public class FileInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public bool CanPreview { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public string MonthFolder { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; }

        // PDF-specific
        public int PageCount { get; set; }
        public string PdfVersion { get; set; } = string.Empty;
        public bool HasFormFields { get; set; }
    }

    public class MonthlyFolderDto
    {
        public string FolderName { get; set; } = string.Empty; // yyyy-MM
        public string DisplayName { get; set; } = string.Empty; // January 2025
        public int FileCount { get; set; }
        public long TotalSize { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastModified { get; set; }
    }

    public class FileCleanupDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int FilesDeleted { get; set; }
        public int FoldersDeleted { get; set; }
        public long SpaceFreed { get; set; } // In bytes
        public List<string> DeletedFiles { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }

    public class UploadConfigurationDto
    {
        public long MaxFileSize { get; set; }
        public string[] AllowedExtensions { get; set; } = Array.Empty<string>();
        public string[] AcceptedMimeTypes { get; set; } = Array.Empty<string>();
        public int MaxFiles { get; set; }
        public string UploadUrl { get; set; } = string.Empty;
        public string DeleteUrl { get; set; } = string.Empty;
        public string ValidateUrl { get; set; } = string.Empty;
        public StorageConfigDto StorageInfo { get; set; } = new StorageConfigDto();
    }

    public class StorageConfigDto
    {
        public bool UseMonthlyFolders { get; set; } = true;
        public string BasePath { get; set; } = "uploads/templates";
        public bool IsTemporary { get; set; } = false;
        public int CleanupAfterDays { get; set; } = 30;
    }


    /// <summary>
    /// Response model for mapped fields search results
    /// </summary>
    public class SearchMappedFieldsResponse
    {
        /// <summary>
        /// List of search results for the current page
        /// </summary>
        public List<MappedFieldSearchResult> Results { get; set; } = new();

        /// <summary>
        /// Total number of results matching the search criteria
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Current page number (0-based)
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Indicates if there are more results available
        /// </summary>
        public bool HasMore { get; set; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

        /// <summary>
        /// Starting index of current page results (1-based for display)
        /// </summary>
        public int StartIndex => Page * PageSize + 1;

        /// <summary>
        /// Ending index of current page results (1-based for display)
        /// </summary>
        public int EndIndex => Math.Min((Page + 1) * PageSize, TotalCount);
    }

    // Add this to your DTOs namespace (Models/DTOs/)
    public class SearchMappedFieldsRequest
    {
        public string SearchTerm { get; set; } = string.Empty;
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;

        // ✅ NEW: Multi-select template filter
        public List<int>? TemplateIds { get; set; }

        // ✅ OPTIONAL: Additional filters for future use
        public string? CategoryName { get; set; }
        public string? VendorName { get; set; }
        public string? DepartmentName { get; set; }

    }
     

    public class SaveFieldMappingsRequest
    {
        public int? TemplateId { get; set; }
        public List<FieldMappingDto>? FieldMappings { get; set; }
    }



    public class FileUploadResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public FileUploadDto? File { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class ImportResultDto
    {
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public string ImportedBy { get; set; } = string.Empty;
        public bool HasErrors => ErrorCount > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public int TotalProcessed => SuccessCount + ErrorCount;
    }


}