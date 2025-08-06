//using Microsoft.AspNetCore.Mvc.Rendering;
//using System;
//using System.Collections.Generic;

//namespace DT_PODSystem.Models.ViewModels
//{
//    /// <summary>
//    /// Monthly outputs listing with role-based access and audit capabilities
//    /// </summary>
//    public class ProcessingOutputViewModel
//    {
//        // User context
//        public string UserRole { get; set; } = string.Empty;
//        public bool CanViewFinancialData { get; set; }
//        public bool CanAudit { get; set; }
//        public bool CanExport { get; set; }

//        // Filters
//        public ProcessingOutputFiltersViewModel Filters { get; set; } = new ProcessingOutputFiltersViewModel();

//        // Data
//        public List<ProcessingOutputItemViewModel> Outputs { get; set; } = new List<ProcessingOutputItemViewModel>();

//        // Pagination
//        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();

//        // Summary statistics
//        public ProcessingOutputSummaryViewModel Summary { get; set; } = new ProcessingOutputSummaryViewModel();

//        // Audit workflow (for Auditors)
//        public AuditWorkflowViewModel? AuditWorkflow { get; set; }

//        // Bulk operations
//        public List<BulkActionViewModel> BulkActions { get; set; } = new List<BulkActionViewModel>();

//        // Export options
//        public List<ExportOptionViewModel> ExportOptions { get; set; } = new List<ExportOptionViewModel>();

//        // Charts and analytics
//        public ProcessingTrendsChartViewModel? TrendsChart { get; set; }
//        public SuccessRateChartViewModel? SuccessRateChart { get; set; }
//    }

//    public class ProcessingOutputFiltersViewModel
//    {
//        public string? SearchTerm { get; set; }
//        public string? ProcessingMonth { get; set; }
//        public string? ProcessingStatus { get; set; }
//        public int? TemplateId { get; set; }
//        public int? CategoryId { get; set; }
//        public int? DepartmentId { get; set; }
//        public DateTime? ProcessingFromDate { get; set; }
//        public DateTime? ProcessingToDate { get; set; }
//        public decimal? MinAmount { get; set; }
//        public decimal? MaxAmount { get; set; }
//        public string? Currency { get; set; }
//        public string? AuditStatus { get; set; }
//        public string? AssignedAuditor { get; set; }
//        public bool? RequiresAudit { get; set; }
//        public bool? HasErrors { get; set; }
//        public decimal? MinConfidence { get; set; }
//        public decimal? MaxConfidence { get; set; }
//        public string SortBy { get; set; } = "ProcessingDate";
//        public string SortDirection { get; set; } = "desc";
//        public int PageSize { get; set; } = 25;
//        public string ViewMode { get; set; } = "table";
//        public bool ShowFinancialData { get; set; } = true;
//        public PaginationViewModel Pagination { get; set; } = new PaginationViewModel();
//        public List<SelectListItem> ProcessingMonthOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> ProcessingStatusOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> TemplateOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> CategoryOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> DepartmentOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> CurrencyOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> AuditStatusOptions { get; set; } = new List<SelectListItem>();
//        public List<SelectListItem> AuditorOptions { get; set; } = new List<SelectListItem>();
//        public bool ShowAdvancedFilters { get; set; }
//        public int ActiveFilterCount { get; set; }
//        public bool HasActiveFilters { get; set; }
//    }


//public class ProcessingOutputItemViewModel
//    {
//        public int Id { get; set; }
//        public string ProcessingMonth { get; set; } = string.Empty;
//        public string SourceFileName { get; set; } = string.Empty;
//        public DateTime ProcessingDate { get; set; }
//        public string ProcessingStatus { get; set; } = string.Empty;
//        public string ProcessingStatusBadgeClass { get; set; } = string.Empty;

//        // Template information
//        public int TemplateId { get; set; }
//        public string TemplateName { get; set; } = string.Empty;
//        public string CategoryName { get; set; } = string.Empty;
//        public string DepartmentName { get; set; } = string.Empty;
//        public string? VendorName { get; set; }

//        // Processing metrics
//        public decimal OverallConfidence { get; set; }
//        public int TotalFieldsProcessed { get; set; }
//        public int SuccessfulExtractions { get; set; }
//        public int FailedExtractions { get; set; }
//        public long ProcessingTimeMs { get; set; }

//        // Financial data (role-restricted)
//        public decimal? TotalAmount { get; set; }
//        public string? Currency { get; set; }
//        public decimal? TaxAmount { get; set; }
//        public decimal? NetAmount { get; set; }

//        // Audit information
//        public bool RequiresAudit { get; set; }
//        public bool IsAudited { get; set; }
//        public string? AuditStatus { get; set; }
//        public string? AuditStatusBadgeClass { get; set; }
//        public string? AuditedBy { get; set; }
//        public DateTime? AuditDate { get; set; }
//        public int? DaysInAudit { get; set; }
//        public bool IsOverdue { get; set; }

//        // File information
//        public string? OutputFileName { get; set; }
//        public string? OutputFormat { get; set; }
//        public bool HasOutputFile { get; set; }

//        // Error information
//        public bool HasErrors { get; set; }
//        public bool HasWarnings { get; set; }
//        public int ErrorCount { get; set; }
//        public int WarningCount { get; set; }

//        // Actions (role-based)
//        public List<ActionButtonViewModel> Actions { get; set; } = new List<ActionButtonViewModel>();

//        // Display properties
//        public bool IsSelected { get; set; }
//        public bool CanView { get; set; } = true;
//        public bool CanAudit { get; set; }
//        public bool CanReprocess { get; set; }
//        public bool CanExport { get; set; }
//        public bool CanDownload { get; set; }
//    }

