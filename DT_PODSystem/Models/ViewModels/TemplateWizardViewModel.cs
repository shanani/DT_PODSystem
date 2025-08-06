using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DT_PODSystem.Models.ViewModels
{
    public class TemplateWizardViewModel
    {
        public int CurrentStep { get; set; } = 1;
        public int TotalSteps { get; set; } = 3; // Only 3 steps now
        public int TemplateId { get; set; }
        public bool IsEditMode { get; set; } = false;

        // Only 3 steps - removed Step4
        public Step1UploadViewModel Step1 { get; set; } = new();
        public Step2TemplateDetailsViewModel Step2 { get; set; } = new();
        public Step3MappingViewModel Step3 { get; set; } = new();

        // Navigation properties
        public List<WizardStepViewModel> Steps { get; set; } = new();
        public bool CanNavigateBack { get; set; }
        public bool CanNavigateForward { get; set; }
        public bool CanSaveAndExit { get; set; }
        public bool CanFinalize { get; set; }

        // Template status
        public TemplateStatus? Status { get; set; }
        public string? StatusDisplayName { get; set; }
        public string? StatusBadgeClass { get; set; }

        // Progress tracking
        public double ProgressPercentage => ((double)CurrentStep / TotalSteps) * 100;
        public bool IsFirstStep => CurrentStep == 1;
        public bool IsLastStep => CurrentStep == TotalSteps; // This is now step 3
        public bool HasCompletedSteps => CurrentStep > 1;
        public bool IsComplete => Status == TemplateStatus.Active || Status == TemplateStatus.Testing;

        // Helper methods
        public string GetStepTitle(int stepNumber)
        {
            return stepNumber switch
            {
                1 => "Upload PDF",
                2 => "Template Details",
                3 => "Map Fields & Finalize", // Updated title
                _ => $"Step {stepNumber}"
            };
        }

        public string GetStepDescription(int stepNumber)
        {
            return stepNumber switch
            {
                1 => "Upload your PDF document",
                2 => "Configure template settings",
                3 => "Map fields and create template", // Updated description
                _ => ""
            };
        }

        public string GetStepIcon(int stepNumber)
        {
            return stepNumber switch
            {
                1 => "fa-upload",
                2 => "fa-cog",
                3 => "fa-check-circle", // Changed from mapping to completion icon
                _ => "fa-circle"
            };
        }
    }

    public class WizardStepViewModel
    {
        public int StepNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsActive { get; set; }
        public bool IsAccessible { get; set; }
        public bool HasErrors { get; set; }
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }

    // Step 1 - Upload PDF Files
    public class Step1UploadViewModel
    {
        public string UploadUrl { get; set; } = "/Upload/UploadPdf";
        public string DeleteUrl { get; set; } = "/Upload/DeleteFile";
        public string ValidateUrl { get; set; } = "/Upload/ValidatePdf";

        public List<string> AcceptedTypes { get; set; } = new List<string> { ".pdf" };
        public int MaxFiles { get; set; } = 5;
        public long MaxFileSize { get; set; } = 10485760;
        public bool AllowMultiple { get; set; } = true;

        public List<FileUploadDto> UploadedFiles { get; set; } = new List<FileUploadDto>();
        public int PrimaryFileId { get; set; }

        public bool HasValidFiles => UploadedFiles.Any(f => f.ContentType == "application/pdf");
        public int TotalFiles => UploadedFiles.Count;
        public long TotalSize => UploadedFiles.Sum(f => f.FileSize);

        public string PrimaryFileName { get; set; } = string.Empty; // Add this line
    }

    // Step 2 - Template Details & Configuration
    public class Step2TemplateDetailsViewModel
    {
        [Required(ErrorMessage = "Template name is required")]
        [StringLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "File prefix is required.")]
        [StringLength(200, ErrorMessage = "File prefix cannot exceed 200 characters")]
        public string NamingConvention { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public TemplateStatus Status { get; set; } = TemplateStatus.Draft;
        public string? Version { get; set; } = "1.0";

        [Required(ErrorMessage = "Domain is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a domain")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Department is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a department")]
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Vendor is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a vendor")]
        public int? VendorId { get; set; }

        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Departments { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Vendors { get; set; } = new List<SelectListItem>();

        public bool RequiresApproval { get; set; }
        public bool IsFinancialData { get; set; }
        public int ProcessingPriority { get; set; } = 5;

        public string NamePreview { get; set; } = string.Empty;
        public bool IsNameValid { get; set; } = true;
        public string? ValidationErrors { get; set; }
    }

    // Step 3 - PDF Field Mapping & Template Finalization
    public class Step3MappingViewModel
    {
        public List<FieldMappingDto> FieldMappings { get; set; } = new List<FieldMappingDto>();
        public List<TemplateAnchorDto> TemplateAnchors { get; set; } = new List<TemplateAnchorDto>();
        public string? PdfViewerUrl { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; } = 1;

        // Field mapping helpers
        public bool HasMappedFields => FieldMappings.Any();
        public int MappedFieldCount => FieldMappings.Count;
        public int TemplateAnchorCount => TemplateAnchors.Count;

        // Data type options for field mapping
        public List<DataTypeOption> DataTypeOptions { get; set; } = new List<DataTypeOption>
        {
            new DataTypeOption { Value = "String", Text = "Text", Icon = "fa-font", Description = "Text values" },
            new DataTypeOption { Value = "Number", Text = "Number", Icon = "fa-hashtag", Description = "Numeric values" },
            new DataTypeOption { Value = "Date", Text = "Date", Icon = "fa-calendar", Description = "Date values" },
            new DataTypeOption { Value = "Currency", Text = "Currency", Icon = "fa-dollar-sign", Description = "Money amounts" }
        };

        // Finalization properties
        public bool ReadyToFinalize => HasMappedFields && TemplateAnchorCount >= 1;
        public string? FinalizationMessage { get; set; }
    }

    public class DataTypeOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // Template Management ViewModels
    public class TemplateListViewModel
    {
        public List<TemplateListItemViewModel> Templates { get; set; } = new List<TemplateListItemViewModel>();
        public TemplateFiltersViewModel Filters { get; set; } = new TemplateFiltersViewModel();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        public TemplateSummaryViewModel Summary { get; set; } = new TemplateSummaryViewModel();

        public string UserRole { get; set; } = string.Empty;
        public bool CanCreate { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanViewFinancialData { get; set; }
    }

    public class TemplateListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TemplateStatus Status { get; set; }
        public string StatusBadgeClass => Status switch
        {
            TemplateStatus.Draft => "bg-secondary",
            TemplateStatus.Testing => "bg-warning",
            TemplateStatus.Active => "bg-success",
            TemplateStatus.Archived => "badge-dark",
            TemplateStatus.Suspended => "bg-danger",
            _ => "bg-secondary"
        };
        public string StatusDisplayName => Status switch
        {
            TemplateStatus.Draft => "Draft",
            TemplateStatus.Testing => "Testing",
            TemplateStatus.Active => "Active",
            TemplateStatus.Archived => "Archived",
            TemplateStatus.Suspended => "Suspended",
            _ => "Unknown"
        };
        public string CategoryName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GeneralDirectorateName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string Version { get; set; } = "1.0";
        public int FieldMappingCount { get; set; }
        public int ProcessedCount { get; set; }
        public bool IsFinancialData { get; set; }
        public bool RequiresApproval { get; set; }
        public TemplatePriority Priority { get; set; }
        public string PriorityBadgeClass => Priority switch
        {
            TemplatePriority.Low => "bg-success",
            TemplatePriority.Medium => "bg-warning",
            TemplatePriority.High => "bg-danger",
            TemplatePriority.Critical => "bg-danger",
            _ => "bg-secondary"
        };
        public string PriorityDisplayName => Priority switch
        {
            TemplatePriority.Low => "Low",
            TemplatePriority.Medium => "Medium",
            TemplatePriority.High => "High",
            TemplatePriority.Critical => "Critical",
            _ => "Normal"
        };
        public List<string> Actions { get; set; } = new List<string>();
    }

    public class TemplateFiltersViewModel
    {
        public string? SearchTerm { get; set; }
        public TemplateStatus? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }
        public int? VendorId { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public bool? IsFinancialData { get; set; }
        public bool? RequiresApproval { get; set; }

        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
        public string? CreatedBy { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        public DateTime? ModifiedFromDate { get; set; }
        public DateTime? ModifiedToDate { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortDirection { get; set; } = "ASC";
        public bool HasActiveFilters => !string.IsNullOrEmpty(SearchTerm) || Status.HasValue || CategoryId.HasValue || DepartmentId.HasValue || VendorId.HasValue || !string.IsNullOrEmpty(CreatedBy) || CreatedFromDate.HasValue || CreatedToDate.HasValue || ModifiedFromDate.HasValue || ModifiedToDate.HasValue;
        public int ActiveFilterCount => (string.IsNullOrEmpty(SearchTerm) ? 0 : 1) + (Status.HasValue ? 1 : 0) + (CategoryId.HasValue ? 1 : 0) + (DepartmentId.HasValue ? 1 : 0) + (VendorId.HasValue ? 1 : 0) + (string.IsNullOrEmpty(CreatedBy) ? 0 : 1) + (CreatedFromDate.HasValue ? 1 : 0) + (CreatedToDate.HasValue ? 1 : 0) + (ModifiedFromDate.HasValue ? 1 : 0) + (ModifiedToDate.HasValue ? 1 : 0);
        public bool ShowAdvancedFilters { get; set; }
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CreatedByOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PageSizeOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "10", Text = "10 per page" },
            new SelectListItem { Value = "25", Text = "25 per page" },
            new SelectListItem { Value = "50", Text = "50 per page" },
            new SelectListItem { Value = "100", Text = "100 per page" }
        };
    }




    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }

    public class TemplateSummaryViewModel
    {
        public int TotalTemplates { get; set; }
        public int ActiveTemplates { get; set; }
        public int DraftTemplates { get; set; }
        public int ArchivedTemplates { get; set; }
    }

    // Common UI ViewModels
    public class BulkActionViewModel
    {
        public string Action { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string CssClass { get; set; } = "btn btn-outline-secondary";
        public bool RequiresConfirmation { get; set; } = true;
        public string? ConfirmationMessage { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
    }


    public class ActionButtonViewModel
    {
        public string Action { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string IconClass { get; set; } = string.Empty;
        public string CssClass { get; set; } = "btn btn-sm";
        public string? Url { get; set; }
        public string? OnClick { get; set; }
        public bool RequiresConfirmation { get; set; }
        public string? ConfirmationMessage { get; set; }
        public bool IsEnabled { get; set; } = true;
        public bool IsVisible { get; set; } = true;
        public string? Tooltip { get; set; }
        public bool OpenInNewWindow { get; set; }

    }
}