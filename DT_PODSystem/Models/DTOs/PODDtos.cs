using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DT_PODSystem.Models.DTOs
{

    /// <summary>
    /// POD Update DTO - Handles POD updates with entries
    /// Aligned with JavaScript PODDataManager.collectFormData()
    /// </summary>
    public class PODUpdateDto
    {
        // Basic Information - matches JavaScript form field names
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        public string PodCode { get; set; } = string.Empty; // Read-only from form

        [StringLength(1000)]
        public string? Description { get; set; }

        // Business References
        [StringLength(100)]
        public string? PoNumber { get; set; }
        public string PONumber { get; internal set; }
        [StringLength(100)]
        public string? ContractNumber { get; set; }

        // Organizational - handled as nullable integers per JS logic
        public int CategoryId { get; set; }
        public int DepartmentId { get; set; }
        public int? VendorId { get; set; }

        // Processing Configuration
       
        public string? ProcessingFrequency { get; set; }
        public int ProcessingPriority { get; set; }

        // SPOC fields
        [StringLength(100)]
        public string? VendorSPOC { get; set; }

        [StringLength(100)]
        public string? GovernorSPOC { get; set; }

        [StringLength(100)]
        public string? FinanceSPOC { get; set; }

        // Business Rules - boolean flags from form checkboxes
        public bool RequiresApproval { get; set; }
        public bool ContainsFinancialData { get; set; }

        // POD Entries - matches generatePODEntriesJSON() output format
        public List<object> PodEntries { get; set; } = new List<object>();

        // Attachments - matches getUploadedFilesList() output format  
        public List<AttachmentDto> Attachments { get; set; } = new List<AttachmentDto>();
        public ProcessingFrequency Frequency { get;  set; }
        public string VendorSPOCUsername { get;  set; }
        public string GovernorSPOCUsername { get;  set; }
        public string FinanceSPOCUsername { get;  set; }
        public bool IsFinancialData { get;  set; }
        public string AutomationStatus { get;   set; }
    }

    /// <summary>
    /// Attachment DTO - matches JavaScript file structure
    /// </summary>
    public class AttachmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
    }

    /// <summary>
    /// POD Response DTO - for loading POD data to JavaScript
    /// </summary>
    public class PODResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public PODDataDto? Pod { get; set; }
    }

    /// <summary>
    /// POD Data DTO - structured for JavaScript consumption
    /// </summary>
    public class PODDataDto
    {
        // Basic fields matching JavaScript form names
        public string Name { get; set; } = string.Empty;
        public string PodCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? PoNumber { get; set; }
        public string? ContractNumber { get; set; }

        // Organizational
        public int CategoryId { get; set; }
        public int DepartmentId { get; set; }
        public int? VendorId { get; set; }

        // Processing Configuration  
        public string AutomationStatus { get; set; } = string.Empty;
        public string ProcessingFrequency { get; set; } = string.Empty;
        public int ProcessingPriority { get; set; }

        // SPOC
        public string? VendorSPOC { get; set; }
        public string? GovernorSPOC { get; set; }
        public string? FinanceSPOC { get; set; }

        // Business Rules
        public bool RequiresApproval { get; set; }
        public bool ContainsFinancialData { get; set; }

        // Display names for dropdowns
        public string? CategoryName { get; set; }
        public string? DepartmentName { get; set; }
        public string? VendorName { get; set; }

        // POD Entries in JavaScript format
        public List<object> Entries { get; set; } = new List<object>();
    }

    /// <summary>
    /// POD Entry DTO for data transfer
    /// </summary>
    public class PODEntryDto
    {
        public int Id { get; set; }
        public string EntryType { get; set; } = string.Empty;
        public int EntryOrder { get; set; }
        public string EntryData { get; set; } = string.Empty;
        public string? EntryName { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
    }

 

    /// <summary>
    /// POD Entry Update DTO
    /// </summary>
    public class PODEntryUpdateDto
    {
        public int? Id { get; set; } // Null for new entries
        public string EntryType { get; set; } = "single"; // 'single' or 'table'
        public int EntryOrder { get; set; } = 0;
        public string EntryData { get; set; } = string.Empty; // JSON data
        public string? EntryName { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public bool IsRequired { get; set; } = false;
        public bool IsActive { get; set; } = true;
    }




    /// <summary>
    /// Updated PODCreationDto for form binding
    /// </summary>
    public partial class PODCreationDto
    {
        [Required(ErrorMessage = "POD name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "POD name must be between 3 and 200 characters")]
        [Display(Name = "POD Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "PO Number cannot exceed 100 characters")]
        [Display(Name = "Purchase Order Number")]
        public string? PONumber { get; set; }

        [StringLength(100, ErrorMessage = "Contract Number cannot exceed 100 characters")]
        [Display(Name = "Contract Number")]
        public string? ContractNumber { get; set; }

        [Required(ErrorMessage = "Please select a category")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Please select a department")]
        [Display(Name = "Department")]
        public int DepartmentId { get; set; }

        [Display(Name = "Vendor")]
        public int? VendorId { get; set; }

        [Required]
        [Display(Name = "Automation Status")]
        public string AutomationStatus { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Processing Frequency")]
        public ProcessingFrequency Frequency { get; set; } = ProcessingFrequency.Monthly;

        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        [Display(Name = "Vendor SPOC")]
        public string? VendorSPOCUsername { get; set; }

        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        [Display(Name = "Governor SPOC")]
        public string? GovernorSPOCUsername { get; set; }

        [StringLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
        [Display(Name = "Finance SPOC")]
        public string? FinanceSPOCUsername { get; set; }

        [Display(Name = "Requires Approval")]
        public bool RequiresApproval { get; set; } = false;

        [Display(Name = "Contains Financial Data")]
        public bool IsFinancialData { get; set; } = false;

        [Range(1, 10, ErrorMessage = "Processing priority must be between 1 (highest) and 10 (lowest)")]
        [Display(Name = "Processing Priority")]
        public int ProcessingPriority { get; set; } = 5;
    }







    /// <summary>
    /// DTO for POD selection in dropdowns
    /// </summary>
    public class PODSelectionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PODCode { get; set; } = string.Empty;
        public string DisplayName => $"{Name} ({PODCode})";
        public PODStatus Status { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for POD list items
    /// </summary>
    public class PODListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string PODCode { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PODStatus Status { get; set; }
        public AutomationStatus AutomationStatus { get; set; }
        public ProcessingFrequency Frequency { get; set; }

        // Organizational info
        public string CategoryName { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string GeneralDirectorateName { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;

        // Statistics
        public int TemplateCount { get; set; }
        public int ProcessedCount { get; set; }
        public DateTime? LastProcessedDate { get; set; }

        // Audit info
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        // Business flags
        public bool RequiresApproval { get; set; }
        public bool IsFinancialData { get; set; }
        public int ProcessingPriority { get; set; }

        // UI helpers
        public string StatusBadgeClass => Status switch
        {
            PODStatus.Draft => "bg-secondary",
            PODStatus.Active => "bg-success",
            PODStatus.Suspended => "bg-danger",
            PODStatus.Archived => "bg-dark",
            _ => "bg-secondary"
        };

        public string StatusDisplayName => Status switch
        {
            PODStatus.Draft => "Draft",
            PODStatus.Active => "Active",
            PODStatus.Suspended => "Suspended",
            PODStatus.Archived => "Archived",
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
    }

    /// <summary>
    /// POD validation result
    /// </summary>
    public class PODValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }



}