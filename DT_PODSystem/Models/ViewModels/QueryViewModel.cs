// Add these to QueryViewModel.cs

using System;
using System.Collections.Generic;
using System.Linq;
using DT_PODSystem.Models.DTOs;
using DT_PODSystem.Models.Enums;
using DT_PODSystem.Models.ViewModels;

namespace DT_PODSystem.Models.ViewModels
{
    // Add this complete QueryBuilderViewModel to QueryViewModel.cs

    public class QueryBuilderViewModel
    {
        public int QueryId { get; set; }
        public string QueryName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public QueryStatus Status { get; set; } = QueryStatus.Draft;
        public bool IsEditMode { get; set; } = false;

        // Constants - split into global and local for easier management
        public List<QueryConstantDto> GlobalConstants { get; set; } = new List<QueryConstantDto>();
        public List<QueryConstantDto> LocalConstants { get; set; } = new List<QueryConstantDto>();

        // Outputs
        public List<QueryOutputDto> OutputFields { get; set; } = new List<QueryOutputDto>();

        // Canvas state
        public string CanvasState { get; set; } = "{}";
        public List<CanvasElementDto> CanvasElements { get; set; } = new List<CanvasElementDto>();
        public List<CanvasConnectionDto> CanvasConnections { get; set; } = new List<CanvasConnectionDto>();

        // UI state
        public string ActiveSection { get; set; } = "constants"; // constants, outputs, canvas
        public bool ShowTemplateVariables { get; set; } = true;
        public bool ShowGlobalConstants { get; set; } = true;
        public bool ShowValidation { get; set; } = false;
        public bool ShowPreview { get; set; } = false;

        public bool CanSave { get; set; } = true;
        public bool CanTest { get; set; } = false;
        public bool CanActivate { get; set; } = false;
        public bool HasUnsavedChanges { get; set; } = false;

        // Progress tracking
        public double CompletionPercentage { get; set; }
        public bool IsComplete => Status == QueryStatus.Active || Status == QueryStatus.Testing;

        // Validation
        public bool IsValid { get; set; } = false;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public List<string> ValidationWarnings { get; set; } = new List<string>();

        // Execution information
        public int ExecutionPriority { get; set; } = 5;
        public int ExecutionCount { get; set; } = 0;
        public DateTime? LastExecuted { get; set; }
        public TimeSpan? LastExecutionTime { get; set; }

        // Helper properties
        public int GlobalConstantCount => GlobalConstants.Count;
        public int LocalConstantCount => LocalConstants.Count;
        public int TotalConstantCount => GlobalConstantCount + LocalConstantCount;
        public int OutputCount => OutputFields.Count;
        public int ActiveOutputCount => OutputFields.Count(o => o.IsActive);
        public int RequiredOutputCount => OutputFields.Count(o => o.IsRequired);

        public bool HasGlobalConstants => GlobalConstants.Any();
        public bool HasLocalConstants => LocalConstants.Any();
        public bool HasOutputs => OutputFields.Any();
        public bool HasCanvas => !string.IsNullOrEmpty(CanvasState) && CanvasState != "{}";
        public bool HasElements => CanvasElements.Any();
        public bool HasConnections => CanvasConnections.Any();

        // Status helpers
        public bool IsDraft => Status == QueryStatus.Draft;
        public bool IsTesting => Status == QueryStatus.Testing;
        public bool IsActive => Status == QueryStatus.Active;
        public bool IsArchived => Status == QueryStatus.Archived;
        public bool IsSuspended => Status == QueryStatus.Suspended;

        public string StatusDisplayName => Status.ToString();
        public string StatusBadgeClass => Status switch
        {
            QueryStatus.Draft => "bg-secondary",
            QueryStatus.Testing => "bg-warning",
            QueryStatus.Active => "bg-success",
            QueryStatus.Archived => "bg-dark",
            QueryStatus.Suspended => "bg-danger",
            _ => "bg-secondary"
        };

        // Permissions (set by controller based on user role)
        public bool CanEdit { get; set; } = true;
        public bool CanDelete { get; set; } = true;
        public bool CanChangeStatus { get; set; } = true;
        public bool CanExecute { get; set; } = true;

