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

        // Only 3 steps      
        public Step1TemplateDetailsViewModel Step1 { get; set; } = new();
        public Step2UploadViewModel Step2 { get; set; } = new();
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

        public int PODId { get; internal set; }

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

    // ✅ UPDATED: Step1PODDefinitionViewModel - Was Step1TemplateDefinitionViewModel
    public class Step1PODDefinitionViewModel
    {
        // ✅ POD BUSINESS FIELDS:
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PONumber { get; set; }
        public string? ContractNumber { get; set; }

        // Organizational relationships
        public int CategoryId { get; set; }
        public int DepartmentId { get; set; }
        public int? VendorId { get; set; }

        // ✅ NEW POD CONFIGURATION:
        public AutomationStatus AutomationStatus { get; set; } = AutomationStatus.PDF;
        public ProcessingFrequency Frequency { get; set; } = ProcessingFrequency.Monthly;
        public string? VendorSPOCUsername { get; set; }
        public string? GovernorSPOCUsername { get; set; }
        public string? FinanceSPOCUsername { get; set; }

        // Business configuration
        public bool RequiresApproval { get; set; }
        public bool IsFinancialData { get; set; }
        public int ProcessingPriority { get; set; } = 5;
        public PODStatus Status { get; set; } = PODStatus.Draft;

        // ✅ TEMPLATE TECHNICAL FIELDS:
        public string NamingConvention { get; set; } = "DOC_POD";
        public string? TechnicalNotes { get; set; }
        public bool HasFormFields { get; set; } = false;
        public string? ExpectedPdfVersion { get; set; }
        public int? ExpectedPageCount { get; set; }

        // UI Support - Dropdown options
        public List<SelectListItem> Categories { get; set; } = new();
        public List<SelectListItem> Departments { get; set; } = new();
        public List<SelectListItem> Vendors { get; set; } = new();
        public List<SelectListItem> AutomationStatusOptions { get; set; } = new();
        public List<SelectListItem> FrequencyOptions { get; set; } = new();

        // Validation support
        public bool IsValid => !string.IsNullOrEmpty(Name) && CategoryId > 0 && DepartmentId > 0;
        public List<string> ValidationErrors { get; set; } = new();
    }

    // ✅ UNCHANGED: Step2UploadViewModel remains the same
    public class Step2UploadViewModel
    {
        public List<FileUploadDto> UploadedFiles { get; set; } = new();
        public string? PrimaryFileName { get; set; }
        public int? PrimaryFileId { get; set; }
        public bool HasFiles => UploadedFiles.Any();
        public int FileCount => UploadedFiles.Count;
        public bool HasPrimaryFile => !string.IsNullOrEmpty(PrimaryFileName);
        public string? ErrorMessage { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    // ✅ UNCHANGED: Step3MappingViewModel remains the same
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

    // ✅ UPDATED: TemplateListViewModel - Now shows POD + Template information
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

    // ✅ UPDATED: TemplateListItemViewModel - Now displays POD business info
    public class TemplateListItemViewModel
    {
        // Template technical info
        public int Id { get; set; }
        public string NamingConvention { get; set; } = string.Empty; // ✅ UPDATED: Technical naming only
        public string? TechnicalNotes { get; set; } // ✅ UPDATED: Technical notes only
        public TemplateStatus Status { get; set; }
        public string? Version { get; set; } = "1.0";

        // ✅ POD BUSINESS INFO (displayed as main information):
        public int PODId { get; set; }
        public string PODName { get; set; } = string.Empty; // ✅ NEW: Primary display name
        public string PODCode { get; set; } = string.Empty; // ✅ NEW
        public string Description { get; set; } = string.Empty; // ✅ FROM POD
        public string? PONumber { get; set; } // ✅ NEW
        public string? ContractNumber { get; set; } // ✅ NEW

        // ✅ POD ORGANIZATIONAL INFO:
        public string CategoryName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GeneralDirectorateName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        // ✅ POD CONFIGURATION:
        public AutomationStatus AutomationStatus { get; set; }
        public ProcessingFrequency Frequency { get; set; }
        public bool IsFinancialData { get; set; }
        public bool RequiresApproval { get; set; }
        public int ProcessingPriority { get; set; }

        // Audit and statistics
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int FieldMappingCount { get; set; }
        public int ProcessedCount { get; set; }

        // UI Helpers
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

        public string AutomationStatusBadgeClass => AutomationStatus switch
        {
            AutomationStatus.PDF => "bg-info",
            AutomationStatus.ManualEntryWorkflow => "bg-warning",
            AutomationStatus.FullyAutomated => "bg-success",
            _ => "bg-secondary"
        };

        public string FrequencyDisplayName => Frequency switch
        {
            ProcessingFrequency.Monthly => "Monthly",
            ProcessingFrequency.Quarterly => "Quarterly",
            ProcessingFrequency.HalfYearly => "Half Yearly",
            ProcessingFrequency.Yearly => "Yearly",
            _ => "Unknown"
        };

        public TemplatePriority Priority => ProcessingPriority switch
        {
            <= 3 => TemplatePriority.Low,
            <= 6 => TemplatePriority.Medium,
            <= 8 => TemplatePriority.High,
            _ => TemplatePriority.Critical
        };

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
        public string Name { get; internal set; }
    }

    // ✅ UPDATED: TemplateFiltersViewModel - Now includes POD filters
    public class TemplateFiltersViewModel
    {
        // ✅ UPDATED: Search now searches POD names and descriptions
        public string? SearchTerm { get; set; }

        // Template status filter
        public TemplateStatus? Status { get; set; }

        // ✅ POD ORGANIZATIONAL FILTERS:
        public int? CategoryId { get; set; }
        public int? DepartmentId { get; set; }
        public int? VendorId { get; set; }

        // ✅ POD CONFIGURATION FILTERS:
        public AutomationStatus? AutomationStatus { get; set; }
        public ProcessingFrequency? Frequency { get; set; }
        public bool? IsFinancialData { get; set; }
        public bool? RequiresApproval { get; set; }

        // Date filters
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? CreatedFromDate { get; set; }
        public DateTime? CreatedToDate { get; set; }
        public DateTime? ModifiedFromDate { get; set; }
        public DateTime? ModifiedToDate { get; set; }

        // Sorting and pagination
        public string SortBy { get; set; } = "PODName"; // ✅ UPDATED: Sort by POD name by default
        public string SortDirection { get; set; } = "ASC";
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();

        // Filter state helpers
        public bool HasActiveFilters => !string.IsNullOrEmpty(SearchTerm) || Status.HasValue ||
            CategoryId.HasValue || DepartmentId.HasValue || VendorId.HasValue ||
            AutomationStatus.HasValue || Frequency.HasValue ||
            !string.IsNullOrEmpty(CreatedBy) || CreatedFromDate.HasValue ||
            CreatedToDate.HasValue || ModifiedFromDate.HasValue || ModifiedToDate.HasValue;

        public int ActiveFilterCount => (string.IsNullOrEmpty(SearchTerm) ? 0 : 1) +
            (Status.HasValue ? 1 : 0) + (CategoryId.HasValue ? 1 : 0) +
            (DepartmentId.HasValue ? 1 : 0) + (VendorId.HasValue ? 1 : 0) +
            (AutomationStatus.HasValue ? 1 : 0) + (Frequency.HasValue ? 1 : 0) +
            (string.IsNullOrEmpty(CreatedBy) ? 0 : 1) + (CreatedFromDate.HasValue ? 1 : 0) +
            (CreatedToDate.HasValue ? 1 : 0) + (ModifiedFromDate.HasValue ? 1 : 0) +
            (ModifiedToDate.HasValue ? 1 : 0);

        public bool ShowAdvancedFilters { get; set; }

        // UI Options
        public List<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> VendorOptions { get; set; } = new List<SelectListItem>(); // ✅ NEW
        public List<SelectListItem> AutomationStatusOptions { get; set; } = new List<SelectListItem>(); // ✅ NEW
        public List<SelectListItem> FrequencyOptions { get; set; } = new List<SelectListItem>(); // ✅ NEW
        public List<SelectListItem> CreatedByOptions { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PageSizeOptions { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "10", Text = "10 per page" },
            new SelectListItem { Value = "25", Text = "25 per page" },
            new SelectListItem { Value = "50", Text = "50 per page" },
            new SelectListItem { Value = "100", Text = "100 per page" }
        };
    }

    // ✅ UNCHANGED: Supporting ViewModels
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

        // ✅ NEW: POD-related summaries
        public int TotalPODs { get; set; }
        public int ActivePODs { get; set; }
        public int FullyAutomatedPODs { get; set; }
    }

    public class DataTypeOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // ✅ UNCHANGED: Action and UI ViewModels
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
   

    // Step 2 - Template Details & Configuration
    public class Step1TemplateDetailsViewModel
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
        public int PODId { get; internal set; }
        public string TechnicalNotes { get;  set; }
        public bool HasFormFields { get;  set; }
        public string ExpectedPdfVersion { get;  set; }
        public int? ExpectedPageCount { get;  set; }
    }

    
}