//    public class ProcessingOutputSummaryViewModel
//    {
//        public int TotalOutputs { get; set; }
//        public int SuccessfulOutputs { get; set; }
//        public int FailedOutputs { get; set; }
//        public int OutputsWithWarnings { get; set; }

//        public decimal OverallSuccessRate { get; set; }
//        public decimal AverageConfidence { get; set; }
//        public double AverageProcessingTime { get; set; }

//        // Financial summary (role-restricted)
//        public decimal? TotalProcessedAmount { get; set; }
//        public string? PrimaryCurrency { get; set; }
//        public decimal? TotalTaxAmount { get; set; }
//        public decimal? TotalNetAmount { get; set; }
//        public int? FinancialDocumentCount { get; set; }

//        // Audit summary
//        public int? PendingAudits { get; set; }
//        public int? CompletedAudits { get; set; }
//        public int? RejectedAudits { get; set; }
//        public int? OverdueAudits { get; set; }
//        public decimal? AuditCompletionRate { get; set; }

//        // Monthly breakdown
//        public Dictionary<string, int> ByMonth { get; set; } = new Dictionary<string, int>();
//        public Dictionary<string, int> ByStatus { get; set; } = new Dictionary<string, int>();
//        public Dictionary<string, int> ByTemplate { get; set; } = new Dictionary<string, int>();
//        public Dictionary<string, int> ByDepartment { get; set; } = new Dictionary<string, int>();
//    }

//    public class AuditWorkflowViewModel
//    {
//        public List<PendingAuditItemViewModel> PendingAudits { get; set; } = new List<PendingAuditItemViewModel>();
//        public List<AuditActionViewModel> AvailableActions { get; set; } = new List<AuditActionViewModel>();
//        public AuditStatisticsViewModel Statistics { get; set; } = new AuditStatisticsViewModel();
//        public List<AuditTemplateViewModel> AuditTemplates { get; set; } = new List<AuditTemplateViewModel>();

//        // Current audit session
//        public int? CurrentAuditId { get; set; }
//        public string? CurrentAuditStatus { get; set; }
//        public DateTime? AuditSessionStart { get; set; }
//        public int? AuditsCompletedInSession { get; set; }
//    }

//    public class PendingAuditItemViewModel
//    {
//        public int ProcessingOutputId { get; set; }
//        public string TemplateName { get; set; } = string.Empty;
//        public string SourceFileName { get; set; } = string.Empty;
//        public DateTime ProcessingDate { get; set; }
//        public decimal? TotalAmount { get; set; }
//        public string? Currency { get; set; }
//        public string Priority { get; set; } = string.Empty;
//        public string PriorityBadgeClass { get; set; } = string.Empty;
//        public int DaysOverdue { get; set; }
//        public bool IsOverdue { get; set; }
//        public string? AssignedTo { get; set; }
//        public bool IsAssignedToCurrentUser { get; set; }
//        public decimal OverallConfidence { get; set; }
//        public bool HasErrors { get; set; }
//        public bool HasWarnings { get; set; }
//    }

//    public class AuditActionViewModel
//    {
//        public string Action { get; set; } = string.Empty;
//        public string DisplayName { get; set; } = string.Empty;
//        public string IconClass { get; set; } = string.Empty;
//        public string CssClass { get; set; } = string.Empty;
//        public string? Description { get; set; }
//        public bool RequiresComment { get; set; }
//        public bool RequiresConfirmation { get; set; }
//        public string? ConfirmationMessage { get; set; }
//        public List<string> RequiredFields { get; set; } = new List<string>();
//    }

//    public class AuditStatisticsViewModel
//    {
//        public int TotalAuditsCompleted { get; set; }
//        public int AuditsCompletedToday { get; set; }
//        public int AuditsCompletedThisWeek { get; set; }
//        public int AuditsCompletedThisMonth { get; set; }
//        public double AverageAuditTime { get; set; }
//        public decimal AuditEfficiency { get; set; } // audits per hour
//        public decimal ValidationAccuracy { get; set; }
//        public int TotalPendingAudits { get; set; }
//        public int MyPendingAudits { get; set; }
//        public int OverdueAudits { get; set; }
//    }

//    public class AuditTemplateViewModel
//    {
//        public string Name { get; set; } = string.Empty;
//        public string Description { get; set; } = string.Empty;
//        public List<AuditChecklistItemViewModel> ChecklistItems { get; set; } = new List<AuditChecklistItemViewModel>();
//        public List<string> CommonComments { get; set; } = new List<string>();
//        public List<string> CommonActions { get; set; } = new List<string>();
//    }

//    public class AuditChecklistItemViewModel
//    {
//        public string Item { get; set; } = string.Empty;
//        public string? Description { get; set; }
//        public bool IsRequired { get; set; }
//        public bool IsChecked { get; set; }
//        public string? Notes { get; set; }
//    }

//    public class ExportOptionViewModel
//    {
//        public string Format { get; set; } = string.Empty;
//        public string DisplayName { get; set; } = string.Empty;
//        public string Description { get; set; } = string.Empty;
//        public string IconClass { get; set; } = string.Empty;
//        public string FileExtension { get; set; } = string.Empty;
//        public bool RequiresConfiguration { get; set; }
//        public Dictionary<string, object> DefaultSettings { get; set; } = new Dictionary<string, object>();
//    }



//    public class SuccessRateChartViewModel
//    {
//        public List<string> Categories { get; set; } = new List<string>();
//        public List<ChartSeriesViewModel> Series { get; set; } = new List<ChartSeriesViewModel>();
//        public string Title { get; set; } = "Success Rate by Template";
//        public string Type { get; set; } = "column";
//    }


//}