        // Form data for creation/editing
        public string OriginalName { get; set; } = string.Empty; // For change tracking
        public string OriginalDescription { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ModifiedBy { get; set; }

        // URLs for AJAX operations (set by controller)
        public string SaveConstantUrl { get; set; } = string.Empty;
        public string DeleteConstantUrl { get; set; } = string.Empty;
        public string GetConstantUrl { get; set; } = string.Empty;
        public string SaveOutputUrl { get; set; } = string.Empty;
        public string DeleteOutputUrl { get; set; } = string.Empty;
        public string GetOutputUrl { get; set; } = string.Empty;
        public string SaveQueryDataUrl { get; set; } = string.Empty;
        public string TestQueryUrl { get; set; } = string.Empty;
        public string GetConstantsUrl { get; set; } = string.Empty;
        public string GetOutputsUrl { get; set; } = string.Empty;
        public List<MappedFieldSearchResult> MappedFields { get; internal set; }
    }


    public class QueryListViewModel
    {
        public List<QueryListItemViewModel> Queries { get; set; } = new List<QueryListItemViewModel>();
        public QueryFiltersViewModel Filters { get; set; } = new QueryFiltersViewModel();
        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();

        // User permissions
        public string UserRole { get; set; } = string.Empty;
        public bool CanCreate { get; set; } = false;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
        public bool CanTest { get; set; } = false; // ✅ MISSING PROPERTY ADDED

        // Summary information
        public int TotalQueries => Queries.Count;
        public int ActiveQueries => Queries.Count(q => q.Status == QueryStatus.Active);
        public int DraftQueries => Queries.Count(q => q.Status == QueryStatus.Draft);
        public int TestingQueries => Queries.Count(q => q.Status == QueryStatus.Testing);
    }

    public class QueryListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public QueryStatus Status { get; set; }
        public string StatusDisplayName => Status.ToString();
        public string StatusBadgeClass => Status switch
        {
            QueryStatus.Draft => "bg-secondary",
            QueryStatus.Testing => "bg-warning",
            QueryStatus.Active => "bg-success",
            QueryStatus.Archived => "bg-dark",
            QueryStatus.Suspended => "bg-danger",
            _ => "bg-secondary"
        };

        public int ExecutionPriority { get; set; }
        public int ConstantsCount { get; set; }
        public int OutputsCount { get; set; }
        public int ExecutionCount { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? ModifiedBy { get; set; }
    }

    public class QueryFiltersViewModel
    {
        public string? SearchTerm { get; set; }
        public QueryStatus? Status { get; set; }
        public int? MinPriority { get; set; }
        public int? MaxPriority { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public string? CreatedBy { get; set; }
        public bool HasConstants { get; set; }
        public bool HasOutputs { get; set; }

        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
    }

    // Add these result classes to DTOs

    public class QueryTestResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public Dictionary<string, object> TestResults { get; set; } = new Dictionary<string, object>();
        public TimeSpan ExecutionTime { get; set; }
        public bool HasWarnings { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();

    }

    public class QueryValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public int CompletionPercentage { get; set; }
        public bool CanActivate { get; set; }
        public bool CanTest { get; set; }

    }

    public class QueryExecutionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, object> Results { get; set; } = new Dictionary<string, object>();
        public List<string> Errors { get; set; } = new List<string>();
        public TimeSpan ExecutionTime { get; set; }
        public int ProcessedRecords { get; set; }
    }

    public class BulkActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ProcessedCount { get; set; }
        public int TotalCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<int> SuccessfulIds { get; set; } = new List<int>();
        public List<int> FailedIds { get; set; } = new List<int>();

    }

    public class ConstantUsageResult
    {
        public bool IsInUse { get; set; }
        public int UsageCount { get; set; }
        public List<string> UsageDetails { get; set; } = new List<string>();
        public bool CanDelete { get; set; } = true;
        public string? DeleteBlockReason { get; set; }
        public bool IsUsed { get; set; }
        public List<string> UsedInOutputs { get; set; } = new List<string>();
        public List<string> UsedInFormulas { get; set; } = new List<string>();

    }

    public class OutputUsageResult
    {
        public bool IsUsed { get; set; }
        public int UsageCount { get; set; }
        public List<string> UsedInQueries { get; set; } = new List<string>();
        public List<string> UsedInReports { get; set; } = new List<string>();
        public bool CanDelete { get; set; }
        public string? DeleteBlockReason { get; set; }
        public List<string> UsageDetails { get; internal set; }
        public bool IsInUse { get; internal set; }
    }

}